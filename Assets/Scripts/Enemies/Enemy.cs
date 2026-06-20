using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;

public class Enemy : Entity
{
    public GameObject loot;


    private Rigidbody2D rb;
    private List<GameObject> players;

    private int maxHp = 5;
    private float movementSpeed = 2f;

    private float lastDecayTime;
    private TextMeshProUGUI hpText;

    public override void OnStartServer()
    {

        SetBaseData(maxHp, movementSpeed);
        rb = GetComponent<Rigidbody2D>();
        players = GameObject.FindGameObjectsWithTag("Player").ToList();


        lastDecayTime = Time.time;
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


    // Update is called once per frame
    void Update()
    {
        if (!authority) return;

        if (Time.time - lastDecayTime > 1)
        {
            ReceiveDamage(1);
            lastDecayTime = Time.time;
        }
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
        GameObject newLoot = Instantiate(loot, transform.position, Quaternion.identity);
        NetworkServer.Spawn(newLoot);
        NetworkServer.Destroy(this.gameObject);
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
