using System;
using Mirror;
using UnityEngine;

// One per game scene: the tick is per lobby, so the event must not be static.
public class TriggerTickManager : NetworkBehaviour
{
    public event Action OnTick;
    public int collisionTicksPerSecond = 32;
    private float _collisionTickRate = 1f;
    private float _collisionTicks = 0f;

    [ServerCallback]
    void Start()
    {
        _collisionTickRate = 1.0f / GlobalConstants.TRIGGER_CHECK_RATE;
    }

    [ServerCallback]
    private void Update()
    {
        _collisionTicks += Time.deltaTime;
        if (_collisionTicks >= _collisionTickRate)
        {
            _collisionTicks -= _collisionTickRate;
            OnTick?.Invoke();
        }
    }
}
