using Mirror;
using UnityEngine;

public class Bomb : Projectile
{

    private Gradient lifeTimeColorGradient;
    [SyncVar(hook = nameof(OnColorChanged))]
    private Color currentColor;
    private GameObject explosionPrefab;
    private float explosionVisualDuration;

    [Server]
    protected override void LoadBaseStats(int level, AbilityData abilityData)
    {
        if (abilityData is BombAbilityData bombData)
        {
            baseSpeed = 0;
            maxLifeTime = bombData.maxLifeTime;
            damage = Mathf.RoundToInt(Mathf.Pow(1.25f, level) * bombData.baseDamage);
            lifeTimeColorGradient = bombData.lifeTimeColorGradient;
            aoeSize = Mathf.Pow(1.25f, level) * bombData.explosionSize;
            explosionPrefab = bombData.explosionPrefab;
            explosionVisualDuration = bombData.explosionVisualDuration;
        }
        else
        {
            Debug.LogError("Bomb ability was created with other AbilityData than BombAbilitData!");
        }
    }

    protected override void Update()
    {
        if (!isServer) return;
        base.Update();
        currentColor = lifeTimeColorGradient.Evaluate(LifeTime / maxLifeTime);

    }
    public override void OnCollision(ServerEntity collision)
    {
       //Dont 
    }
    [Client]
    private void OnColorChanged(Color oldColor, Color newColor)
    {
        GetComponentInChildren<SpriteRenderer>().color = newColor;
    }

    [Server]
    protected override void OnLifeTimeEnded()
    {
        GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
        explosion.transform.localScale = Vector2.one * aoeSize;
        explosion.GetComponent<Explosion>().explosionVisualDuration = explosionVisualDuration;
        NetworkServer.Spawn(explosion);
        foreach (ServerEnemy enemy in SpatialHashGrid.ServerEnemies.GetNearObjects(transform.position, aoeSize / 2.0f))
        {
            enemy.ReceiveDamage(new DamageEvent(3));
        }
    }
}
