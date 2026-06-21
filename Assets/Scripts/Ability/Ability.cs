using Mirror;
using UnityEngine;

public abstract class Ability

{
    public AbilityName AbilityName;
    protected NetworkIdentity _owner;
    protected Entity _entity;
    public string Name { get; protected set; }
    public int Level { get; protected set; } = 1;
    public AbilityType Type { get; protected set; }

    public AbilityData data;
    public Ability(AbilityData data, NetworkIdentity owner, Entity entity)
    {
        this.data = data;
        this._owner = owner;
        this._entity = entity;
    }

    protected Ability(AbilityData data)
    {
        this.data = data;
    }

    public virtual void OnEquip()
    {
        Name = data.abilityName;


    }

    public void LevelUp()
    {
        Level++;
    }
    
    
}
