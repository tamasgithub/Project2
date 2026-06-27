using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class EnemyManager : NetworkBehaviour
{
    public static EnemyManager Instance;
    private List<GameObject> players;
    private HashSet<ServerEnemy> enemies = new();
    private HashSet<ServerEnemy> toRemove = new();
    private List<EnemyDto> enemyDtos = new();
    public float ticksPerSeconds = 8;
    private float _tickRate;
    private float _tick;
 
    void Awake()
    {
        Instance = this;
        _tickRate = 1.0f / ticksPerSeconds;
    }

    public override void OnStartServer()
    {
        players = GameObject.FindGameObjectsWithTag("Player").ToList();
        SurvivorNetworkManager.PlayerJoined += (conn) => players.Add(conn.identity.gameObject);
        SurvivorNetworkManager.PlayerLeft += (conn) => players.Remove(conn.identity.gameObject);

    }

    void Update()
    {
        if (!isServer) return;
        _tick += Time.deltaTime;
        if (_tick >= _tickRate)
        {
            UpdatePositions(_tick);
            SendSnapShot();
            foreach (var enemy in enemies)
            {
                enemy.damageEvents.Clear();
            }
            _tick -= _tickRate;
        }

    }

    private void UpdatePositions(float deltaTime)
    {
        var targetPos = (Vector2)FindNearestPlayerPos().position;
        if (targetPos == null) return;
        toRemove.Clear();
        foreach (var enemy in enemies)
        {
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
                enemy.Position += direction * 1 * Time.deltaTime;
            }
            SpatialHashGrid.ServerEnemies.Update(enemy);
            enemyDtos.Add(enemy.ToDto());

        }

        enemies.ExceptWith(toRemove);

    }

    private void SendSnapShot()
    {
        var msg = new EnemySnapshot()
        {
            enemies = enemyDtos
        };

        NetworkServer.SendToAll(msg);
        enemyDtos.Clear();
    }

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
    public void RegisterEnemy(ServerEnemy enemy)
    {
        enemies.Add(enemy);
        SpatialHashGrid.ServerEnemies.Insert(enemy);
    }
    public void UnregisterEnemy(ServerEnemy enemy)
    {
        enemies.Remove(enemy);
        SpatialHashGrid.ServerEnemies.Remove(enemy);
    }

}