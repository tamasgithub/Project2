using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;


public class KnifeOrbital : NetworkBehaviour
{

    private float rotationSpeed = 20.0f;
    [Server]
    void Start()
    {
        foreach (Transform item in transform)
        {
            item.GetComponent<CollisionForwarder>().OnTriggerEnter = OnCollision;
        }
    }
    public void Refresh(int level, Entity entity, KnifeAbilityData data)
    {

        var angle = 360f / level;
        for (int i = 0; i < level; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            transform.GetChild(i).eulerAngles = Vector3.forward * angle * i;
        }
    }

    void Update()
    {
        if (!isServer) return;
        transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
    }

    [Server]
    private void OnCollision(Collider2D collider)
    {
        if (collider.tag == "Enemy")
        {
            var enemy = collider.GetComponent<Enemy>();
            var bleed = new TemporaryEffect(15.0f)
            .SetTickRate(0.5f)
            .SetMaxTicks(2)
            .IsBleed(enemy);
            
            enemy.RegisterTemporaryEffect(bleed);
        }


    }
}