using Mirror;
using UnityEngine;

public abstract class Ability

{
    public NetworkIdentity Owner { get; private set; }
    public string Name { get; protected set; }
    public int Level { get; protected set; } = 1;
    public AbilityType Type { get; protected set; }

    public AbilityData data;
    public Ability(AbilityData data, NetworkIdentity owner)
    {
        this.data = data;
        this.Owner = owner;
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
