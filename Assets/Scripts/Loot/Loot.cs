using Mirror;
using UnityEngine;

public class Loot : NetworkBehaviour, ISpatialHashGridData
{
    public enum LootType
    {
        EXP,
        HP_POT
    }

    private Vector2Int sphCellIndex;

    [SerializeField]
    private LootType _type;
    public LootType Type { get => _type; set => _type = value; }

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
