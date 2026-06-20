using Mirror;
using UnityEngine;

public class DaggerProjectile : NetworkBehaviour
{
    public float speed;
    public Vector2 direction;
    private float lifeTime;

    [Server]
    void Update()
    {
        if (!authority) return;
        transform.position += (Vector3)direction * speed * Time.deltaTime;
        lifeTime += Time.deltaTime;
        if(lifeTime >= 10.0f)
        {
            NetworkServer.Destroy(gameObject);
        }      
    }
}
