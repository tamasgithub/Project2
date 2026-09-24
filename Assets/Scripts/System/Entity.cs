using System;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Mirror;
using UnityEngine;

public class Entity : NetworkBehaviour
{

    // The modifier lists live on the server only, so the client cannot fold them itself.
    // What is replicated is therefore the finished value, not the base one.
    [SyncVar(hook = nameof(OnMaxHpChanged))]
    private int _maxHp;
    private int _baseMaxHp;
    public int MaxHp => _maxHp;
    [SyncVar(hook = nameof(OnHpChanged))]
    private int _hp;
    public int Hp
    {
        get
        {
           return  _hp;
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
    public float MovementSpeed { get {return ApplyMovementSpeedMods();}  private set => _movementSpeed = value; }
    private float _cdr = 0.0f;
    public float CDR { get{ return ApplyCDRMods(); } private set => _cdr = value; }
    private int _damage = 1;
    public int Damage { get { return ApplyDamageMods(); } private set => _damage = value; }
    private float _projectileSize = 1.0f;
    public float ProjectileSize { get { return ApplyProjectileSizeMods(); } set => _projectileSize = value; }
    private float _areaOfEffectSize = 1.0f;
    public float AreaOfEffectSize { get { return ApplyAreaOfEffectSizeMods(); } private set => _areaOfEffectSize = value; }
    private int _pierce = 0;
    public int Pierce { get { return ApplyPierceMods(); } private set => _pierce = value; }
	
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
    private List<IStatModifier> areaOfEffectSizeModifiers = new();
    // Pierce counts projectile hits, so it is integral by nature: no IStatModifier, which
    // works in float and would only invite a percentage that cannot mean anything here.
    private List<int> pierceModifiers = new();
    #endregion
    private List<TemporaryEffect> temporaryEffects = new();
    protected void SetBaseData(int maxHp, float movementSpeed)
    {
        _baseMaxHp = maxHp;
        RecalculateMaxHp();
        Hp = MaxHp;
        MovementSpeed = movementSpeed;
    }

    /// <summary>Folds the max hp modifiers into the replicated value. Server only.</summary>
    [Server]
    private void RecalculateMaxHp()
    {
        _maxHp = ApplyMaxHPMods();
    }

    [Client]
    private void OnMaxHpChanged(int _, int __)
    {
        // Without this the hp bars would keep their old denominator until the next hit.
        OnStatChanged?.Invoke();
    }

    public void ReceiveDamage(int amount)
    {
        //Damage Modifiers
        
        Hp -= amount;

        ObjectPool pool = GameContext.For(this)?.ObjectPool;
        PoolableObject dmgNr = pool != null
            ? pool.Get(PoolableObjectType.DMG_NR, transform.position, Quaternion.identity)
            : null;
        if (dmgNr != null)
        {
            dmgNr.GetComponent<DamageNumber>().SetDamage(amount, this is Player);
        }
    }

    public void Heal(int amount)
    {
        //Heal Modifiers
        Hp += amount;
    }

    [ServerCallback]
    protected virtual void Update()
    {
        if (!isServer) return;
        //Temporary Effects
        // temporaryEffects.FindAll(e => e.IsComplete).ForEach(e => e.OnRemove?.Invoke());
        // temporaryEffects.RemoveAll(e => e.IsComplete);
        foreach (var effect in temporaryEffects)
        {
            effect.Update(Time.deltaTime);
        }
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
        }
        else
        {
            maxHpModifiers.Add(mod);
        }

        RecalculateMaxHp();
    }
    private int ApplyMaxHPMods()
    {
        var value = _baseMaxHp;
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

    public void RegisterAreaOfEffectSizeModifier(IStatModifier mod)
    {
        if (mod is StatModifierFlat flat)
        {
            areaOfEffectSizeModifiers.Insert(0, flat);
            return;
        }
        areaOfEffectSizeModifiers.Add(mod);
    }

    private float ApplyAreaOfEffectSizeMods()
    {
        var value = _areaOfEffectSize;
        foreach (var mod in areaOfEffectSizeModifiers)
        {
            value = mod.Calculate(value);
        }
        return value;
    }

    public void RegisterPierceModifier(int amount)
    {
        pierceModifiers.Add(amount);
    }

    private int ApplyPierceMods()
    {
        var value = _pierce;
        foreach (int mod in pierceModifiers)
        {
            value += mod;
        }
        return value;
    }
    #endregion 
}
