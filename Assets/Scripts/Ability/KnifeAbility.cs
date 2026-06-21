using System.Collections.Generic;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class KnifeAbility : PermanentAbility
{

    private GameObject kniveOrbitalPrefab;
    private KnifeOrbital orbital;
    public KnifeAbility(KnifeAbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {
        kniveOrbitalPrefab = data.orbitalPrefab;
        AbilityName = AbilityName.KnifeAbility;
    }

    public override void OnEquip()
    {
        base.OnEquip();
        SpawnOrbital();
    }
    public override void LevelUp()
    {
        base.LevelUp();
        orbital.Refresh(Level, _entity, (KnifeAbilityData)data);
    }
    private void SpawnOrbital()
    {
        var gO = GameObject.Instantiate(kniveOrbitalPrefab, _owner.transform);
        orbital = gO.GetComponent<KnifeOrbital>();
        orbital.Refresh(Level, _entity, (KnifeAbilityData)data);
        // daggerProjectile.LoadStats(Level, data, direction, _entity);
        // dagger.transform.rotation = Quaternion.FromToRotation((Vector3)Vector2.up, (Vector3)direction);
        NetworkServer.Spawn(gO);  
    } 



    
}