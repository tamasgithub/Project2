using UnityEngine;
using Mirror;

public abstract class Projectile : NetworkBehaviour
{
    // default if not overridden by AbilityData in LoadBaseStats
    protected float baseSpeed = 3f;
    protected float maxLifeTime = 10.0f;
    protected int damage = 1;
    protected int pierce = 0;

    private Vector2 direction;
    private float lifeTime;

    public virtual void LoadStats(int level, AbilityData abilityData, Vector2 direction, Entity _entity)
    {
        LoadBaseStats(level, abilityData);
        this.direction = direction;
        transform.localScale *= _entity.ProjectileSize;
        this.damage += _entity.Damage;
        this.pierce += _entity.Pierce;
    }
    [Server]
    void Update()
    {
        if (!authority) return;
        Move();
        if (lifeTime >= maxLifeTime)
        {
            NetworkServer.Destroy(gameObject);
        }
    }

    protected virtual void Move()
    {
        transform.position += (Vector3)direction * baseSpeed * Time.deltaTime;
        lifeTime += Time.deltaTime;
    }

    protected abstract void LoadBaseStats(int level, AbilityData abilityData);

    // ServerCallback to not log warnings on collision on client side
    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {
        
        if (collision.collider.tag == "Enemy")
        {
            Enemy enemy = collision.collider.GetComponent<Enemy>();
            enemy.ReceiveDamage(damage);
            pierce -= 1;
            if (pierce < 0)
            {
                NetworkServer.Destroy(gameObject);
            }
        }
    }
}