using Mirror;
using UnityEngine;

public class BombAbility : PeriodicAbility
{

    private GameObject bombPrefab;
    public BombAbility(BombAbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {
        AbilityName = AbilityName.BombAbility;
    }

    [Server]
    public override void Cast()
    {
        GameContext context = Context;
        if (context == null) return;

        GameObject newBomb = context.Create(bombPrefab, _owner.transform.position, Quaternion.identity);
        newBomb.transform.GetComponent<Bomb>().LoadStats(Level, data, Vector2.zero, _entity);
        context.Spawn(newBomb);
    }

    public override void OnEquip()
    {
        base.OnEquip();
        if (data is BombAbilityData bombData)
        {
            bombPrefab = bombData.bombPrefab;
        }
        else
        {
            Debug.LogError("Unexpected Ability Data");
        }

    }
}
