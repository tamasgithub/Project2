using Mirror;
using UnityEngine;

public class PoolableObject : NetworkBehaviour
{
    public PoolableObjectType PoolableObjectType;

    /// <summary>
    /// Whether this object is currently handed out by the pool.
    ///
    /// Replicated instead of pushed through ClientRpcs: an Rpc only reaches the observers an
    /// object has at that moment, and the pool is filled while the game scene still has none,
    /// so the initial "hidden" state never arrived on any client. A SyncVar travels in the
    /// spawn payload, so a client that starts observing later sees the correct state.
    /// </summary>
    [SyncVar(hook = nameof(OnInUseChanged))]
    private bool inUse;

    public bool InUse => inUse;

    [Server]
    public void SetInUse(bool value)
    {
        inUse = value;

        if (value) OnGet();
        else OnReturn();
    }

    public override void OnStartClient()
    {
        // Mirror does not run SyncVar hooks for the values that arrive with the spawn payload,
        // so apply the state once here as well.
        ApplyOnClient(inUse);
    }

    private void OnInUseChanged(bool _, bool value)
    {
        if (!NetworkClient.active) return;
        ApplyOnClient(value);
    }

    /// <summary>Server side reaction to being handed out.</summary>
    public virtual void OnGet() { }

    /// <summary>Server side reaction to being returned.</summary>
    public virtual void OnReturn() { }

    /// <summary>Client side presentation, driven purely by the replicated state.</summary>
    protected virtual void ApplyOnClient(bool isInUse) { }
}
