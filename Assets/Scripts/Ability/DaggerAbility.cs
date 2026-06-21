using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class DaggerAbility : PeriodicAbility
{

    private GameObject daggerPrefab;
    private int extraDaggers = 0;
    private float spread = 15f;
    public DaggerAbility(AbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {
        AbilityName = AbilityName.DaggerAbility;
    }

    [Server]
    public override void Cast()
    {
        var facedirection = _owner.transform.GetComponent<PlayerInputController>().FaceDirection;
        SpawnDagger(facedirection);
        for (int i = 0; i < extraDaggers; i++)
        {
            SpawnDagger(facedirection.Rotate(spread * (-extraDaggers/2 + i*2)));
        }
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

    public override void LevelUp()
    {
        base.LevelUp();
        extraDaggers = Level >= 2 ? 2 : 0;
    }

    private void SpawnDagger(Vector2 direction)
    {
        var dagger = GameObject.Instantiate(daggerPrefab, _owner.transform.position, quaternion.identity);
        var daggerProjectile = dagger.GetComponent<DaggerProjectile>();
        daggerProjectile.LoadStats(Level, data, direction, _entity);
        dagger.transform.rotation = Quaternion.FromToRotation((Vector3)Vector2.up, (Vector3)direction);
        NetworkServer.Spawn(dagger);
    }
}
