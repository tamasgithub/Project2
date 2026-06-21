using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

public class Enemy : Entity
{
    public List<LootTableEntry> lootTable;
    public List<LootPrefabs> lootPrefabs;

    private Rigidbody2D rb;
    private List<GameObject> players;

    private int maxHp = 1;
    private float movementSpeed = 2f;

    private TextMeshProUGUI hpText;

    public override void OnStartServer()
    {
        SetBaseData(maxHp + Level * 1, movementSpeed + Level * 0.1f);
        rb = GetComponent<Rigidbody2D>();
        players = GameObject.FindGameObjectsWithTag("Player").ToList();

        OnserverSubscribeToEvents();

        // destroy UI on server
        if (isServerOnly)
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            Destroy(canvas.gameObject);
        }
    }

    [Server]
    private void OnserverSubscribeToEvents()
    {
        SurvivorNetworkManager.PlayerJoined += (conn) => players.Add(conn.identity.gameObject);
        SurvivorNetworkManager.PlayerLeft += (conn) => players.Remove(conn.identity.gameObject);
        OnDeath += OnKilled;
    }

    public override void OnStartClient()
    {
        Canvas canvas = GetComponentInChildren<Canvas>();
        hpText = canvas.transform.GetComponentInChildren<TextMeshProUGUI>();
        hpText.text = ""; 
        OnDamageTaken += UpdateHpUI;
    }


    [Server]
    void Update()
    {
        Transform targetPos = FindNearestPlayerPos();
        if (targetPos == null) return;
        rb.MovePosition(transform.position + (targetPos.position - transform.position).normalized * MovementSpeed * Time.deltaTime);
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

    [Server]
    private void OnKilled()
    {
        OnDeath -= OnKilled;

        SpawnRandomLoot();

        NetworkServer.Destroy(this.gameObject);
    }

    [Server]
    private void SpawnRandomLoot()
    {
        float roll = Random.Range(0f, GetTotalProbability());
        float cumulative = 0f;

        foreach (var entry in lootTable)
        {
            cumulative += entry.probability;

            if (roll <= cumulative)
            {
                GameObject prefab = lootPrefabs
                    .Find(p => p.LootType == entry.LootType)
                    .prefab;

                GameObject loot = Instantiate(prefab, transform.position, Quaternion.identity);
                NetworkServer.Spawn(loot);
                return;
            }
        }
    }

    private float GetTotalProbability()
    {
        float sum = 0f;
        foreach (var entry in lootTable)
            sum += entry.probability;

        return sum;
    }

    [Client]
    private void UpdateHpUI(int _)
    {
        hpText.text = Hp + "/" + MaxHp;
    }

    [Server]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Player")
        { 
            collision.gameObject.GetComponent<Entity>().ReceiveDamage(1);
        }
    }
}

[System.Serializable]
public struct LootTableEntry {
    public Loot.LootType LootType;
    public float probability;
}

[System.Serializable]
public struct LootPrefabs
{
    public Loot.LootType LootType;
    public GameObject prefab;
}
