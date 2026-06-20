using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class ServerAbilityManager: NetworkBehaviour
{
    public static ServerAbilityManager Instance;
    void Awake()
    {
        Instance = this;
    }
    private readonly Dictionary<NetworkIdentity, List<PeriodicAbility>> periodicAbilities = new();
    public void RegisterAbility(GameObject player, Ability ability)
    {
        var identity = player.GetComponent<NetworkIdentity>();
        if (ability is PeriodicAbility periodic)
        {
            if (!periodicAbilities.TryGetValue(identity, out var p))
            {
                periodicAbilities.Add(identity, new List<PeriodicAbility>());
            }
            periodicAbilities[identity].Add(periodic);
        }
    }

    void Update()
    {
        foreach (var entry in periodicAbilities)
        {
            foreach (var ability in entry.Value)
            {
                ability.Update(Time.deltaTime);
                if (ability.IsReady)
                {
                    // ability.Cast(this, entry.Key.gameObject);
                }
            }
        }
    }

    public GameObject ServerInstantiate(GameObject prefab)
    {
        return Instantiate(prefab);
    }
    public void ServerSpawn(GameObject gameObject)
    {
        NetworkServer.Spawn(gameObject);
    }
    
}