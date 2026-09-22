# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Unity **6000.4.11f1** (URP, 2D), server-authoritative co-op survivor game ("vampire-survivors"-like) built on **Mirror 96.0.1** (vendored under `Assets/Mirror`, not a UPM package). One dedicated server process hosts many lobbies at once.

Game code lives in `Assets/Scripts` and `Assets/Editor`. There are no `.asmdef` files for game code, so everything compiles into `Assembly-CSharp` together with Mirror's weaver — a compile error anywhere breaks all gameplay code. `.sln`/`.csproj` at the root are Unity-generated and gitignored.

## Commands

There is no test suite, no linter, and no package-manager workflow. Work happens in the Unity Editor.

Headless Linux server build (invokes `Assets/Editor/BuildScript.cs` → `Builds/LinuxServer/MyServer.x86_64`):

```bash
Unity -quit -batchmode -nographics -projectPath . -executeMethod BuildScript.BuildLinuxServer
```

Run the built server:

```bash
./Builds/LinuxServer/MyServer.x86_64 -batchmode -nographics
```

`SurvivorNetworkManager.Start` calls `StartServer()` automatically when `Application.isBatchMode`, so the headless build needs no extra flags.

CI (`.github/workflows/deploy.yml`, self-hosted runner, on every push) kills whatever listens on UDP 7777, runs `xvfb-run -a ./build.sh`, and restarts the server. **`build.sh` is not in the repository** — it only exists on the runner; changing build steps means changing it there too.

## Architecture

### Multi-lobby scene model

`Assets/Scripts/Networking/SurvivorNetworkManager.cs` is the centre of the project. A single server process keeps several concurrent games apart by loading **one additive scene instance per lobby**:

- `lobbies[lobbyId]` / `games[lobbyId]` map a lobby id to its additively loaded `LobbyScene` / `GameScene`; `clientsInLobbies` / `clientsInGames` track membership.
- A lobby id is assigned to a connection via `conn.authenticationData` and mirrored onto the `Player` as a `[SyncVar]`.
- Flow is a request/ready handshake over custom `NetworkMessage` structs (`Assets/Scripts/Networking/NetworkMessages/LobbyMessages.cs`): client sends `LobbyRequestMessage` → server creates/joins the scene and replies `LobbySceneMessage` → client loads its own copy of the scene and replies `LobbySceneReadyMessage` → server `MoveGameObjectToScene` + `RebuildObservers`. The same three-step pattern repeats for the game (`GameStartRequestMessage` / `GameSceneMessage` / `GameSceneReadyMessage`).
- Host mode short-circuits the client half (`NetworkClient.activeHost` checks) because the server already loaded the scene.
- Broadcast only via `SendToClientsInLobby` / `SendToClientsInGame` — never `NetworkServer.SendToAll`, which would leak across lobbies.

**Consequence for all new code:** `FindAnyObjectByType` and `GameObject.Find` can return an object belonging to a different lobby, and `SceneManager.GetSceneByName("GameScene")` returns whichever instance loaded first. On the server, resolve through `GameContext` or `HierarchyUtility.FindInScene<T>(scene)` / `FindAllInScene<T>(scene)` and scope by `gameObject.scene`. Resolving a scene by name is valid only in client-only code, guarded with `if (NetworkServer.active) return;`.

### GameContext: per-lobby state

`Assets/Scripts/Networking/GameContext.cs` holds everything that used to be a process-wide singleton: the two `SpatialHashGrid`s, the `ObjectPool`, the `EnemyManager`, both tick managers, and the players of that lobby. One is created per game scene by `SurvivorNetworkManager.CreateGame`; resolve it with `GameContext.For(gameObject.scene)` (overloads take a `GameObject` or a `Component`).

Rules that follow from it:

