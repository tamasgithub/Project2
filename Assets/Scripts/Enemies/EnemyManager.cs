using System.Collections.Generic;
using Mirror;
using UnityEngine;

// Server only, one per game scene. All per lobby state now comes from the scene's
// GameContext instead of static singletons.
public class EnemyManager : NetworkBehaviour
{
    private readonly HashSet<ServerEnemy> enemies = new();
    private readonly HashSet<ServerEnemy> toRemove = new();
    private readonly List<EnemyDto> enemyDtos = new();
    private readonly List<DamageEventDto> damageDtos = new();

    private float _tickRate;
    private float _tick;

    private GameContext _context;
    private GameContext Context
    {
        get
        {
            if (_context == null) _context = GameContext.For(this);
            return _context;
        }
    }

    private SurvivorNetworkManager NetworkManager => Mirror.NetworkManager.singleton as SurvivorNetworkManager;

    [ServerCallback]
    void Awake()
    {
        _tickRate = 1.0f / GlobalConstants.ENEMY_STATE_UPDATE_RATE;
    }

    [ServerCallback]
    void Update()
    {
        if (enemies.Count < 1) return;
        if (Context == null) return;

        _tick += Time.deltaTime;
        if (_tick >= _tickRate)
        {
            if (UpdateEnemies(_tick))
            {
                SendMessages();
            }

            foreach (var enemy in enemies)
            {
                enemy.damageEvents.Clear();
            }
            _tick -= _tickRate;
        }
    }

    [Server]
    private bool UpdateEnemies(float deltaTime)
    {
        Transform t = FindNearestPlayerPos();
        if (t == null) return false;
        var targetPos = (Vector2)t.position;

        SpatialHashGrid<ServerEnemy> grid = Context.EnemyGrid;
        ObjectPool pool = Context.ObjectPool;

        toRemove.Clear();

        foreach (var enemy in enemies)
        {
            enemy.Update(deltaTime);
            //Damage Events
            var dmgDto = new DamageEventDto();
            dmgDto.TargetId = enemy.id;
            foreach (var damageEvent in enemy.damageEvents)
            {
                dmgDto.Amount += damageEvent.amount;
                dmgDto.Flags |= damageEvent.flag;
            }
            if (dmgDto.Amount > 0)
            {
                damageDtos.Add(dmgDto);

                PoolableObject dmgNr = pool != null ? pool.Get(PoolableObjectType.DMG_NR, enemy.Position, Quaternion.identity) : null;
                if (dmgNr != null)
                {
                    dmgNr.GetComponent<DamageNumber>().SetDamage(dmgDto.Amount, true);
                }
            }

            //Dont Calculate Position if enemy is dead
            if (enemy.IsDead)
            {
                toRemove.Add(enemy);
                grid.Remove(enemy);
                enemyDtos.Add(enemy.ToDto());
                continue;
            }
            enemy.Position += (targetPos - enemy.Position).normalized * enemy.MovementSpeed * deltaTime;

            //Anit clumping push

            foreach (ServerEnemy other in grid.GetNearObjects(enemy.Position, 1f))
            {
                if (other == enemy) continue;
                var direction = (enemy.Position - other.Position).normalized;
                enemy.Position += direction * 10f * Time.deltaTime;
            }
            grid.Update(enemy);
            enemyDtos.Add(enemy.ToDto());
        }

        enemies.ExceptWith(toRemove);
        return true;
    }

    [Server]
    private void SendMessages()
    {
        SurvivorNetworkManager networkManager = NetworkManager;
        if (networkManager == null) return;

        var enemyStatusMsg = new EnemyStatusMessage()
        {
            enemies = enemyDtos
        };
        networkManager.SendToClientsInGame(enemyStatusMsg, Context.LobbyId);
        enemyDtos.Clear();


        var damageEventsMsg = new DamageEventsMessage()
        {
            damageEventDtos = damageDtos
        };
        networkManager.SendToClientsInGame(damageEventsMsg, Context.LobbyId);
        damageDtos.Clear();
    }

    [Server]
    private Transform FindNearestPlayerPos()
    {
        Transform nearestTarget = null;
        float smallestDistance = float.MaxValue;
        foreach (Player player in Context.Players)
        {
            if (player == null) continue;
            float distance = Vector2.Distance(transform.position, player.transform.position);
            if (nearestTarget == null || distance < smallestDistance)
            {
                smallestDistance = distance;
                nearestTarget = player.transform;
            }
        }
        return nearestTarget;
    }

    [Server]
    public void RegisterEnemy(ServerEnemy enemy)
    {
        GameContext context = Context;
        if (context == null)
        {
            Debug.LogError("RegisterEnemy called before the lobby's GameContext exists");
            return;
        }
        enemies.Add(enemy);
        context.EnemyGrid.Insert(enemy);
    }

    [Server]
    public void UnregisterEnemy(ServerEnemy enemy)
    {
        enemies.Remove(enemy);
        Context?.EnemyGrid.Remove(enemy);
    }
}
