using Mirror;
using UnityEngine;

public class DaggerProjectile : NetworkBehaviour
{
    public float speed;
    public Vector2 direction;

    [Server]
    void Update()
    {
        if (!authority) return;
        transform.position += (Vector3) direction * speed * Time.deltaTime;          
    }
}
