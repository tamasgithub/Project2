using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class Enemy : Entity
{
    public GameObject loot;

    
    private Rigidbody2D rb;
    private List<GameObject> players;

    private int maxHp = 5;
    private float movementSpeed = 2f;

    private float lastDecayTime;

    void Start()
    {
        SetBaseData(maxHp, movementSpeed);
        rb = GetComponent<Rigidbody2D>();
        players = GameObject.FindGameObjectsWithTag("Player").ToList();
        SurvivorNetworkManager.PlayerJoined += (conn) => players.Add(conn.identity.gameObject);
        SurvivorNetworkManager.PlayerLeft += (conn) => players.Remove(conn.identity.gameObject);

        lastDecayTime = Time.time;
        OnDeath += () => OnKilled();
    }

    // Update is called once per frame
    void Update()
    {
        if (!authority) return;

        if (Time.time - lastDecayTime > 1)
        {
            ReceiveDamage(1);
        }
        Transform targetPos = FindNearestPlayerPos();
        if (targetPos  == null ) return;
        rb.MovePosition(transform.position + (targetPos.position - transform.position).normalized * Data.MovementSpeed* Time.deltaTime);


    }

    private Transform FindNearestPlayerPos()
    {
        Transform nearestTarget = null;
        float smallestDistance = float.MaxValue;
        foreach (GameObject player in players)
        {
            if (nearestTarget == null || Vector2.Distance(transform.position, player.transform.position) < smallestDistance) {
                smallestDistance = Vector2.Distance(transform.position, player.transform.position);
                nearestTarget = player.transform;
            }
        }
        return nearestTarget;
    }

    [Server]
    private void OnKilled()
    {
        GameObject newLoot = Instantiate(loot, transform.position, Quaternion.identity);
        NetworkServer.Spawn(newLoot);
        NetworkServer.Destroy(this.gameObject);
    }
}
