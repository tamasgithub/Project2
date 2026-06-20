using TMPro;
using UnityEngine;
using Mirror;
public class Player : Entity
{
    public int maxHp = 100;
    public float movementSpeed = 3.0f;
    public int XP { get; private set; } = 0;

    // private TextMeshProUGUI hpText;
   
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
        // hpText = canvas.transform.GetComponentInChildren<TextMeshProUGUI>();
        // hpText.text = Hp + "/" + MaxHp;
        // OnDamageTaken += UpdateHpUI;

        if (!isOwned) return;
       
        Camera.main.gameObject.GetComponent<CameraController>().POI = transform;
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

    public void GainXP(int amount)
    {
        XP += amount;
        
    }

    [Client]
    private void UpdateHpUI(int _)
    {
        // hpText.text = Hp + "/" + MaxHp;
    }

}
