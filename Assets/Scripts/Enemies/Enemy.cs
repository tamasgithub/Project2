using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    public float speed = 1f;
    public float lifeTime = 5f;
    public GameObject loot;
    
    private Rigidbody2D rb;
    private float spawnTime;
    List<GameObject> players;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spawnTime = Time.time;
        players = GameObject.FindGameObjectsWithTag("Player").ToList();
        SurvivorNetworkManager.PlayerJoined += (conn) => players.Add(conn.identity.gameObject);
        SurvivorNetworkManager.PlayerLeft += (conn) => players.Remove(conn.identity.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (!authority) return;

        if (Time.time - spawnTime > lifeTime)
        {
            OnKilled();
        }

        Transform targetPos = FindNearestPlayerPos();
        if (targetPos  == null ) return;
        rb.MovePosition(transform.position + (targetPos.position - transform.position).normalized * speed * Time.deltaTime);


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
