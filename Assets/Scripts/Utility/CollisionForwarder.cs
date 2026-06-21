

using System;
using Mirror;
using UnityEngine;

public class CollisionForwarder : NetworkBehaviour {
    // ServerCallback to not log warnings on collision on client side
    public Action<Collision2D> OnCollision;
    public Action<Collider2D> OnTriggerEnter;
    [ServerCallback]
    private void OnCollisionEnter2D(Collision2D collision)
    {

        if (OnCollision != null)
        {
            OnCollision(collision);
        }
    }
     [ServerCallback]
    private void OnTriggerEnter2D(Collider2D collision)
    {
        OnTriggerEnter?.Invoke(collision);
    }
}