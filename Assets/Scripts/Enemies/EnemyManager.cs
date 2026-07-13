using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class EnemyManager : NetworkBehaviour
{
    public int lobbyId = -1;
    public static EnemyManager Instance;
    private List<GameObject> players = new();
    private HashSet<ServerEnemy> enemies = new();
    private HashSet<ServerEnemy> toRemove = new();
    private List<EnemyDto> enemyDtos = new();
    private List<DamageEventDto> damageDtos = new();
    public float ticksPerSeconds = 8;
    private float _tickRate;
    private float _tick;

    private SurvivorNetworkManager networkManager;

    void Awake()
    {
        Instance = this;
        _tickRate = 1.0f / GlobalConstants.ENEMY_STATE_UPDATE_RATE;
    }

    public override void OnStartServer()
    {
        players = GameObject.FindGameObjectsWithTag("Player").ToList();
        SurvivorNetworkManager.PlayerJoined += (conn) => players.Add(conn.identity.gameObject);
        SurvivorNetworkManager.PlayerLeft += (conn) => players.Remove(conn.identity.gameObject);
        networkManager = FindAnyObjectByType<SurvivorNetworkManager>();
    }

    void Update()
    {
        if (enemies.Count < 1) return;
        if (!isServer || lobbyId < 0) return;
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
        var targetPos = t != null ? (Vector2)t.position : Vector2.zero;


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

                PoolableObject dmgNr = ObjectPool.Instance?.Get(PoolableObjectType.DMG_NR, enemy.Position, Quaternion.identity);
                dmgNr.GetComponent<DamageNumber>().SetDamage(dmgDto.Amount, true);
            }

            //Dont Calculate Position if enemy is dead
            if (enemy.IsDead)
            {
                toRemove.Add(enemy);
                SpatialHashGrid.ServerEnemies.Remove(enemy);
                enemyDtos.Add(enemy.ToDto());
                continue;
            }
            enemy.Position += (targetPos - enemy.Position).normalized * enemy.MovementSpeed * deltaTime;

            //Anit clumping push

            foreach (ServerEnemy other in SpatialHashGrid.ServerEnemies.GetNearObjects(enemy.Position, 1f))
            {
                if (other == enemy) continue;
                var direction = (enemy.Position - other.Position).normalized;
                enemy.Position += direction * 10f * Time.deltaTime;
            }
            SpatialHashGrid.ServerEnemies.Update(enemy);
            enemyDtos.Add(enemy.ToDto());

        }

        enemies.ExceptWith(toRemove);
        return true;
    }

    [Server]
    private void SendMessages()
    {
        var enemyStatusMsg = new EnemyStatusMessage()
        {
            enemies = enemyDtos
        };
        networkManager.SendToClientsInGame(enemyStatusMsg, lobbyId);
        enemyDtos.Clear();


        var damageEventsMsg = new DamageEventsMessage()
        {
            damageEventDtos = damageDtos
        };
        networkManager.SendToClientsInGame(damageEventsMsg, lobbyId);
        damageDtos.Clear();
    }

    [Server]
    private Transform FindNearestPlayerPos()
    {
        Transform nearestTarget = null;
        float smallestDistance = float.MaxValue;
        foreach (GameObject player in players)
        {
            if (nearestTarget == null || Vector2.Distance(transform.position, player.transform.position) < smallestDistance)
            {
                smallestDistance = Vector2.Distance(transform.position, player.transform.position);
                nearestTarget = player.transform;
            }
        }
        return nearestTarget;
    }

    [Server]
    public void RegisterEnemy(ServerEnemy enemy)
    {
        enemies.Add(enemy);
        SpatialHashGrid.ServerEnemies.Insert(enemy);
    }
    [Server]
    public void UnregisterEnemy(ServerEnemy enemy)
    {
        enemies.Remove(enemy);
        SpatialHashGrid.ServerEnemies.Remove(enemy);
    }

}