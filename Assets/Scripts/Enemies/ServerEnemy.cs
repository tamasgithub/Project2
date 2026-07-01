using UnityEngine;

public class ServerEnemy : ServerEntity
{

    public override void OnKilled()
    {
        // OnDeath -= OnKilled;
        // SpatialHashGrid.Enemies.Remove(this);
        SpawnRandomLoot();
    }

    private void SpawnRandomLoot()
    {

        PoolableObjectType type = PoolableObjectType.EXP;
        PoolableObject loot = ObjectPool.Instance.Get(type, (Vector3)Position, Quaternion.identity);
        return;

    }

}