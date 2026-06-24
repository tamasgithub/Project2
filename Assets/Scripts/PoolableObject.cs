using Mirror;
using UnityEngine;

public class PoolableObject : NetworkBehaviour
{
    public PoolableObjectType PoolableObjectType { get; protected set; }
    public virtual void OnGet() { }
    public virtual void OnReturn() { }

    public virtual void RpcOnGet() { }
    public virtual void RpcOnReturn() { }
}