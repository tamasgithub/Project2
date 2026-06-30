using System;
using Mirror;
using TMPro;
using UnityEngine;

public class DamageNumber : PoolableObject
{
    Animation Animation => GetComponentInChildren<Animation>();
    TextMeshPro TextMeshPro => GetComponentInChildren<TextMeshPro>();
    Renderer Renderer => GetComponentInChildren<Renderer>();
    
    public override void OnStartClient()
    {
        ClientOnReturn();
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

    [ClientRpc]
    public override void RpcOnGet()
    {
        Animation.Play();
        Renderer.enabled = true;
    }

    [ClientRpc]
    public override void RpcOnReturn()
    {
        ClientOnReturn();
    }
    [Client]
    private void ClientOnReturn()
    {
        Animation.Stop();
        Renderer.enabled = false;
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
        ObjectPool.Instance.Return(this);
    }

}
