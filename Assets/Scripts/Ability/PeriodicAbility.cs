
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public abstract class PeriodicAbility : Ability
{
    protected PeriodicAbility(AbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {
    }

    public float AbilityCoolDown { get; protected set; } = 0;
    public float CurrentCoolDown { get; protected set; } = 0;
    public bool IsReady {get { return CurrentCoolDown >= AbilityCoolDown; }} 
   
    public virtual void Update(float deltaTime)
    {
        CurrentCoolDown += deltaTime;
        if(CurrentCoolDown >= AbilityCoolDown)
        {
            CurrentCoolDown -= AbilityCoolDown;
            Cast();
        }
    }

    public abstract void Cast();

    public override void OnEquip()
    {
        base.OnEquip();
        if (data is PeriodicAbilityData pdata)
        {
            AbilityCoolDown = pdata.abilityCooldown;
        }
        else
        {
            Debug.LogError("Unexpected Ability Data");
        }
        
    }
}