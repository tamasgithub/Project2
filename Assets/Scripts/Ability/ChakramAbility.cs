using Mirror;
using UnityEngine;

public class ChakramAbility : PermanentAbility
{
    private GameObject chakramPrefab;
    private ChakramOrbital orbital;
    private UpgradableStat<int> _chakramCount;
    private UpgradableStat<float> _hoverDuration;
    private UpgradableStat<float> _detachDistance;
    private UpgradableStat<float> _individualDelay;
    public ChakramAbility(ChakramAbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {
        AbilityName = AbilityName.ChakramAbility;

        chakramPrefab = data.orbital;
        _chakramCount = data.orbitalCount;
        _hoverDuration = data.hoverDuration;
        _detachDistance = data.detachDistance;
        _individualDelay = data.individualDelay;
    }
    public override void OnEquip()
    {
        base.OnEquip();
        SpawnOrbital();
    }
    public override void LevelUp()
    {
        base.LevelUp();
        orbital?.Init(_owner, _chakramCount.Value, _hoverDuration.Value, _detachDistance.Value,_individualDelay.Value);
    }
    private void SpawnOrbital()
    {
        Debug.Log("SPAWN ORBITALS");
        var gO = GameObject.Instantiate(chakramPrefab);
        
        orbital = gO.GetComponent<ChakramOrbital>();
        NetworkServer.Spawn(gO);
        orbital.Init( _owner, _chakramCount.Value, _hoverDuration.Value, _detachDistance.Value , _individualDelay.Value);
    }

}