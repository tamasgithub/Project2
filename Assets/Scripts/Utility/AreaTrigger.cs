using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class AreaTrigger : NetworkBehaviour
{
    public float radius = 1.0f;
    private HashSet<ServerEntity> _inside = new();
    public event Action<ServerEntity> OnTriggerEnter;
    public event Action<ServerEntity> OnTriggerExit;

    [ServerCallback]
    void OnEnable()
    {
        TriggerTickManager.OnTick += CheckTrigger;
    }

    [ServerCallback]
    void OnDisable()
    {
        TriggerTickManager.OnTick -= CheckTrigger;
    }


    private void CheckTrigger()
    {
        var newlyEntered = new HashSet<ServerEntity>();
        var enemies = SpatialHashGrid.ServerEnemies.GetNearObjects((Vector2)transform.position, radius);
        foreach (var enemy in enemies)
        {
            if (Vector2.Distance(enemy.Position, (Vector2)transform.position) <= radius + 0.5f) //0.5f hardocded enemy hitbox
            {
                newlyEntered.Add(enemy);
                if (!_inside.Contains(enemy))
                {
                    OnTriggerEnter?.Invoke(enemy);
                    
                }
            }
        }

        foreach (var current in _inside)
        {
            if (!newlyEntered.Contains(current))
            {
                OnTriggerExit?.Invoke(current);
            }
        }

        _inside.Clear();
        _inside.UnionWith(newlyEntered);

    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, radius);
    }
#endif

}