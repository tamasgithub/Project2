using System;
using Mirror;
using UnityEngine;

public class TriggerTickManager : NetworkBehaviour
{
    public static event Action OnTick;
    public int collisionTicksPerSecond = 32;
    private float _collisionTickRate = 1f;
    private float _collisionTicks = 0f;
    void Start()
    {
        if (!isServer) return;
        _collisionTickRate = 1.0f / collisionTicksPerSecond;

    }
    
    private void Update()
    {
        if (!isServer) return;
        _collisionTicks += Time.deltaTime;
        if (_collisionTicks >= _collisionTickRate)
        {
            _collisionTicks -= _collisionTickRate;
            OnTick?.Invoke();
        }

    }
    

}