- **Spawn through the context**, never `Instantiate` + `NetworkServer.Spawn` directly. `context.Create(...)` places the instance inside the lobby's scene *before* `context.Spawn(...)` — which is what `SceneInterestManagement` reads to decide who may observe the object, and what the instance's own `Awake`/`OnEnable` sees.
- Components that subscribe to a tick (`AreaTrigger`, `DamageSource`) implement `IContextBound`. They bind themselves on enable; the player is bound explicitly by `GameContext.AttachPlayer`, because it is moved between scenes after it spawns.
- A `GameContext` exists **only on the server**. A client has exactly one GameScene and keeps resolving it by name.

### Host mode is not supported

Server and client in one process would give "which GameScene is mine?" two different answers. `SurvivorNetworkManager.OnStartHost` logs an error and stops. Server-only in the editor still works (the `NetworkManagerHUD` sits in the MainMenuScene); connect with a separate client.

### Enemies: server-side objects, snapshot replication

Enemies are **plain C# objects** (`ServerEnemy : ServerEntity`), not GameObjects: no `NetworkIdentity`, no Transform, server only. This replaced a GameObject-per-enemy model that cost one Mirror spawn message and SyncVar deltas per enemy.

`Entity : NetworkBehaviour` (`Assets/Scripts/System/Entity.cs`) still exists, but `Player` is its only subclass. `ServerEntity` is a near-copy of its stat/modifier/HP code — when changing stat behaviour, check whether both need the change.

The replication loop:

- `EnemySpawner` (server) creates plain `ServerEnemy` instances and registers them with the scene's `EnemyManager`.
- `EnemyManager.Update` ticks at `GlobalConstants.ENEMY_STATE_UPDATE_RATE`: moves every enemy toward the nearest player, applies anti-clumping via the spatial grid, aggregates that tick's `DamageEvent`s per enemy, and serialises everything into `EnemyDto` / `DamageEventDto` lists.
- Those go out as one `EnemyStatusMessage` + one `DamageEventsMessage` per tick to that lobby's clients.
- `EnemyVisualsManager` (client) is the mirror image: it keys visuals by the enemy's `string id`, instantiates `EnemyVisual` prefabs for ids it hasn't seen, destroys visuals for ids absent from the latest snapshot, and lerps positions in `LateUpdate`. It self-destructs when `!NetworkClient.active`.

Death is communicated by absence from the next snapshot, not by a message.

### No physics

Gameplay uses no Unity 2D physics. Proximity is resolved through `SpatialHashGrid` only: `AreaTrigger` reports overlaps on a fixed tick, `DamageSource` applies damage in a radius. Enemy contact damage reaches the player through the `AreaTrigger` on the player prefab (`Player.OnEnemyContact`), firing once per entry the way a collision callback used to.

Leftover colliders on prefabs are inert (a `BoxCollider2D` without a `Rigidbody2D` is a static collider with nothing to collide against). Do not reintroduce physics for gameplay: all lobbies share one physics world and every lobby plays on the same coordinates.

### Stats and modifiers

Stats are stored as a private base value plus per-stat `List<IStatModifier>`; the public property recomputes on **every get** by folding the modifier list (`ApplyXxxMods`). `StatModifierFlat` is inserted at index 0 and `StatModifierPercent` appended, so flat-then-percent ordering is implicit in list position. Modifier removal is commented out everywhere — modifiers are currently permanent.

### Abilities

`Ability` (`Assets/Scripts/Ability/`) subclasses are **plain C# classes, not MonoBehaviours**, owned and ticked by `PlayerAbilityController` on the server only. Two shapes: `PeriodicAbility` (cooldown-driven `Cast()`) and `PermanentAbility` (e.g. orbitals).

Tuning data lives in `AbilityData` ScriptableObjects under `Assets/ScriptableObjects`. Per-level values use `UpgradableStat<T>` with `UpgradePair<T>{ Level, Value }` steps; `Ability.UpgradeStats()` finds every private field implementing `IUpgradableStat` **by reflection** and upgrades it. So a new per-level stat only needs to be a private `UpgradableStat<T>` field — it is picked up automatically, and renaming/reordering fields silently changes what gets upgraded.

