using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class EnemyVisualsManager : NetworkBehaviour
{
    Dictionary<string, EnemyDto> enemies = new();
    Dictionary<string, EnemyDto> currentEnemies = new();
    Dictionary<string, GameObject> visuals = new();
    private Material mat;
    public GameObject enemyPrefab;
    private GameObject enemyParent;
    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<EnemySnapshot>(UpdateData);
        enemyParent = new GameObject("EnemyVisuals");
    }

    private void UpdateData(EnemySnapshot snapshot)
    {
        if (snapshot.IsUnityNull()) return;

        currentEnemies.Clear();
        foreach (var dto in snapshot.enemies)
        {
            enemies[dto.Id] = dto;
            Debug.Log(dto.DamageEvents.Count);
            currentEnemies[dto.Id] = dto;
        }
        foreach (var enemy in enemies.Values)
        {
            InstantiateVisual(enemy);
            var totalDamage = 0;
            foreach (var dmgEvent in enemy.DamageEvents)
            {
                totalDamage += dmgEvent.amount;

            }
           
            if (totalDamage > 0 && ObjectPool.Instance != null)
            {
                PoolableObject dmgNr = ObjectPool.Instance?.Get(PoolableObjectType.DMG_NR, (Vector3)enemy.Position, Quaternion.identity);

                dmgNr.GetComponent<DamageNumber>().SetDamage(totalDamage, true);
            }
        }


        foreach (var key in enemies.Keys.ToList())
        {
            if (!currentEnemies.ContainsKey(key))
            {
                
                if (visuals.Remove(key, out var gO))
                {
                    Destroy(gO);
                }
                enemies.Remove(key);
            }
        }


    }
    //Could Probably be optimized
    public void InstantiateVisual(EnemyDto enemy)
    {

        if (!visuals.ContainsKey(enemy.Id))
        {
            var visual = Instantiate(enemyPrefab, (Vector3)enemy.Position, quaternion.identity, enemyParent.transform);
            if (mat == null)
            {
                mat = visual.GetComponentInChildren<SpriteRenderer>().sharedMaterial;
            }
            else
            {

                visual.GetComponentInChildren<SpriteRenderer>().sharedMaterial = mat;
            }
            visuals.Add(enemy.Id, visual);
        }
    }
    private void LateUpdate()
    {
        if (!isClient) return;
        if (enemies.Count < 1) return;
        foreach (var enemy in enemies.Values)
        {
            try
            {
                visuals[enemy.Id].transform.position = Vector3.Lerp(visuals[enemy.Id].transform.position, enemy.Position, 2f* Time.deltaTime);
            }
            catch
            {
                Debug.LogError("Missing Enemy Visual");
            }
        }
    }

}