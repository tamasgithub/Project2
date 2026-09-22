using System;
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
        Transform coPlayerVisuals = transform.Find("CoplayerVisuals");
        if (isOwned)
        {
            Destroy(coPlayerVisuals.gameObject);
        } else
        {
            coPlayerVisuals.gameObject.SetActive(true);
            Image[] coplayerImages = coPlayerVisuals.GetComponentsInChildren<Image>();
            coplayerHpBar = coplayerImages[coplayerImages.Length - 1];
            coplayerHpBar.fillAmount = Mathf.Clamp01((float)Hp / maxHp);
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

        foreach (Loot loot in context.LootGrid.GetNearObjects(transform.position, 1f))
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
                    // Several orbs can be picked up in one frame, so this has to be >=:
                    // with == a single skipped value meant no level up ever again.
                    if (xp >= xpToNextLevel)
                    {
                        RpcRequestUpgrade();
                    }
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
            coplayerHpBar.fillAmount = Mathf.Clamp01((float)Hp / maxHp);
        }
    }

    [ClientRpc]
    private void RpcRequestUpgrade()
    {
        OnLevelUp?.Invoke(new UpgradeRequest(gameObject));
    }

    [Command]
    public void CmdSubmitUpgradeChoice(UpgradeChoice choice)
    {
        //  Debug.Log(JsonUtility.ToJson(choice));
        // Debug.Log($"Player selected: {choice.Type}");
        if (choice.Type == ChoiceType.STAT)
        {
            ApplyStatUpgrade(choice);
        }
        if(choice.Type == ChoiceType.ABILITY)
        {
            GetComponent<PlayerAbilityController>()?.HandleUpgradeChoice(choice);
        }

        xp -= xpToNextLevel;
        xpToNextLevel *= Mathf.RoundToInt(1.5f);
        if (xp >= xpToNextLevel)
        {
            RpcRequestUpgrade();
        }
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
