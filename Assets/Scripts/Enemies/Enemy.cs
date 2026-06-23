using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Mirror;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Enemy : Entity, ISpatialHashGridData
{
    public List<LootTableEntry> lootTable;
    public List<LootPrefabs> lootPrefabs;

    private Rigidbody2D rb;
    private List<GameObject> players;

    private int maxHp = 1;
    private float movementSpeed = 2f;

    [SerializeField]
    private Canvas canvas;
    [SerializeField]
    public Image hpBar;
    private Vector2Int sphCellIndex;

    public override void OnStartServer()
    {
        SetBaseData(maxHp + Level * 1, movementSpeed + Level * 0.1f);
        rb = GetComponent<Rigidbody2D>();
        players = GameObject.FindGameObjectsWithTag("Player").ToList();

        OnserverSubscribeToEvents();
        SpatialHashGrid.Enemies.Insert(this);

        // destroy UI on server
        if (isServerOnly)
        {
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
        OnDamageTaken += UpdateHpUI;
    }

    // ISpatialHashGridData implementation
    public Vector2 GetPosition() => transform.position;
    public void SetCellKey(Vector2Int index) => sphCellIndex = index;
    public Vector2Int GetCellKey() => sphCellIndex;

    protected override void Update()
    {
        base.Update();
        if (!isServer) return;
        Transform targetPos = FindNearestPlayerPos();
        if (targetPos == null) return;
        rb.MovePosition(transform.position + (targetPos.position - transform.position).normalized * MovementSpeed * Time.deltaTime);
        SpatialHashGrid.Enemies.Update(this);
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
        SpatialHashGrid.Enemies.Remove(this);

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

    Tween hitflash;
    [Client]
    private void UpdateHpUI(int _)
    {
        canvas.gameObject.SetActive(true);

        hpBar.fillAmount = Mathf.Clamp01((float)Hp / MaxHp);
        
        DOTween.Kill(hitflash);
        var renderer = GetComponent<SpriteRenderer>();
        renderer.color = Color.white;
        hitflash = DOTween.To(() => renderer.color, (col) => renderer.color = col, Color.red, 0.2f);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isServer) return;
        if (collision.collider.tag == "Player")
        {
            collision.gameObject.GetComponent<Entity>().ReceiveDamage(1);
        }
    }
}

[System.Serializable]
public struct LootTableEntry
{
    public Loot.LootType LootType;
    public float probability;
}

[System.Serializable]
public struct LootPrefabs
{
    public Loot.LootType LootType;
    public GameObject prefab;
}
