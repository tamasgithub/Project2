using System.Collections.Generic;
using System.Linq;
using Mirror;
using Mirror.BouncyCastle.Asn1.X509;
using UnityEngine;
public class DamageSource : NetworkBehaviour
{
    private Entity _owner;
    private bool isPlayer;
    private HashSet<Entity> targets = new();
    public float radius = 1.0f;

    public void Load(Entity owner)
    {
        _owner = owner;
        isPlayer = _owner is Player;
    }

    [ServerCallback]
    void OnEnable()
    {
        CombatTickManager.OnTick += DealDamage;
    }

    [ServerCallback]
    void OnDisable()
    {
        CombatTickManager.OnTick -= DealDamage;
    }
    
    [Server]
    private void DealDamage()
    {
        // Calculate
        var enemies = SpatialHashGrid.ServerEnemies.GetNearObjects((Vector2)transform.position, 2f);
        // var dmg = _owner.Damage;
        foreach (var enemy in enemies)
        {
            if (Vector2.Distance(enemy.Position, (Vector2)transform.position) <= radius + 0.5f) //0.5f hardocded enemy hitbox
            {
                enemy.ReceiveDamage(new DamageEvent(2));
            }
        }
        // if (isPlayer)
        // {
        //     foreach (var target in targets)
        //     {
        //         target?.ReceiveDamage(dmg);
        //     }
        // }
    }
#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.forward, radius);
    }
#endif

}