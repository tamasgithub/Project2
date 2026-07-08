
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Mirror;
using NUnit.Framework;
using UnityEngine;


public class ChakramOrbital : NetworkBehaviour
{
    [SyncVar] private NetworkIdentity _identity;
    [SyncVar (hook = nameof(OnChakramCountChanged))] private int _chakramCount;
    [SyncVar] private ChakramState state = ChakramState.ORBIT;
    private SyncList<Vector3> chakramPositions = new();
    private List<Vector3> offset = new List<Vector3>()
    {

    };
    private SyncList<Vector3> hoverPositions = new SyncList<Vector3>();
    private List<float> _returnDelays = new();

    void Start()
    {
        if (!isServer) return;
        foreach (Transform child in transform)
        {
            chakramPositions.Add(new Vector3());
        }

        SetupCollision();
    }

    public void Init(NetworkIdentity identity, int chakramCount)
    {
        _identity = identity;
        _chakramCount = chakramCount;
        var fraction = 360.0f / chakramCount;
        offset.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i < chakramCount);
            offset.Add(Vector2.up.Rotate(fraction * i));
        }
       
    }
    public void OnChakramCountChanged(int old, int count)
    {
        offset.Clear();
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(i < _chakramCount);
        }
    }


    [Server]
    private void SetupCollision()
    {
        foreach (Transform child in transform)
        {
            child.Find("Collider").GetComponent<AreaTrigger>().OnTriggerEnter += OnHit;
            child.Find("Collider").GetComponent<DamageSource>().Load(_identity.GetComponent<Entity>());

        }
    }

    [Server]
    private void OnHit(ServerEntity collision)
    {
        if (collision is not ServerEnemy enemy) return;
        enemy.ReceiveDamage(new DamageEvent(3));
        if (state == ChakramState.ORBIT)
        {

            DetachChakrams((Vector3)enemy.Position);
            return;
        }
    }
    [Server]
    private void DetachChakrams(Vector3 target)
    {

        hoverPositions.Clear();
        for (int i = 0; i < _chakramCount; i++)
        {
            var direction = (chakramPositions[i] - target) * 2;
            hoverPositions.Add(target - direction * 2);
        }
        state = ChakramState.DETACH;
    }

    private void MoveToHoverPos()
    {
        var complete = true;
        for (int i = 0; i < _chakramCount; i++)
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
        for (int i = 0; i < _chakramCount; i++)
        {
            _returnDelays[i] -= Time.deltaTime;
            Debug.Log($"Delay{i} {_returnDelays[i]}");
            if (_returnDelays[i] > 0) return;

            chakramPositions[i] = Vector3.MoveTowards(chakramPositions[i], _identity.transform.position + offset[i], Time.deltaTime * 50f);
            complete = (Vector3.Distance(chakramPositions[i], _identity.transform.position + offset[i]) <= 0.2f) && complete;

        }
        if (complete)
        {
            state = ChakramState.ORBIT;
        }


    }

    float delay = 0;
    private void Update()
    {
        Debug.Log(state);
        if (isServer)
        {
            switch (state)
            {

                case ChakramState.ORBIT:
                    for (int i = 0; i < _chakramCount; i++)
                    {
                        chakramPositions[i] = _identity.transform.position + offset[i];

                    }
                    break;
                case ChakramState.DETACH:
                    MoveToHoverPos();
                    delay = 0;
                    break;
                case ChakramState.HOVER:
                    delay += Time.deltaTime;
                    if (delay >= 3.0f)
                    {
                        state = ChakramState.RETURN;
                        _returnDelays.Clear();
                        var counter = 0;
                        foreach (var item in chakramPositions)
                        {
                            _returnDelays.Add(0.2f * counter);
                            counter++;
                        }
                    }

                    break;
                case ChakramState.RETURN:
                    ReturnToOwner();
                    break;

            }


        }


        switch (state)
        {

            default:
                for (int i = 0; i < _chakramCount; i++)
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