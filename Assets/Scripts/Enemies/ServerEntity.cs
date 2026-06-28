using System;
using System.Collections.Generic;

using Mirror;
using UnityEngine;

public class ServerEntity : ISpatialHashGridData
{
    public string id = GUID.Generate().ToString();
    public Vector2 Position = Vector2.zero;
    private int _maxHp = 5;
    public int MaxHp { get => ApplyMaxHPMods(); private set => _maxHp = value; }
    private int _hp = 5;
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
    public bool IsDead {get { return _hp <= 0; }}
    private float _movementSpeed = 1.0f;
    public float MovementSpeed { get { return ApplyMovementSpeedMods(); } private set => _movementSpeed = value; }
    public List<DamageEvent> damageEvents = new();

    private float _cdr = 0.0f;
    public float CDR { get { return ApplyCDRMods(); } private set => _cdr = value; }
    private int _damage = 1;
    public int Damage { get { return ApplyDamageMods(); } private set => _damage = value; }
    private float _projectileSize = 1.0f;
    public float ProjectileSize { get { return ApplyProjectileSizeMods(); } set => _projectileSize = value; }
    public float AreaOfEffectSize { get; set; } = 1.0f;
    public int Pierce { get; set; } = 0;

    public int Level { get; set; } = 1;

    #region Events
    public event Action OnDeath;
    public event Action<int> OnDamageTaken;
    public event Action<int> OnHpRecovered;
    public event Action OnStatChanged;
    #endregion
    #region Modifiers
    private List<IStatModifier> maxHpModifiers = new();
    private List<IStatModifier> damageModifiers = new();
    private List<IStatModifier> movementSpeedModifiers = new();
    private List<IStatModifier> cdrModifiers = new();
    private List<IStatModifier> projectileSizeModifiers = new();
    #endregion
    private List<TemporaryEffect> temporaryEffects = new();
    protected void SetBaseData(int maxHp, float movementSpeed)
    {
        MaxHp = maxHp;
        Hp = maxHp;
        MovementSpeed = movementSpeed;
    }

    public void ReceiveDamage(DamageEvent damageEvent)
    {
        //Damage Modifiers
        if (damageEvent.amount > 0)
        {
            Hp -= damageEvent.amount;

            damageEvents.Add(damageEvent); 

        }
    }

    public void Heal(int amount)
    {
        //Heal Modifiers
        Hp += amount;
    }


    public void Update(float deltaTime)
    {

        //Temporary Effects
        // temporaryEffects.FindAll(e => e.IsComplete).ForEach(e => e.OnRemove?.Invoke());
        // temporaryEffects.RemoveAll(e => e.IsComplete);
        foreach (var effect in temporaryEffects)
        {
            effect.Update(deltaTime);
        }
    }
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

    [Server]
    public void RegisterTemporaryEffect(TemporaryEffect effect)
    {

        temporaryEffects.Add(effect);
        effect.OnApply?.Invoke();
    }

    #region Modifier Functions
    public void RegisterMaxHpModifier(IStatModifier mod)
    {
        //First Calculate Flat
        if (mod is StatModifierFlat flat)
        {
            maxHpModifiers.Insert(0, flat);
            return;
        }
        maxHpModifiers.Add(mod);
    }
    private int ApplyMaxHPMods()
    {
        var value = _maxHp;
        // maxHpModifiers.RemoveAll(x => !x.IsActive);
        foreach (var mod in maxHpModifiers)
        {
            value = (int)mod.Calculate(value);
        }
        return value;
    }
    public void RegisterDamageModifier(IStatModifier mod)
    {
        //First Calculate Flat
        if (mod is StatModifierFlat flat)
        {
            damageModifiers.Insert(0, flat);
            return;
        }
        damageModifiers.Add(mod);
    }
    private int ApplyDamageMods()
    {
        var value = _damage;
        // damageModifiers.RemoveAll(x => !x.IsActive);
        foreach (var mod in damageModifiers)
        {
            value = (int)mod.Calculate(value);
        }

        return value;
    }
    public void RegisterMovementSpeedModifier(IStatModifier mod)
    {
        //First Calculate Flat
        if (mod is StatModifierFlat flat)
        {
            movementSpeedModifiers.Insert(0, flat);
            return;
        }
        movementSpeedModifiers.Add(mod);
    }
    public float ApplyMovementSpeedMods()
    {
        var value = _movementSpeed;
        // movementSpeedModifiers.RemoveAll(x => !x.IsActive);
        foreach (var mod in movementSpeedModifiers)
        {
            value = mod.Calculate(value);
        }
        return value;
    }

    public void RegisterCDRModifier(IStatModifier mod)
    {
        if (mod is StatModifierFlat flat)
        {
            cdrModifiers.Insert(0, flat);
            return;
        }
        cdrModifiers.Add(mod);
    }

    private float ApplyCDRMods()
    {
        var value = _cdr;
        // cdrModifiers.RemoveAll(x => !x.IsActive);
        foreach (var mod in cdrModifiers)
        {
            value = mod.Calculate(value);
        }
        return value;
    }

    public void RegisterProjectileSizeModifier(IStatModifier mod)
    {
        if (mod is StatModifierFlat flat)
        {
            projectileSizeModifiers.Insert(0, flat);
            return;
        }
        projectileSizeModifiers.Add(mod);
    }

    private float ApplyProjectileSizeMods()
    {
        var value = _projectileSize;
        // projectileSizeModifiers.RemoveAll(x => !x.IsActive);
        foreach (var mod in projectileSizeModifiers)
        {
            value = mod.Calculate(value);
        }
        return value;
    }

    // ISpatialHashGridData implementation
    private Vector2Int sphCellIndex;
    public Vector2 GetPosition() => Position;
    public void SetCellKey(Vector2Int index) => sphCellIndex = index;
    public Vector2Int GetCellKey() => sphCellIndex;
    #endregion
}
