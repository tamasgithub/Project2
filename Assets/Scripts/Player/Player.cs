using Mirror;
using UnityEngine;

public class Player : NetworkBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
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
