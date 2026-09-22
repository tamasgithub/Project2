using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    void Awake()
    {
        PoolableObjectType = _type == LootType.EXP ? PoolableObjectType.EXP : PoolableObjectType.HP_POTION;
    }

    public override void OnStartClient()
    {
        // Applies the replicated in-use state.
        base.OnStartClient();

        Scene scene = SceneManager.GetSceneByName("GameScene");
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        // NOT "ObjectPool/...": the game scene already contains a root object of that name,
        // and it carries a NetworkIdentity. Mirror keeps scene objects with a NetworkIdentity
        // deactivated until they are spawned, so anything parented under it inherits
        // activeInHierarchy == false and is neither drawn nor updated.
        Transform parent = HierarchyUtility.GetOrCreatePath("PooledObjects/Loot/" + _type, scene);
        transform.SetParent(parent, false);
    }

    public override void OnGet()
    {
        this.enabled = true;
        MyRenderer.enabled = true;
        GameContext.For(this)?.LootGrid?.Insert(this);
    }

    public override void OnReturn()
    {
        GameContext.For(this)?.LootGrid?.Remove(this);
        MyRenderer.enabled = false;
        this.enabled = false;
    }

    protected override void ApplyOnClient(bool isInUse)
    {
        this.enabled = isInUse;
        MyRenderer.enabled = isInUse;
    }
}
