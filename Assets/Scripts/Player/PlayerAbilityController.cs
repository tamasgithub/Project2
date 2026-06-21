using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerAbilityController : NetworkBehaviour
{
    public List<Ability> Abilities { get{ return periodicAbilities.Select(x => x as Ability ).ToList(); }}
    private readonly List<PeriodicAbility> periodicAbilities = new();
    public DaggerAbilityData daggerAbilityData;
    public BombAbilityData bombAbilityData;

    
    public void Start()
    {
        if (!isServer) return;
        Entity entity = GetComponent<Entity>();
        RegisterAbility(new DaggerAbility(daggerAbilityData, GetComponent<NetworkIdentity>(), entity));
        RegisterAbility(new BombAbility(bombAbilityData, GetComponent<NetworkIdentity>(), entity));
    }

    [Server]
    public void RegisterAbility(Ability ability)
    {

        if (ability is PeriodicAbility periodic)
        {
            periodicAbilities.Add(periodic);
            periodic.OnEquip();
        }
    }

    
    void Update()
    {
        if (!isServer) return;
        foreach (var ability in periodicAbilities)
        {
            ability.Update(Time.deltaTime);
        }
    }

}