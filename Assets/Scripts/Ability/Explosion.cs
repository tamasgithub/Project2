using Mirror;
using UnityEngine;

public class Explosion : NetworkBehaviour
{
    public float explosionVisualDuration;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (isServer)
        {
            Invoke(nameof(SelfDestroy), explosionVisualDuration);
        }
    }

    private void SelfDestroy()
    {
        NetworkServer.Destroy(gameObject);
    }


}
