using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class UpgradeRequest
{
    public List<UpgradeChoice> choices = new();
    public List<Ability> abilities;
    private List<UpgradeChoice> pool = new()
    {
        //Abilities
        new UpgradeChoice( AbilityName.DaggerAbility),
        new UpgradeChoice( AbilityName.BombAbility),
        new UpgradeChoice( AbilityName.KnifeAbility),
        //Stats
        new UpgradeChoice(StatName.MAX_HP, 1),
        new UpgradeChoice(StatName.MOVEMENTSPEED,1.5f, false),
        new UpgradeChoice(StatName.PROJECTILE_SIZE,1.2f,false),
        new UpgradeChoice(StatName.DAMAGE, 1),
    };

    public void GenerateRequest(GameObject player)
    {
        var rnd = new System.Random();
        this.choices = pool.OrderBy(x => rnd.Next()).Take(3).ToList();
        
    }
    public UpgradeRequest(GameObject player)
    {
        abilities = player.GetComponent<PlayerAbilityController>().Abilities;
        GenerateRequest(player);
    }
    public UpgradeRequest(List<UpgradeChoice> choices)
    {
        this.choices = choices;
    }

    
}
