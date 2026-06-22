using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class DaggerAbility : PeriodicAbility
{

    private GameObject daggerPrefab;
    private int extraDaggers = 4;
    private DaggerAbilityData daggerData;
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
        var half = (int)(extraDaggers + 1) / 2;
        for (int i = -half; i <= half; i++)
        {
            if (i == 0) continue;
            SpawnDagger(facedirection.Rotate(daggerData.spreadAngle * i));
        }
    }

    public override void OnEquip()
    {
        base.OnEquip();
        if (data is DaggerAbilityData daggerData)
        {
            this.daggerData = daggerData;
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
