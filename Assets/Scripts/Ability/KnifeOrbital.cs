using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;


public class KnifeOrbital : NetworkBehaviour
{

    private float rotationSpeed = 20.0f;
    [SyncVar(hook = nameof(OnOwnerAssigned))] private NetworkIdentity _owner;



    [SyncVar] private float rotation;
    [SyncVar] private int _level = 1;
    private Entity _entity;
    public override void OnStartServer()
    {
        base.OnStartServer();

        foreach (Transform item in transform)
        {
            item.GetComponentInChildren<AreaTrigger>().OnTriggerEnter += OnCollision;
        }
    }

    private void OnOwnerAssigned(NetworkIdentity old, NetworkIdentity newOwner)
    {
        
        // _entity = _owner.GetComponent<Entity>();
        // transform.SetParent(_owner.transform);
         
        // Refresh();       

        // Mirror does not replicate the server side parenting, so on the client the orbital
        // arrives as a root object and has to be moved into the client's single GameScene.
        // The server already created it inside its lobby's scene and must not touch this.
        if (NetworkServer.active) return;
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("GameScene"));
    }

    public void Init(int level, NetworkIdentity owner, KnifeAbilityData data)
    {
        _level = level > 0 ? level : 1;
        _owner = owner;
        Refresh();
        RpcSynchronizeClient(_level, owner);
    }

    [ClientRpc]
    private void RpcSynchronizeClient(int level, NetworkIdentity owner)
    {
        _level = level;
        _owner = owner;
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