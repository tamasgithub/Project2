using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class DaggerAbility : PeriodicAbility
{

    private GameObject daggerPrefab;

    public DaggerAbility(AbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner,entity)
    {
        
    }

    [Server]
    public override void Cast()
    {
        var facedirection = _owner.transform.GetComponent<PlayerInputController>().FaceDirection;
        SpawnDagger(facedirection);   
    }

    public override void OnEquip()
    {
        base.OnEquip();
        if (data is DaggerAbilityData daggerData)
        {
            daggerPrefab = daggerData.daggerPrefab;
        }
        else
        {
            Debug.LogError("Unexpected Ability Data");
        }
        
    }

    private void SpawnDagger(Vector2 direction)
    {
        var dagger = GameObject.Instantiate(daggerPrefab, _owner.transform.position, quaternion.identity);
        dagger.GetComponent<DaggerProjectile>().Load(direction, _entity);
        dagger.transform.rotation = Quaternion.FromToRotation((Vector3)Vector2.up, (Vector3)direction);
        NetworkServer.Spawn(dagger);  
    }
}