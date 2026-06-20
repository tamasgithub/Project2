using TMPro;
using UnityEngine;
using Mirror;
public class Player : Entity
{
    public int maxHp = 100;
    public float movementSpeed = 3.0f;
    private long xp = 0;
    private long xpToNextLevel = 5; // update on level up
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
        if (!isOwned)
        {
            hpText = transform.Find("Visuals").GetComponentInChildren<TextMeshProUGUI>();
            OnDamageTaken += UpdateHpUI;
            hpText.text = Hp + "/" + MaxHp;
            return;
        }

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

                xp++;
                if (xp >= xpToNextLevel)
                {
                    // OnLevelUp?.Invoke();
                }
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

}
