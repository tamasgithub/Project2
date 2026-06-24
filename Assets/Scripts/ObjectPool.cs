using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class ObjectPool : NetworkBehaviour
{
    public static ObjectPool Instance;
    public bool isFake = false;

    public List<PoolInfo> poolInfos;
    private Dictionary<PoolableObjectType, Queue<PoolableObject>> availableObjects;
    private Dictionary<PoolableObjectType, HashSet<PoolableObject>> activeObjects;
    Dictionary<PoolableObjectType, Transform> parents;

    public override void OnStartServer()
    {
        Instance = this;
        availableObjects = new(poolInfos.Count);
        activeObjects = new(poolInfos.Count);
        parents = new(poolInfos.Count);
        for (int i = 0; i < poolInfos.Count; i++)
        {
            PoolInfo poolInfo = poolInfos[i];

            Transform child = new GameObject(poolInfo.type.ToString()).transform;
            child.transform.SetParent(transform, false);
            parents[poolInfo.type] = child;

            int size = poolInfo.size;
            availableObjects.Add(poolInfo.type, new(size));
            activeObjects.Add(poolInfo.type, new(size));
            ExtendPool(poolInfo);
        }

        Debug.Log("ObjectPool initialized");
    }

    [Server]
    public PoolableObject Get(PoolableObjectType type, Vector3 position, Quaternion rotation)
    {
        if (!isServer) return null;
        //Debug.Log("Get poolable object of type " + type);

        if (isFake)
        {
            GameObject go = Instantiate(poolInfos.Find(i => i.type == type).prefab, position, rotation);
            NetworkServer.Spawn(go);
            go.GetComponent<PoolableObject>().OnGet();
            return go.GetComponent<PoolableObject>();
        }

        if (!availableObjects.TryGetValue(type, out var pool))
        {
            Debug.LogError($"No pool exists for {type}");
            return null;
        }
        if (pool.Count == 0)
        {
            PoolInfo infoForType = poolInfos.Find(i => i.type == type);
            if (infoForType.prefab == null)
            {
                Debug.LogError($"Cannot return GameObject for type {type}, no prefab is known!");
                return null;
            }
            Transform child = transform.Find(type.ToString());
            ExtendPool(infoForType);
        }
        PoolableObject poolableObject = pool.Dequeue();
        activeObjects[type].Add(poolableObject);
        poolableObject.transform.position = position;
        poolableObject.transform.rotation = rotation;
        poolableObject.OnGet();
        poolableObject.RpcOnGet();
        return poolableObject;
    }

    [Server]
    public void Return(PoolableObject returnedObject)
    {
        if (!isServer) return;
        if (isFake)
        {
            returnedObject.OnReturn();
            NetworkServer.Destroy(returnedObject.gameObject);
            return;
        }
        PoolableObjectType type = returnedObject.PoolableObjectType;
        Debug.Log("Return poolable object of type " + type);

        if (!availableObjects.TryGetValue(type, out var pool))
        {
            Debug.LogError($"No pool exists for {type}");
            return;
        }
        if (!activeObjects.TryGetValue(type, out var actives))
        {
            Debug.LogError($"No active objects of type {type}");
            return;
        }
        if (!actives.Remove(returnedObject))
        {
            Debug.LogError($"Returned object {returnedObject} of type {type} wasn't active!");
            return;
        }
        /*if (!gameObject.activeSelf)
        {
            Debug.LogWarning($"{returnedObject} was already inactive.");
        }
        gameObject.SetActive(false);*/
        returnedObject.OnReturn();
        returnedObject.RpcOnReturn();
        pool.Enqueue(returnedObject);

    }

    private void ExtendPool(PoolInfo info)
    {
        ExtendPool(info, info.size);
    }

    private void ExtendPool(PoolInfo info, int size)
    {
        Debug.Log($"Extend pool of type {info.type}");
        availableObjects.TryGetValue(info.type, out var queue);
        Transform parent = parents[info.type];
        for (int i = 0; i < size; i++)
        {
            GameObject go = Instantiate(info.prefab, Vector3.zero, Quaternion.identity, parent);
            PoolableObject poolableObject = go.GetComponent<PoolableObject>();
            if (poolableObject == null)
            {
                Debug.LogError($"The provided prefab for type {info.type} does not have a PoolableObject component attached!");
                return;
            }
            NetworkServer.Spawn(go);
            poolableObject.OnReturn();
            poolableObject.RpcOnReturn();
            queue.Enqueue(poolableObject);
        }
        //Debug.Log($"Added {size} objects of type {info.type}");
    }
}

public enum PoolableObjectType
{
    EXP,
    HP_POTION,
    ENEMY,
    DMG_NR
}

[System.Serializable]
public struct PoolInfo
{
    public PoolableObjectType type;
    public GameObject prefab;
    public int size;
}
