using Mirror;
using UnityEngine;

public class Experience : NetworkBehaviour, ISpatialHashGridData
{
    private Vector2Int sphCellIndex;

    public Vector2Int GetCellKey()
    {
        return sphCellIndex;
    }
    public void SetCellKey(Vector2Int index)
    {
        sphCellIndex = index;
    }

    public Vector2 GetPosition()
    {
        return transform.position;
    }

    [Server]
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {        
        SpatialHashGrid.Instance.Insert(this);
    }

}
