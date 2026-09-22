using System.Collections.Generic;
using Mirror;
using UnityEngine;

// One pool per game scene. Reach it through GameContext.For(gameObject.scene).ObjectPool
// instead of a static instance, which used to hand every lobby the pool of the scene
// that happened to load last.
public class ObjectPool : NetworkBehaviour
{
    public bool isFake = false;

    public List<PoolInfo> poolInfos;
    private Dictionary<PoolableObjectType, Queue<PoolableObject>> availableObjects;
    private Dictionary<PoolableObjectType, HashSet<PoolableObject>> activeObjects;
    Dictionary<PoolableObjectType, Transform> parents;

    // ServerCallback and not isServer: the pool has to work before Mirror has spawned
    // the scene object itself, and a client must never build a pool of its own.
    [ServerCallback]
    public void Start()
    {
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

    [ServerCallback]
    public PoolableObject Get(PoolableObjectType type, Vector3 position, Quaternion rotation)
    {
        if (isFake)
        {
            GameObject go = Instantiate(poolInfos.Find(i => i.type == type).prefab, position, rotation, transform);
            NetworkServer.Spawn(go);
            PoolableObject fake = go.GetComponent<PoolableObject>();
            fake.SetInUse(true);
            return fake;
        }

        if (availableObjects == null || !availableObjects.TryGetValue(type, out var pool))
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
            ExtendPool(infoForType);
        }
        PoolableObject poolableObject = pool.Dequeue();
        activeObjects[type].Add(poolableObject);
        poolableObject.transform.position = position;
        poolableObject.transform.rotation = rotation;
        poolableObject.SetInUse(true);
        return poolableObject;
    }

    [ServerCallback]
    public void Return(PoolableObject returnedObject)
    {
        if (isFake)
        {
            returnedObject.SetInUse(false);
            NetworkServer.Destroy(returnedObject.gameObject);
            return;
        }
        PoolableObjectType type = returnedObject.PoolableObjectType;

        if (availableObjects == null || !availableObjects.TryGetValue(type, out var pool))
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
        returnedObject.SetInUse(false);
        pool.Enqueue(returnedObject);
    }

    private void ExtendPool(PoolInfo info)
    {
        ExtendPool(info, info.size);
    }

    private void ExtendPool(PoolInfo info, int size)
    {
        availableObjects.TryGetValue(info.type, out var queue);
        Transform parent = parents[info.type];
        for (int i = 0; i < size; i++)
        {
            // Parenting into the pool keeps the object inside this lobby's scene, which is
            // what SceneInterestManagement uses to decide who gets to see it.
            GameObject go = Instantiate(info.prefab, Vector3.zero, Quaternion.identity, parent);
            PoolableObject poolableObject = go.GetComponent<PoolableObject>();
            if (poolableObject == null)
            {
                Debug.LogError($"The provided prefab for type {info.type} does not have a PoolableObject component attached!");
                return;
            }
            NetworkServer.Spawn(go);
            poolableObject.SetInUse(false);
            queue.Enqueue(poolableObject);
        }
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
