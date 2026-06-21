using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class PlayerAbilityController : NetworkBehaviour
{
    public List<Ability> Abilities { get{ return periodicAbilities.Select(x => x as Ability )
    .Concat(permanentAbilities.Select(x => x as Ability)).ToList(); }}
    private readonly List<PeriodicAbility> periodicAbilities = new();
    private readonly List<PermanentAbility> permanentAbilities = new();
    public DaggerAbilityData daggerAbilityData;
    public BombAbilityData bombAbilityData;
    public KnifeAbilityData knifeAbilityData;


    public void Start()
    {
        if (!isServer) return;
        Entity entity = GetComponent<Entity>();
        RegisterAbility(new DaggerAbility(daggerAbilityData, GetComponent<NetworkIdentity>(), entity));
        RegisterAbility(new BombAbility(bombAbilityData, GetComponent<NetworkIdentity>(), entity));
        var k = new KnifeAbility(knifeAbilityData, GetComponent<NetworkIdentity>(), GetComponent<Entity>());
        RegisterAbility(k);
    }

    [Server]
    public void RegisterAbility(Ability ability)
    {

        if (ability is PeriodicAbility periodic)
        {
            periodicAbilities.Add(periodic);
            periodic.OnEquip();
        }
        if (ability is PermanentAbility permanent)
        {
            permanentAbilities.Add(permanent);
            permanent.OnEquip();
        }
    }
    
    public void HandleUpgradeChoice(UpgradeChoice choice)
    {
        if(Abilities.Exists(x => x.AbilityName == choice.AbilityName))
        {
            
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