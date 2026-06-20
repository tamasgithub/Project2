using UnityEngine;
using Mirror;

public class Projectile : NetworkBehaviour
{
    public float baseSpeed;
    public float maxLifeTime = 10.0f;
    private Vector2 direction;
    private float lifeTime;


    public virtual void Load(Vector2 direction)
    {
        this.direction = direction;
    

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

    protected virtual void OnEnemyCollision()
    {
        NetworkServer.Destroy(gameObject);
    }
}