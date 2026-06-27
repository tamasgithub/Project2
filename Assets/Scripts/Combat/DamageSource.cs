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
    // [ServerCallback]
    // void OnTriggerEnter2D(Collider2D collision)
    // {
    //     if (isPlayer && collision.tag == "Enemy")
    //     {
    //         if (collision.TryGetComponent<Enemy>(out var e))
    //         {
    //             targets.Add(e);
    //         }
    //     }

    //     else if (!isPlayer && collision.tag == "Player")
    //     {
    //         if (collision.TryGetComponent<Player>(out var p))
    //         {
    //             targets.Add(p);
    //         }
    //     }
    // }
    // [ServerCallback]
    // void OnTriggerExit2D(Collider2D collision)
    // {
    //     if (isPlayer && collision.tag == "Enemy")
    //     {
    //         if (collision.TryGetComponent<Enemy>(out var e))
    //         {
    //             targets.Remove(e);
    //         }
    //     }
    //     else if (!isPlayer && collision.tag == "Player")
    //     {
    //         if (collision.TryGetComponent<Player>(out var p))
    //         {
    //             targets.Remove(p);
    //         }
    //     }
    // }
    [Server]
    private void DealDamage()
    {
        // Calculate
        var enemies = SpatialHashGrid.ServerEnemies.GetNearObjects((Vector2)transform.position, radius);
        // var dmg = _owner.Damage;
        foreach (var enemy in enemies)
        {
            if (Vector2.Distance(enemy.Position, (Vector2)transform.position) <= radius + 0.5f) //0.5f hardocded enemy hitbox
            {
                Debug.Log("Hit");
                var dmg = new DamageEvent();
                dmg.amount = 2;
                enemy.damageEvents.Add(dmg);
                // PoolableObject dmgNr = ObjectPool.Instance.Get(PoolableObjectType.DMG_NR, (Vector3)enemy.Position, Quaternion.identity);
                // dmgNr.GetComponent<DamageNumber>().SetDamage(1, true);
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