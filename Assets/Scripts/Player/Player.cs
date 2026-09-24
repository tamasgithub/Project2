using System;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public partial class Player : Entity
{
    public static event Action<Player> OnPlayerDisconnected;
    public static event Action<Player> OnPlayerDataChanged;

    public int maxHp = 10;
    public float movementSpeed = 3.0f;
    public GameObject cameraPrefab;
    // the arguments are current and needed xp
    public event Action<long, long> OnXpChanged;
    public event Action<UpgradeRequest> OnLevelUp;

    // SyncVars, not plain fields: xp only ever changes on the server, while the only
    // subscriber of OnXpChanged is the client's UIManager. Without replication the event was
    // raised on the server, where nobody listens, and the client's xp bar could never fill.
    [SyncVar(hook = nameof(OnXpSynced))]
    private long xp = 0;
    [SyncVar(hook = nameof(OnXpSynced))]
    private long xpToNextLevel = 5; // update on level up

    // Server side. The threshold is only consumed once the player submits a choice, so
    // without this every further orb picked up while standing above it would offer another
    // choice for the same level.
    private bool upgradePending;

    // Server side: the options actually offered for the pending level up. The client answers
    // with an index into this array, so it cannot invent an upgrade or its value.
    private UpgradeChoice[] offeredChoices = new UpgradeChoice[0];
    private Image coplayerHpBar;


    public override void OnStartServer()
    {
        base.OnStartServer();

        SetBaseData(maxHp, movementSpeed);

        // Contact damage used to come from the deleted GameObject enemy's OnCollisionEnter2D.
        // The player prefab already carries an AreaTrigger, which reports the same overlaps
        // through the spatial grid, once per entry just like a physics collision did.
        AreaTrigger contactTrigger = GetComponent<AreaTrigger>();
        if (contactTrigger != null)
        {
            contactTrigger.OnTriggerEnter += OnEnemyContact;
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();

        AreaTrigger contactTrigger = GetComponent<AreaTrigger>();
        if (contactTrigger != null)
        {
            contactTrigger.OnTriggerEnter -= OnEnemyContact;
        }
    }

    [Server]
    private void OnEnemyContact(ServerEntity other)
    {
        if (other is not ServerEnemy) return;
        ReceiveDamage(1);
    }

    public override void OnStartClient()
    {
        // Before any scene transition runs: a Screen Space - Camera canvas without a camera
        // falls back to overlay rendering, so the HUD would flash up for a frame on spawn.
        ShowInGameHud(false);

        Transform coPlayerVisuals = transform.Find("CoplayerVisuals");
        if (isOwned)
        {
            Destroy(coPlayerVisuals.gameObject);
        } else
        {
            coPlayerVisuals.gameObject.SetActive(true);
            Image[] coplayerImages = coPlayerVisuals.GetComponentsInChildren<Image>();
            coplayerHpBar = coplayerImages[coplayerImages.Length - 1];
            coplayerHpBar.fillAmount = Mathf.Clamp01((float)Hp / MaxHp);
            OnDamageTaken += UpdateHpUI;
            OnHpRecovered += UpdateHpUI;
        }

        if (ShouldJoinLobby())
        {
            if (isInGame)
            {
                MoveToClientGameScene();
            }
            else
            {
                MoveToClientLobbyScene();
            }
        }

        // The camera is attached in MoveToClientGameScene, for the local player only.
    }

    protected override void Update()
    {
        base.Update();
        if (!isServer) return;

        GameContext context = GameContext.For(this);
        if (context == null) return;

        foreach (Loot loot in context.LootGrid.GetNearObjects(transform.position, 2f))
        {
            Loot.LootType type = loot.Type;
            switch (type)
            {
                case Loot.LootType.HP_POT:
                    Heal(1);
                    break;
                case Loot.LootType.EXP:
                default:
                    xp++;
                    TryOfferUpgrade();
                    break;

            }
            context.ObjectPool.Return(loot);

        }

    }

    [Client]
    private void UpdateHpUI(int _)
    {
        if (!isOwned)
        {
            // MaxHp, not the serialized maxHp: the latter is the designer's base value and
            // ignores every upgrade the player has taken.
            coplayerHpBar.fillAmount = Mathf.Clamp01((float)Hp / MaxHp);
        }
    }

    /// <summary>
    /// Offers a level up choice, at most one at a time. Several orbs can be picked up in one
    /// frame, and the xp stays above the threshold until the player has chosen, so the offer
    /// has to be gated rather than raised per pickup.
    /// </summary>
    [Server]
    private void TryOfferUpgrade()
    {
        if (upgradePending || xp < xpToNextLevel) return;

        upgradePending = true;
        offeredChoices = UpgradeRequest.Roll(3);
        TargetOfferUpgrade(offeredChoices, CurrentAbilityLevels());
    }

    [Server]
    private AbilityLevel[] CurrentAbilityLevels()
    {
        PlayerAbilityController controller = GetComponent<PlayerAbilityController>();
        if (controller == null) return new AbilityLevel[0];

        return controller.Abilities
            .Select(ability => new AbilityLevel { name = ability.AbilityName, level = ability.Level })
            .ToArray();
    }

    // TargetRpc, not ClientRpc: this level up concerns nobody but its own player.
    // The ability levels travel along because the Ability objects exist only on the server,
    // and a separate SyncVar could still arrive after this message.
    [TargetRpc]
    private void TargetOfferUpgrade(UpgradeChoice[] choices, AbilityLevel[] abilityLevels)
    {
        OnLevelUp?.Invoke(new UpgradeRequest(choices, abilityLevels));
    }

    /// <summary>
    /// The client reports which of the offered options it picked. Only the index travels, and
    /// the server applies its own copy of that option
    /// </summary>
    [Command]
    public void CmdSubmitUpgradeChoice(int choiceIndex)
    {
        if (!upgradePending)
        {
            Debug.LogWarning($"{userName} submitted an upgrade choice without a pending level up");
            return;
        }

        if (choiceIndex < 0 || choiceIndex >= offeredChoices.Length)
        {
            Debug.LogWarning($"{userName} submitted upgrade choice {choiceIndex}, which was never offered");
            return;
        }

        UpgradeChoice choice = offeredChoices[choiceIndex];

        if (choice.Type == ChoiceType.STAT)
        {
            ApplyStatUpgrade(choice);
        }
        if (choice.Type == ChoiceType.ABILITY)
        {
            GetComponent<PlayerAbilityController>()?.HandleUpgradeChoice(choice);
        }

        xp -= xpToNextLevel;
        xpToNextLevel = Mathf.RoundToInt(1.5f * xpToNextLevel);

        // The choice has been made, so the next level may be offered right away.
        upgradePending = false;
        offeredChoices = new UpgradeChoice[0];
        TryOfferUpgrade();
    }

    // Raised on the client whenever one of the two replicated xp values arrives.
    private void OnXpSynced(long _, long __)
    {
        OnXpChanged?.Invoke(xp, xpToNextLevel);
    }

    private void ApplyStatUpgrade(UpgradeChoice choice)
    {
        switch (choice.StatName)
            {
                case StatName.MAX_HP:
                    RegisterMaxHpModifier(new StatModifierFlat(choice.Value));
                    break;
                case StatName.DAMAGE:
                    RegisterDamageModifier(new StatModifierFlat(choice.Value));
                    break;
                case StatName.MOVEMENTSPEED:
                    RegisterMovementSpeedModifier(new StatModifierPercent(choice.Value));
                    break;
                case StatName.PROJECTILE_SIZE:
                    RegisterProjectileSizeModifier(new StatModifierPercent(choice.Value));
                    break;
                case StatName.PIERCE:
                    RegisterPierceModifier(Mathf.RoundToInt(choice.Value));
                    break;
                case StatName.AREA_OF_EFFECT:
                    RegisterAreaOfEffectSizeModifier(new StatModifierPercent(choice.Value));
                    break;
            }
    }
   
    public override void OnStopClient()
    {
        base.OnStopClient();
        OnPlayerDisconnected?.Invoke(this);
    }

    public void DataChanged()
    {
        OnPlayerDataChanged?.Invoke(this);
    }
}
