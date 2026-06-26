using System;
using Mirror;
using UnityEngine;

public class CombatTickManager : NetworkBehaviour
{
    public static event Action OnTick;
    public int combatTicksPerSecond = 8;
    private float _combatTickRate = 1f;
    private float _combatTicks = 0f;
    void Start()
    {
        if (!isServer) return;
        _combatTickRate = 1.0f / combatTicksPerSecond;

    }
    
    private void Update()
    {
        if (!isServer) return;
        _combatTicks += Time.deltaTime;
        if (_combatTicks >= _combatTickRate)
        {
            _combatTicks -= _combatTickRate;
            OnTick?.Invoke();
        }

    }
    

}