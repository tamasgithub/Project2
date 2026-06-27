using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class EnemyManager : NetworkBehaviour
{
    public static EnemyManager Instance;
    private List<GameObject> players;
    private List<ServerEnemy> enemies = new();
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
        if(_tick >= _tickRate)
        {
            UpdatePositions(_tick);
            SendSnapShot();
            _tick -= _tickRate;       
        }
        
    }

    private void UpdatePositions(float deltaTime)
    {
        var targetPos = (Vector2)FindNearestPlayerPos().position;
        if (targetPos == null) return;

        foreach (var enemy in enemies)
        {
            enemy.Position = enemy.Position + (targetPos - enemy.Position).normalized * enemy.MovementSpeed * deltaTime;
            SpatialHashGrid.ServerEnemies?.Update(enemy);
        }
    }
    
    private void SendSnapShot()
    {
        var msg = new EnemySnapshot()
        {
            enemies = enemies
        };
        NetworkServer.SendToAll(msg);
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
    }
    public void UnregisterEnemy(ServerEnemy enemy)
    {
        enemies.Remove(enemy);
    }

}