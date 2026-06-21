using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class PlayerAbilityController : NetworkBehaviour
{
    private readonly List<PeriodicAbility> periodicAbilities = new();
    public DaggerAbilityData data;

    [Server]
    public void Start()
    {
        var a = new DaggerAbility(data, GetComponent<NetworkIdentity>(), GetComponent<Entity>());
        RegisterAbility(a);
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

    [Server]
    void Update()
    {
        
        foreach (var ability in periodicAbilities)
        {
            ability.Update(Time.deltaTime);
        }
    }

}