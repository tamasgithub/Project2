using System;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DamageNumber : PoolableObject
{
    Animation Animation => GetComponentInChildren<Animation>();
    TextMeshPro TextMeshPro => GetComponentInChildren<TextMeshPro>();
    Renderer Renderer => GetComponentInChildren<Renderer>();

    public override void OnStartClient()
    {
        // Applies the replicated in-use state.
        base.OnStartClient();

        Scene scene = SceneManager.GetSceneByName("GameScene");
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        // See Loot.OnStartClient: a parent named "ObjectPool" would be the networked scene
        // object, which Mirror keeps deactivated.
        Transform parent = HierarchyUtility.GetOrCreatePath("PooledObjects/DamageNumbers", scene);
        transform.SetParent(parent, false);
    }

    public override void OnGet()
    {
        Animation.Play();
        var length = Animation.clip.length;
        Invoke("OnAnimationEnd", length);
        Renderer.enabled = true;
    }

    public override void OnReturn()
    {
        Animation.Stop();
        Renderer.enabled = false;
    }

    protected override void ApplyOnClient(bool isInUse)
    {
        if (isInUse)
        {
            Animation.Play();
            Renderer.enabled = true;
        }
        else
        {
            Animation.Stop();
            Renderer.enabled = false;
        }
    }

    [Server]
    public void SetDamage(float damage, bool damagedEntityIsPlayer)
    {
        TextMeshPro.text = damage.ToString();
        TextMeshPro.color = damagedEntityIsPlayer ? Color.red : Color.white;
        RpcSetDamage(damage, damagedEntityIsPlayer);
    }

    [ClientRpc]
    public void RpcSetDamage(float damage, bool damagedEntityIsPlayer)
    {
        TextMeshPro.text = damage.ToString();
        TextMeshPro.color = damagedEntityIsPlayer ? Color.red : Color.white;
    }

    public void OnAnimationEnd()
    {
        GameContext.For(this)?.ObjectPool?.Return(this);
    }

}