Projectiles (`Projectile` and subclasses) are real networked GameObjects spawned with `NetworkServer.Spawn`; they scale their stats by the owning `Entity`'s `Damage`, `ProjectileSize`, `Pierce`, `AreaOfEffectSize` in `LoadStats`, and damage `ServerEnemy` targets through `AreaTrigger.OnTriggerEnter`.

### Tick managers

Fixed-rate server loops instead of per-object `Update`, each a `NetworkBehaviour` exposing a `static event Action OnTick`:

- `CombatTickManager` — damage application (`DamageSource` subscribes). Note it hardcodes `1.0f / 2` in `Start`, ignoring its own `combatTicksPerSecond` field.
- `TriggerTickManager` — `GlobalConstants.TRIGGER_CHECK_RATE`.
- `EnemyManager` — enemy simulation + snapshot send, `GlobalConstants.ENEMY_STATE_UPDATE_RATE` (its serialized `ticksPerSeconds` field is unused).

Rates belong in `Assets/Scripts/Utility/GlobalConstants.cs`.

### Spatial queries

`SpatialHashGrid<T>` is a uniform grid used for all proximity queries (enemy anti-clumping, damage radius, loot pickup). Objects implement `ISpatialHashGridData` and cache their own cell key, so **callers must call `Update(data)` after moving an object** or the grid goes stale. Each lobby owns two grids on its `GameContext` (`EnemyGrid`, `LootGrid`), sized from `GlobalConstants.WORLD_*`: a 100×100 world centred on the origin with 50×50 cells; positions outside are clamped into edge cells.

### Object pool

`ObjectPool` lives once per game scene (reach it via `GameContext.ObjectPool`) and pre-instantiates and `NetworkServer.Spawn`s pooled objects (`PoolableObjectType`: EXP, HP_POTION, ENEMY, DMG_NR) and toggles them via `PoolableObject.OnGet`/`OnReturn` plus `RpcOnGet`/`RpcOnReturn` on clients. `isFake = true` bypasses pooling and instantiates/destroys instead — useful when the pool misbehaves.

### Player

`Player` is a `partial class` split across `Assets/Scripts/Player/Player.cs` (combat, XP, upgrades) and `Assets/Scripts/Player/LobbyMember.cs` (lobby/game membership `[SyncVar]`s, scene transitions, ready state). Static events (`OnPlayerMovedToLobby`, `OnPlayerMovedToGame`, `OnPlayerDisconnected`, `OnPlayerDataChanged`) drive the lobby UI.

Movement is server-authoritative without `NetworkTransform`: `PlayerInputController` reads the Input System action on the owner, sends `CmdMovePlayer`, the server integrates the position and broadcasts it with a `[ClientRpc]`.

Leveling: server detects XP threshold → `RpcRequestUpgrade` → client builds an `UpgradeRequest` (3 random choices from a hardcoded pool) → `UIManager` shows cards → `CmdSubmitUpgradeChoice` applies a stat modifier or routes to `PlayerAbilityController.HandleUpgradeChoice`.

## Conventions

- Mark server-only logic with `[Server]` / `[ServerCallback]` and client-only with `[Client]` / `[ClientCallback]`; Mirror's weaver enforces this at compile time. Prefer `[ServerCallback]` on Unity messages (`Awake`, `Start`, `Update`, `OnEnable`) over manual `isServer` guards: `isServer` only becomes true once Mirror has spawned that identity, so it is **false during `Instantiate`**, whereas `[ServerCallback]` checks `NetworkServer.active`. Subscribe to events of a freshly spawned object in `OnStartServer`, not `OnEnable`.
- Additively loaded scenes do not spawn their scene objects automatically: call `NetworkServer.SpawnObjects()` once the load completes.
- New scenes must be added to both `ProjectSettings/EditorBuildSettings.asset` and the `scenes` array in `Assets/Editor/BuildScript.cs`.
- The codebase carries commented-out code from DOTween hit-flash experiments and earlier iterations. Don't treat it as dead-code cleanup unless asked.
