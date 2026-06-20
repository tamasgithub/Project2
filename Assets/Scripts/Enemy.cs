using System;
using Mirror;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float speed = 1f;
    public GameObject[] players;

    private Rigidbody2D rb;

    void Start()
    {
        players = GameObject.FindGameObjectsWithTag("Player");
        rb = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        //if (!authority) return;
        Transform targetPos = FindNearestPlayerPos();
        rb.MovePosition(transform.position + (targetPos.position - transform.position).normalized * speed * Time.deltaTime);
    }

    private Transform FindNearestPlayerPos()
    {
        Transform nearestTarget = null;
        float smallestDistance = float.MaxValue;
        foreach (GameObject player in players)
        {
            if (nearestTarget != null || Vector2.Distance(transform.position, player.transform.position) < smallestDistance) {
                smallestDistance = Vector2.Distance(transform.position, player.transform.position);
                nearestTarget = player.transform;
            }
        }
        return nearestTarget;
    }
}
