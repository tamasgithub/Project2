using System.Collections.Generic;
using UnityEngine;

public class UpgradeRequest
{
    public List<UpgradeChoice> choices = new();

    public void GenerateRequest(GameObject player)
    {
       
        this.choices = new List<UpgradeChoice>()
        {
            new UpgradeChoice( player.GetComponent<PlayerAbilityController>().Abilities[0]),
            new UpgradeChoice( player.GetComponent<PlayerAbilityController>().Abilities[0]),
            new UpgradeChoice( player.GetComponent<PlayerAbilityController>().Abilities[0])
        };
    }
    public UpgradeRequest(GameObject player)
    {
        GenerateRequest(player);
    }
    public UpgradeRequest(List<UpgradeChoice> choices)
    {
        this.choices = choices;
    }


}