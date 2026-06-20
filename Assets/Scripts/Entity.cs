using System;
using Mirror;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    public int MaxHp { get; private set; } = 1;

    public int Hp { get; private set; }

    public float MovementSpeed { get; private set; } = 1f;
    public float CDR { get; private set; } = 0.0f;
    public int Damage { get; private set; } = 0;
    public float ProjectileSize { get; set; } = 1.0f;

    public static event Action OnDeath;


    public void SetHp(int hp)
    {
        Hp = Math.Clamp(hp, 0, MaxHp);
        if (Hp == 0)
        {
            OnDeath.Invoke();
        }
    }

    protected void SetBaseData(int maxHp, float movementSpeed)
    {
        MaxHp = maxHp;
        Hp = maxHp;
        MovementSpeed = movementSpeed;
    }

    public void ReceiveDamage(int amount)
    {
        //Damage Modifiers
        SetHp(Hp - amount);
    }

    public void Heal(int amount)
    {
        //Heal Modifiers
        SetHp(Hp + amount);
    }
}
