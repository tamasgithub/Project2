using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class VFXManager : NetworkBehaviour
{
    public static VFXManager Instance;
    public void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
        
    }
    private Dictionary<string, GameObject> prefabs = new ();
    public GameObject SpawnVFX(string name)
    {
        return null;
    }
}