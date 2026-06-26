using Mirror;
using UnityEngine;

public class ChakramAbility : PermanentAbility
{
    private GameObject chakramPrefab;
    private ChakramOrbital orbital;
    public ChakramAbility(ChakramAbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {
        chakramPrefab = data.orbital;
        AbilityName = AbilityName.ChakramAbility;
    }
    public override void OnEquip()
    {
        base.OnEquip();
        SpawnOrbital();
    }
    public override void LevelUp()
    {
        base.LevelUp();
        orbital.Init(Level, _owner.netId);
    }
    private void SpawnOrbital()
    {
        var gO = GameObject.Instantiate(chakramPrefab);
        orbital = gO.GetComponent<ChakramOrbital>();
        orbital.Init(6, _owner.netId);
        NetworkServer.Spawn(gO);
    }

}