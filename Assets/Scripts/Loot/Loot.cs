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

    // ISpatialHashGridData implementation
    public Vector2 GetPosition() => transform.position;
    public void SetCellKey(Vector2Int index) => sphCellIndex = index;
    public Vector2Int GetCellKey() => sphCellIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!isServer) return;
        SpatialHashGrid.Loot.Insert(this);
    }

}
