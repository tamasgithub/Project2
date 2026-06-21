using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerAbilityController : NetworkBehaviour
{
    public List<Ability> Abilities { get{ return periodicAbilities.Select(x => x as Ability ).ToList(); }}
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