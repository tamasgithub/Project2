using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class DaggerAbility : PeriodicAbility
{

    private GameObject daggerPrefab;

    public DaggerAbility(AbilityData data, NetworkIdentity owner) : base(data, owner)
    {
    }

    [Server]
    public override void Cast()
    {
        var dagger = GameObject.Instantiate(daggerPrefab, Owner.transform.position, quaternion.identity);
        var facedirection = Owner.transform.GetComponent<PlayerInputController>().FaceDirection;
        dagger.GetComponent<DaggerProjectile>().direction = facedirection;
        dagger.transform.rotation = Quaternion.FromToRotation((Vector3)Vector2.up, (Vector3)facedirection);
        NetworkServer.Spawn(dagger);    
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

    
}