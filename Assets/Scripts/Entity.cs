using System;
using System.Runtime.InteropServices.WindowsRuntime;
using Mirror;
using UnityEngine;

public class Entity : NetworkBehaviour
{

    [SyncVar]
    private int _maxHp;
    public int MaxHp { get =>  _maxHp; private set =>  _maxHp = value; } 
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
    [SyncVar]
    private float _movementSpeed = 1.0f;
    public float MovementSpeed { get => _movementSpeed; private set => _movementSpeed = value; }
    public float CDR { get; private set; } = 0.0f;
    public int Damage { get; private set; } = 0;
    public float ProjectileSize { get; set; } = 1.0f;

    public event Action OnDeath;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHpRecovered;
    public event Action OnStatChanged;

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
        OnStatChanged?.Invoke();
    }
}
