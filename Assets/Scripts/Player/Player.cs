using TMPro;
using UnityEngine;
using Mirror;
using System;
public class Player : Entity
{
    public int maxHp = 10;
    public float movementSpeed = 3.0f;
    private long xp = 0;
    private long xpToNextLevel = 5; // update on level up    public event Action<UpgradeRequest> OnLevelUp;

    private TextMeshProUGUI hpText;
    public event Action<UpgradeRequest> OnLevelUp;

    public override void OnStartServer()
    {
        base.OnStartServer();

        SetBaseData(maxHp, movementSpeed);
        // destroy UI on server
        if (isServerOnly)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            Destroy(canvas.gameObject);
        }
    }

    public override void OnStartClient()
    {
        if (!isOwned)
        {
            hpText = transform.Find("Visuals").GetComponentInChildren<TextMeshProUGUI>();
            OnDamageTaken += UpdateHpUI;
            OnHpRecovered += UpdateHpUI;
            hpText.text = Hp + "/" + MaxHp;
            return;
        }

        Camera.main.gameObject.GetComponent<CameraController>().POI = transform;
    }

    void Update()
    {
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
            hpText.text = Hp + "/" + MaxHp;
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
        //TODO: Update xpToNextLevel
        if (xp >= xpToNextLevel)
        {
            RpcRequestUpgrade();
        }
    }

}
