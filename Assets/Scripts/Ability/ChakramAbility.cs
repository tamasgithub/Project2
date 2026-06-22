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
        orbital.Refresh(Level);
    }
    private void SpawnOrbital()
    {
        var gO = GameObject.Instantiate(chakramPrefab, _owner.transform);
        orbital = gO.GetComponent<ChakramOrbital>();
        orbital.Refresh(6);
        NetworkServer.Spawn(gO);  
    } 
    
}