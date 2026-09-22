using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server side per lobby state. One instance is created for every additively loaded
/// GameScene by <see cref="SurvivorNetworkManager"/> when a game starts.
///
/// Everything in here used to be a process wide singleton or a static field, which meant
/// that the second lobby silently took over the first one's enemies, pools and tick events.
/// Resolve the context of any object with <c>GameContext.For(gameObject.scene)</c>.
///
/// Clients never own a GameContext: a client only ever has a single GameScene, so it can
/// keep resolving its scene by name.
/// </summary>
public class GameContext : MonoBehaviour
{
    private static readonly Dictionary<Scene, GameContext> contexts = new();

    public static GameContext For(Scene scene) =>
        contexts.TryGetValue(scene, out GameContext context) ? context : null;

    public static GameContext For(GameObject gameObject) =>
        gameObject != null ? For(gameObject.scene) : null;

    public static GameContext For(Component component) =>
        component != null ? For(component.gameObject.scene) : null;

    public int LobbyId { get; private set; } = -1;

    public SpatialHashGrid<ServerEnemy> EnemyGrid { get; private set; }
    public SpatialHashGrid<Loot> LootGrid { get; private set; }

    public ObjectPool ObjectPool { get; private set; }
    public EnemyManager EnemyManager { get; private set; }
    public CombatTickManager CombatTicks { get; private set; }
    public TriggerTickManager TriggerTicks { get; private set; }

    private readonly List<Player> players = new();
    public IReadOnlyList<Player> Players => players;

    private Scene scene;
    private Transform spawnRoot;

    #region Lifecycle

    public static GameContext Create(Scene scene, int lobbyId)
    {
        GameContext existing = For(scene);
        if (existing != null)
        {
            Debug.LogError($"Scene already holds a GameContext for lobby {existing.LobbyId:X}, not creating one for {lobbyId:X}");
            return existing;
        }

        GameObject go = new GameObject($"GameContext (lobby {lobbyId:X})");
        SceneManager.MoveGameObjectToScene(go, scene);

        GameContext context = go.AddComponent<GameContext>();
        context.Initialize(scene, lobbyId);
        return context;
    }

    private void Initialize(Scene scene, int lobbyId)
    {
        this.scene = scene;
        LobbyId = lobbyId;
        contexts[scene] = this;

        EnemyGrid = new SpatialHashGrid<ServerEnemy>(GlobalConstants.WORLD_CENTER, GlobalConstants.WORLD_SIZE, GlobalConstants.WORLD_CELLS);
        LootGrid = new SpatialHashGrid<Loot>(GlobalConstants.WORLD_CENTER, GlobalConstants.WORLD_SIZE, GlobalConstants.WORLD_CELLS);

        ObjectPool = HierarchyUtility.FindInScene<ObjectPool>(scene);
        EnemyManager = HierarchyUtility.FindInScene<EnemyManager>(scene);
        CombatTicks = HierarchyUtility.FindInScene<CombatTickManager>(scene);
        TriggerTicks = HierarchyUtility.FindInScene<TriggerTickManager>(scene);

        spawnRoot = HierarchyUtility.GetOrCreatePath("Spawned", scene);

        // Every lobby adds another game scene, and with it another camera that would render and
        // another AudioListener that would warn. The server renders nothing, so silence both.
        SceneViewControl.Disable(scene);

        if (ObjectPool == null) Debug.LogError($"Lobby {lobbyId:X}: no ObjectPool in the game scene");
        if (EnemyManager == null) Debug.LogError($"Lobby {lobbyId:X}: no EnemyManager in the game scene");
        if (CombatTicks == null) Debug.LogError($"Lobby {lobbyId:X}: no CombatTickManager in the game scene");
        if (TriggerTicks == null) Debug.LogError($"Lobby {lobbyId:X}: no TriggerTickManager in the game scene");

        Debug.Log($"GameContext ready for lobby {lobbyId:X} in scene {scene.name}#{scene.handle}");
    }

    private void OnDestroy()
    {
        // Remove by the stored scene: during an unload gameObject.scene is no longer reliable.
        if (For(scene) == this)
            contexts.Remove(scene);
    }

    #endregion

    #region Players

    public void AttachPlayer(Player player)
    {
        if (player == null || players.Contains(player)) return;

        players.Add(player);
        BindAll(player.gameObject, this);
    }

    public void DetachPlayer(Player player)
    {
        if (player == null) return;

        if (players.Remove(player))
            BindAll(player.gameObject, null);
    }

    private static void BindAll(GameObject target, GameContext context)
    {
        foreach (IContextBound bound in target.GetComponentsInChildren<IContextBound>(true))
        {
            bound.BindContext(context);
        }
    }

    #endregion

    #region Spawning

    /// <summary>
    /// Instantiates a networked prefab inside this lobby's scene without spawning it yet,
    /// so the caller can still configure the instance. The object is created under a root of
    /// this scene rather than moved there afterwards, which matters twice over:
    /// SceneInterestManagement reads the scene at spawn time, and the instance's own
    /// Awake/OnEnable already sees the scene it belongs to.
    /// Pair every Create with a <see cref="Spawn(GameObject)"/>.
    /// </summary>
    public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation) =>
        Instantiate(prefab, position, rotation, spawnRoot);

    /// <summary>Creates under an existing transform of this lobby, keeping the prefab's local pose.</summary>
    public GameObject CreateAttached(GameObject prefab, Transform parent) =>
        Instantiate(prefab, parent != null ? parent : spawnRoot);

    public void Spawn(GameObject instance)
    {
        if (!NetworkServer.active)
        {
            Debug.LogError("GameContext.Spawn called without an active server");
            return;
        }
        NetworkServer.Spawn(instance);
    }

    /// <summary>Create and spawn in one go, for objects that need no configuration in between.</summary>
    public GameObject CreateAndSpawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject instance = Create(prefab, position, rotation);
        Spawn(instance);
        return instance;
    }

    #endregion
}
