using System;
using Mirror;
using UnityEngine;

// One per game scene: the tick is per lobby, so the event must not be static.
public class CombatTickManager : NetworkBehaviour
{
    public event Action OnTick;
    public int combatTicksPerSecond = 8;
    private float _combatTickRate = 1f;
    private float _combatTicks = 0f;

    [ServerCallback]
    void Start()
    {
        _combatTickRate = 1.0f / 2;
    }

    [ServerCallback]
    private void Update()
    {
        _combatTicks += Time.deltaTime;
        if (_combatTicks >= _combatTickRate)
        {
            _combatTicks -= _combatTickRate;
            OnTick?.Invoke();
        }
    }
}
