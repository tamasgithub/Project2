
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Mirror;
using NUnit.Framework;
using UnityEngine;


public class ChakramOrbital : NetworkBehaviour
{

    [SyncVar] private ChakramState state = ChakramState.ORBIT;
    [SyncVar] private uint ownerNetId;
    private SyncList<Vector3> chakramPositions = new();

    private List<Vector3> offset = new List<Vector3>()
    {
        Vector3.up,
        Vector3.down,
        Vector3.left,
        Vector3.right,
    };
    private SyncList<Vector3> hoverPositions = new SyncList<Vector3>();
    private List<float> _returnDelays = new();
    private Entity _owner;
    private SyncList<Transform> _chakrams = new();
    private int _detachCount = 0;
    private int _returnCount = 0;
    
    void Start()
    {
        if (!isServer) return;
        foreach (Transform child in transform)
        {
            _chakrams.Add(child);
            chakramPositions.Add(new Vector3());
        }
        SetupCollision();
    }

    [Server]
    private void SetupCollision()
    {
        foreach (Transform child in transform)
        {
            child.Find("Collider").GetComponent<CollisionForwarder>().OnTriggerEnter += OnHit;
            child.Find("Collider").GetComponent<DamageSource>().Load(_owner);

        }
    }

    [Server]
    private void OnHit(Collider2D collision)
    {
        if (collision.tag != "Enemy") return;
        var enemy = collision.GetComponent<Enemy>();
        enemy.ReceiveDamage(3);
        if (state == ChakramState.ORBIT)
        {

            DetachChakrams(enemy.transform);
            return;
        }
    }
    [Server]
    private void DetachChakrams(Transform target)
    {
        _returnCount = 0;
        _detachCount = 0;
        var index = 0;
        hoverPositions = new();
        foreach (var chakram in _chakrams)
        {
            var direction = (chakram.position - target.position) * 2;
            hoverPositions.Add(target.position - direction * 2);
            chakramPositions[index] = chakram.position;
            _detachCount++;
            index++;
        }
        state = ChakramState.DETACH;
    }
    public override void OnStartClient()
    {
        if (NetworkClient.spawned.TryGetValue(ownerNetId, out var identity))
            _owner = identity.GetComponent<Entity>();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (NetworkClient.spawned.TryGetValue(ownerNetId, out var identity))
            _owner = identity.GetComponent<Entity>();
    }
    private void MoveToHoverPos()
    {
        var complete = true;
        for (int i = 0; i < chakramPositions.Count; i++)
        {
            chakramPositions[i] = Vector3.MoveTowards(chakramPositions[i], hoverPositions[i], Time.deltaTime * 50f);
            complete = (Vector3.Distance(chakramPositions[i], hoverPositions[i]) <= 0.1f) && complete;
        }
        if (complete)
        {
            state = ChakramState.HOVER;
        }


    }
    private void ReturnToOwner()
    {
        var complete = true;
        for (int i = 0; i < chakramPositions.Count; i++)
        {
            _returnDelays[i] -= Time.deltaTime;
            if (_returnDelays[i] >= 0) return;

            chakramPositions[i] = Vector3.MoveTowards(chakramPositions[i], _owner.transform.position + offset[i], Time.deltaTime * 50f);
            complete = (Vector3.Distance(chakramPositions[i], _owner.transform.position + offset[i]) <= 0.1f) && complete;

        }
        if (complete)
        {
            state = ChakramState.ORBIT;
        }


    }

    public void Init(int level, uint ownerId)
    {
        ownerNetId = ownerId;
    }

    float delay = 0;
    private void Update()
    {
        if (isServer)
        {
            switch (state)
            {

                case ChakramState.DETACH:
                    MoveToHoverPos();
                    delay = 0;
                    break;
                case ChakramState.HOVER:
                    delay += Time.deltaTime;
                    if (delay >= 3.0f)
                    {
                        state = ChakramState.RETURN;
                        _returnDelays = new();
                        var counter = 0;
                        foreach (var item in chakramPositions)
                        {
                            _returnDelays.Add(0.4f * counter);
                            counter++;
                        }
                    }

                    break;
                case ChakramState.RETURN:
                    ReturnToOwner();
                    break;

            }
            

        }

        if (!isClient) return;
        switch (state)
        {
            case ChakramState.ORBIT:
                for (int i = 0; i < _chakrams.Count; i++)
                {
                    transform.GetChild(i).transform.position = _owner.transform.position + offset[i];
                    
                }
                break;
            default:
                for (int i = 0; i < transform.childCount; i++)
                {
                    transform.GetChild(i).transform.position = chakramPositions[i];
                }
                break;
        }

    }
    enum ChakramState
    {
        ORBIT,
        DETACH,
        HOVER,
        RETURN
    }

}