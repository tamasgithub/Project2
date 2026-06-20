using System;
using Mirror;
using UnityEngine;

public class Entity : NetworkBehaviour
{
    public int MaxHp { get; private set; } = 1;

    [SyncVar(hook = nameof(OnHpChanged))]
    private int _hp;
    public int Hp
    {
        get
        {
            return _hp;
        }
        private set
        {
            _hp = Math.Clamp(value, 0, MaxHp);
            if (_hp == 0)
            {
                OnDeath?.Invoke();
            }
        }
    }

    public float MovementSpeed { get; private set; } = 1f;
    public float CDR { get; private set; } = 0.0f;
    public int Damage { get; private set; } = 0;
    public float ProjectileSize { get; set; } = 1.0f;

    public event Action OnDeath;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHpRecovered;


    protected void SetBaseData(int maxHp, float movementSpeed)
    {
        MaxHp = maxHp;
        Hp = maxHp;
        MovementSpeed = movementSpeed;
    }

    public void ReceiveDamage(int amount)
    {
        //Damage Modifiers
        Hp -= amount;
    }

    public void Heal(int amount)
    {
        //Heal Modifiers
        Hp += amount;
    }

    [Client]
    private void OnHpChanged(int hpOld, int hpNew)
    {
        if (hpNew < hpOld)
        {
            OnDamageTaken?.Invoke(hpOld - hpNew);
        }
        else if (hpNew > hpOld)
        {
            OnHpRecovered?.Invoke(hpNew - hpOld);
        }
    }
}
