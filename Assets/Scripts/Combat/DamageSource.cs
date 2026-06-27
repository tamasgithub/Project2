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
    [ServerCallback]
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPlayer && collision.tag == "Enemy")
        {
            if (collision.TryGetComponent<Enemy>(out var e))
            {
                targets.Add(e);
            }
        }

        else if (!isPlayer && collision.tag == "Player")
        {
            if (collision.TryGetComponent<Player>(out var p))
            {
                targets.Add(p);
            }
        }
    }
    [ServerCallback]
    void OnTriggerExit2D(Collider2D collision)
    {
        if (isPlayer && collision.tag == "Enemy")
        {
            if (collision.TryGetComponent<Enemy>(out var e))
            {
                targets.Remove(e);
            }
        }
        else if (!isPlayer && collision.tag == "Player")
        {
            if (collision.TryGetComponent<Player>(out var p))
            {
                targets.Remove(p);
            }
        }
    }
    [Server]
    private void DealDamage()
    {
        // Calculate
        var dmg = _owner.Damage;
        if(isPlayer){
        foreach (var target in targets)
        {
                target?.ReceiveDamage(dmg);
            }
        }
    }

}