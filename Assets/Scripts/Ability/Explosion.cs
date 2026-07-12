using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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

        if (isClient)
        {
            SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("GameScene"));
        }
    }

    private void SelfDestroy()
    {
        NetworkServer.Destroy(gameObject);
    }


}
