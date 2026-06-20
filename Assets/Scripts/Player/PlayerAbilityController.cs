using Mirror;
using UnityEngine;
using System.Collections.Generic;

public class PlayerAbilityController : NetworkBehaviour
{
    private readonly List<PeriodicAbility> periodicAbilities = new ();

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