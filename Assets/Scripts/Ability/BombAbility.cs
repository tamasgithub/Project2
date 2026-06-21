using Mirror;
using UnityEngine;

public class BombAbility : PeriodicAbility
{

    private GameObject bombPrefab;
    public BombAbility(BombAbilityData data, NetworkIdentity owner, Entity entity) : base(data, owner, entity)
    {

    }

    [Server]
    public override void Cast()
    {
        GameObject newBomb = GameObject.Instantiate(bombPrefab, _owner.transform.position, Quaternion.identity);
        newBomb.transform.GetComponent<Bomb>().LoadStats(Level, data, Vector2.zero, _entity);
        NetworkServer.Spawn(newBomb);
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
