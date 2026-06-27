using Mirror;
using UnityEngine;

public class PoolableObject : NetworkBehaviour
{
    public PoolableObjectType PoolableObjectType; 
    public virtual void OnGet() { }
    public virtual void OnReturn() { }

    public virtual void RpcOnGet() { }
    public virtual void RpcOnReturn() { }
}