using System;
using Mirror;
using UnityEngine;

public class CollisionTrigger : NetworkBehaviour
{
    public float radius = 1.0f;
    public event Action<ServerEntity> OnCollisionEnter;

    [ServerCallback]
    void OnEnable()
    {
        CombatTickManager.OnTick += CheckTrigger;
    }

    [ServerCallback]
    void OnDisable()
    {
        CombatTickManager.OnTick -= CheckTrigger;
    }


    private void CheckTrigger()
    {
        var enemies = SpatialHashGrid.ServerEnemies.GetNearObjects((Vector2)transform.position, radius);
        foreach (var enemy in enemies)
        {
            if (Vector2.Distance(enemy.Position, (Vector2)transform.position) <= radius + 0.5f) //0.5f hardocded enemy hitbox
            {

                OnCollisionEnter?.Invoke(enemy);
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, radius);
    }
#endif

}