using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerAbilityController : NetworkBehaviour
{
    public List<Ability> Abilities { get{ return periodicAbilities.Select(x => x as Ability ).ToList(); }}
    private readonly List<PeriodicAbility> periodicAbilities = new();
    private readonly List<PermanentAbility> permanentAbilities = new();
    public DaggerAbilityData daggerAbilityData;
    public BombAbilityData bombAbilityData;

        public KnifeAbilityData knifeData;

    public void Start()
    {
        if (!isServer) return;
        Entity entity = GetComponent<Entity>();
        RegisterAbility(new DaggerAbility(daggerAbilityData, GetComponent<NetworkIdentity>(), entity));
        RegisterAbility(new BombAbility(bombAbilityData, GetComponent<NetworkIdentity>(), entity));
        var k = new KnifeAbility(knifeData, GetComponent<NetworkIdentity>(), GetComponent<Entity>());
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
        if(ability is PermanentAbility permanent)
        {
            permanentAbilities.Add(permanent);
            permanent.OnEquip();
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