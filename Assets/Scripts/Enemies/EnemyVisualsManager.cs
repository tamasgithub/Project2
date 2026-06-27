using System;
using System.Collections.Generic;
using Mirror;
using Unity.Mathematics;
using UnityEngine;

public class EnemyVisualsManager : NetworkBehaviour
{
    List<ServerEnemy> enemies = new ();
    Dictionary<string, GameObject> visuals = new();
    private Material mat;
    public GameObject enemyPrefab;
    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<EnemySnapshot>(UpdateData);
    }

    private void UpdateData(EnemySnapshot snapshot)
    {
        
        enemies = snapshot.enemies;
        foreach (var enemy in enemies)
        {
            InstantiateVisual(enemy);
        }
        
    }
    
    public void InstantiateVisual(ServerEnemy enemy)
    {
        
        if (!visuals.ContainsKey(enemy.id))
        {
            var visual = Instantiate(enemyPrefab, (Vector3)enemy.Position, quaternion.identity);
            if (mat == null)
            {
                mat = visual.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            }
            else
            {
                
            visual.GetComponentInChildren<SpriteRenderer>().sharedMaterial = mat;
            }
            visuals.Add(enemy.id, visual);
        }
    }
    private void LateUpdate()
    {
        if (!isClient) return;
        if (enemies.Count < 1) return;
        foreach (var enemy in enemies)
        {
            try
            {
                visuals[enemy.id].transform.position = Vector3.Lerp(visuals[enemy.id].transform.position, enemy.Position, Time.deltaTime);
            }
            catch
            {
                Debug.LogError("Missing Enemy Visual");
            }
        }
    }

}