using Mirror;
using UnityEngine;

public class Player : Entity
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int maxHp = 10;
    public float movementSpeed = 3.0f;

    void Start()
    {
        SetBaseData(maxHp, movementSpeed);
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
}
