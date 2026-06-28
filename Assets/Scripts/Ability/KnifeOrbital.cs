using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;


public class KnifeOrbital : NetworkBehaviour
{

    private float rotationSpeed = 20.0f;
    [SyncVar] private uint ownerNetId;
    [SyncVar] private float rotation;
    [SyncVar] private int _level;
    private Entity _owner;
    public override void OnStartServer()
    {
        base.OnStartServer();

        foreach (Transform item in transform)
        {
            item.GetComponentInChildren<AreaTrigger>().OnTriggerEnter += OnCollision;
        }
    }

    public override void OnStartClient()
    {
        if (NetworkClient.spawned.TryGetValue(ownerNetId, out var identity))
        {
            _owner = identity.GetComponent<Entity>();
            transform.SetParent(_owner.transform);
            Refresh();
        }

    }




    public void Init(int level, uint ownerId, KnifeAbilityData data)
    {
        this.ownerNetId = ownerId;
        _level = level;
        Refresh();

    }

    private void Refresh()
    {
        var angle = 360f / _level;
        for (int i = 0; i < _level; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
            transform.GetChild(i).eulerAngles = Vector3.forward * angle * i;
        }
    }

    void Update()
    {
        if (isServer)
        {
            transform.Rotate(Vector3.forward * rotationSpeed * Time.deltaTime);
        }

    }

    [Server]
    private void OnCollision(ServerEntity collider)
    {

        if (collider is not ServerEnemy enemy) return;

        var bleed = new TemporaryEffect(15.0f)
        .SetTickRate(0.5f)
        .SetMaxTicks(10)
        .IsBleed(enemy);

        enemy.RegisterTemporaryEffect(bleed);



    }
}