using Mirror;
using TMPro;
using UnityEngine;

public class Player : Entity
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int maxHp = 10;
    public float movementSpeed = 3.0f;

    private TextMeshProUGUI hpText;

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
        Canvas canvas = GetComponentInChildren<Canvas>();
        hpText = canvas.transform.GetComponentInChildren<TextMeshProUGUI>();
        hpText.text = Hp + "/" + MaxHp;
        OnDamageTaken += UpdateHpUI;
    }

    [Server]
    void Update()
    {
        foreach (ISpatialHashGridData data in SpatialHashGrid.Instance.GetNearObjects(transform.position, 1f))
        {
            if (data is Experience)
            {
                SpatialHashGrid.Instance.Remove(data);
                NetworkServer.Destroy(((Experience)data).gameObject);
            }
        }
    }

    [Client]
    private void UpdateHpUI(int _)
    {
        hpText.text = Hp + "/" + MaxHp;
    }

}
