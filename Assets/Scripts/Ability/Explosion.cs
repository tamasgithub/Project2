using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Explosion : NetworkBehaviour
{
    public float explosionVisualDuration;

    public override void OnStartServer()
    {
        Invoke(nameof(SelfDestroy), explosionVisualDuration);
    }

    public override void OnStartClient()
    {
        if (NetworkServer.active) return;
        SceneManager.MoveGameObjectToScene(gameObject, SceneManager.GetSceneByName("GameScene"));
    }

    private void SelfDestroy()
    {
        NetworkServer.Destroy(gameObject);
    }
}
