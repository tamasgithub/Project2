using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;


public class KnifeOrbital : NetworkBehaviour
{

    private float rotationSpeed = 20.0f;
    [SyncVar] private NetworkIdentity _owner;
    [SyncVar(hook = nameof(OnLevelChanged))] private int _level = 1;
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
        // The knives are inactive in the prefab and only Refresh switches them on. Mirror runs
        // no SyncVar hook for the spawn payload, so the level has to be applied once here --
        // this is why the knives were never visible on any client.
        Refresh();

        // Mirror does not replicate the server side parenting, so on the client the orbital
        // arrives as a root object and has to be moved into the client's single GameScene.
        // The server already created it inside its lobby's scene and must not touch this.
        if (NetworkServer.active) return;
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("GameScene"));
    }

    private void OnLevelChanged(int _, int level)
    {
        Refresh();
    }

    public void Init(int level, NetworkIdentity owner, KnifeAbilityData data)
    {
        // Runs before NetworkServer.Spawn, so these values travel in the spawn payload.
        // An Rpc here used to be dropped for exactly that reason: the object was not spawned yet.
        _level = level > 0 ? level : 1;
        _owner = owner;
        Refresh();
    }
    private void Refresh()
    {
        var angle = 360f / Mathf.Max(1, _level);
        for (int i = 0; i < transform.childCount; i++)
        {
            bool used = i < _level;
            transform.GetChild(i).gameObject.SetActive(used);
            if (used)
            {
                transform.GetChild(i).eulerAngles = Vector3.forward * angle * i;
            }
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

        // The knives used to ignore the player's stats entirely, so a DAMAGE upgrade did
        // nothing for them.
        Entity owner = _owner != null ? _owner.GetComponent<Entity>() : null;
        int ownerDamage = owner != null ? owner.Damage : 0;

        var bleed = new TemporaryEffect(15.0f)
        .SetTickRate(0.5f)
        .SetMaxTicks(10)
        .IsBleed(enemy, ownerDamage);

        enemy.RegisterTemporaryEffect(bleed);



    }
}