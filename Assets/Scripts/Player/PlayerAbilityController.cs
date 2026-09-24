using Mirror;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

public class PlayerAbilityController : NetworkBehaviour
{
    public List<Ability> Abilities
    {
        get
        {
            return periodicAbilities.Select(x => x as Ability)
    .Concat(permanentAbilities.Select(x => x as Ability)).ToList();
        }
    }
    private readonly List<PeriodicAbility> periodicAbilities = new();
    private readonly List<PermanentAbility> permanentAbilities = new();
    public List<AbilityRegisterItem> abilityData = new ();
    public DaggerAbilityData daggerAbilityData;
    public BombAbilityData bombAbilityData;
    public KnifeAbilityData knifeAbilityData;
    public ChakramAbilityData chakramAbilityData;


    private bool startingAbilitiesGranted;

    /// <summary>
    /// Granted when the player enters a game scene, not when its object spawns.
    ///
    /// OnStartServer runs while the owner is still in the menu, so the player object lives in
    /// the MainMenuScene and has no GameContext. A PermanentAbility creates its orbital on
    /// equip and needs that context, so it would silently create nothing.
    /// </summary>
    [Server]
    public void GrantStartingAbilities()
    {
        if (startingAbilitiesGranted) return;
        startingAbilitiesGranted = true;

        CreateAbility(AbilityName.KnifeAbility);
    }

    [Server]
    private void CreateAbility(AbilityName abilityName)
    {
        Entity entity = GetComponent<Entity>();
        NetworkIdentity owner = GetComponent<NetworkIdentity>();

        switch (abilityName)
        {
            case AbilityName.DaggerAbility:
                RegisterAbility(new DaggerAbility(daggerAbilityData, owner, entity));
                break;
            case AbilityName.BombAbility:
                RegisterAbility(new BombAbility(bombAbilityData, owner, entity));
                break;
            case AbilityName.KnifeAbility:
                RegisterAbility(new KnifeAbility(knifeAbilityData, owner, entity));
                break;
            case AbilityName.ChakramAbility:
                RegisterAbility(new ChakramAbility(chakramAbilityData, owner, entity));
                break;
            default:
                Debug.LogError($"Could not resolve ability name {abilityName}");
                break;
        }
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
    [Server]
    public void HandleUpgradeChoice(UpgradeChoice choice)
    {
        //Check If Player Already Has Ability
        var ability = Abilities.FirstOrDefault(x => x.AbilityName == choice.AbilityName);
        if (ability != null)
        {
            ability.LevelUp();
            return;
        }
        CreateAbility(choice.AbilityName);
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

[System.Serializable]
public struct AbilityRegisterItem
{
    public AbilityName name;
    public AbilityData data;
}