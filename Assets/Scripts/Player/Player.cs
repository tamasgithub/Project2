using TMPro;
using UnityEngine;
using Mirror;
using System;
using UnityEngine.UI;
public class Player : Entity
{
    public int maxHp = 10;
    public float movementSpeed = 3.0f;
    private long xp = 0;
    private long xpToNextLevel = 5; // update on level up    public event Action<UpgradeRequest> OnLevelUp;

    private Image coplayerHpBar;
    // the arguments are current and needed xp
    public event Action<long, long> OnXpChanged;
    public event Action<UpgradeRequest> OnLevelUp;


    public override void OnStartServer()
    {
        base.OnStartServer();

        SetBaseData(maxHp, movementSpeed);
        // destroy UI on server
        if (isServerOnly)
        {
            Destroy(transform.Find("CoplayerVisuals"));
        }
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
            return;
        }

        Camera.main.gameObject.GetComponent<CameraController>().POI = transform;
    }

    protected override void Update()
    {
        base.Update();
        if (!isServer) return;
        foreach (Loot loot in SpatialHashGrid.Loot.GetNearObjects(transform.position, 1f))
        {
            Loot.LootType type = loot.Type;
            SpatialHashGrid.Loot.Remove(loot);
            NetworkServer.Destroy(loot.gameObject);
            switch (type)
            {
                case Loot.LootType.HP_POT:
                    Heal(1);
                    break;
                case Loot.LootType.EXP:
                default:
                    xp++;
                    OnXpChanged?.Invoke(xp, xpToNextLevel);
                    if (xp == xpToNextLevel)
                    {
                        RpcRequestUpgrade();
                    }
                    break;

            }

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
        OnLevelUp?.Invoke(new(gameObject));
    }

    [Command]
    public void RpcSubmitUpgradeChoice(UpgradeChoice choice)
    {
        Debug.Log($"Player selected: {choice.ChoiceType}");
        xp -= xpToNextLevel;
        xpToNextLevel *= Mathf.RoundToInt(1.5f);
        if (xp >= xpToNextLevel)
        {
            RpcRequestUpgrade();
        }
        OnXpChanged?.Invoke(xp, xpToNextLevel);
    }

}
