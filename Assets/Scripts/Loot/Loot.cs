using Mirror;
using UnityEngine;

public class Loot : PoolableObject, ISpatialHashGridData
{
    public enum LootType
    {
        EXP,
        HP_POT
    }


    [SerializeField]
    private LootType _type;
    public LootType Type { get => _type; set => _type = value; }
    private Renderer MyRenderer { get => GetComponent<Renderer>(); }
    // ISpatialHashGridData implementation
    private Vector2Int sphCellIndex;
    public Vector2 GetPosition() => transform.position;
    public void SetCellKey(Vector2Int index) => sphCellIndex = index;
    public Vector2Int GetCellKey() => sphCellIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (!isServer) return;
        PoolableObjectType = _type == LootType.EXP ? PoolableObjectType.EXP : PoolableObjectType.HP_POTION;
    }

    public override void OnGet()
    {
        this.enabled = true;
        MyRenderer.enabled = true;
        SpatialHashGrid.Loot.Insert(this);
    }

    public override void OnReturn()
    {
        SpatialHashGrid.Loot.Remove(this);
        MyRenderer.enabled = false;
        this.enabled = false;
    }

    [ClientRpc]
    public override void RpcOnGet()
    {
        this.enabled = true;
        MyRenderer.enabled = true;
    }

    [ClientRpc]
    public override void RpcOnReturn()
    {
        MyRenderer.enabled = false;
        this.enabled = false;
    }
}
