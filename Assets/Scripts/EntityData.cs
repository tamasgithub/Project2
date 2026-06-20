using System;
using System.Collections.Generic;
using Mirror;

public class EntityData
{

    public int MaxHp { get; } = 1;

    public int Hp { get; private set; }

    public float MovementSpeed { get; private set; } = 1f;
    public float CDR { get; private set; } = 0.0f;
    public int Damage { get; private set; } = 0;
    public float ProjectileSize { get; set; } = 1.0f;
    public EntityData(int maxHp, float movementSpeed, float cdr = 0.0f, int damage = 0, float projected = 1.0f)
    {
        MaxHp = maxHp;
        Hp = maxHp;
        MovementSpeed = movementSpeed;
    }


    void SetHp(int hp)
    {
        Hp = Math.Clamp(hp, 0, MaxHp);
    }
    void ReceiveDamage(int amount)
    {
        //Damage Modifiers
        SetHp(Hp - amount);
    }
    
    void Heal(int amount)
    {
        //Heal Modifiers
         SetHp(Hp + amount);
    }

}