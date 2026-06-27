using System;
using System.Collections.Generic;
using Mirror;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyVisualsManager : NetworkBehaviour
{
    List<ServerEnemy> enemies = new();
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
        enemies = snapshot.enemies;
        foreach (var enemy in enemies)
        {
            InstantiateVisual(enemy);
            var totalDamage = 0;
            foreach (var dmgEvent in enemy.damageEvents)
            {
                totalDamage += dmgEvent.amount;

            }
            if (totalDamage > 0 && ObjectPool.Instance != null)
            {
                PoolableObject dmgNr = ObjectPool.Instance?.Get(PoolableObjectType.DMG_NR, (Vector3)enemy.Position, Quaternion.identity);
               
                dmgNr.GetComponent<DamageNumber>().SetDamage(totalDamage, true);
            }

        }

    }

    public void InstantiateVisual(ServerEnemy enemy)
    {

        if (!visuals.ContainsKey(enemy.id))
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