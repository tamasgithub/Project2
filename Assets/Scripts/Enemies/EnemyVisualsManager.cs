using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public class EnemyVisualsManager : MonoBehaviour
{
    Dictionary<string, EnemyDto> enemies = new();
    Dictionary<string, EnemyDto> currentEnemies = new();
    Dictionary<string, EnemyVisual> visuals = new();
    List<DamageEventDto> damageEvents = new();
    private Material mat;
    public GameObject enemyPrefab;
    private Transform enemyParent;

    public void Start()
    {
        if (!NetworkClient.active)
        {
            Destroy(this);
            return;
        }
        NetworkClient.RegisterHandler<EnemyStatusMessage>(UpdateData);
        NetworkClient.RegisterHandler<DamageEventsMessage>(HandleDamageEvents);
        Debug.Log("OnStartClient EnemyVisualsManager");
        enemyParent = HierarchyUtility.GetOrCreatePath("EnemyVisuals", SceneManager.GetSceneByName("GameScene"));
    }

    private void UpdateData(EnemyStatusMessage snapshot)
    {
        if (!IsPlayerInGameScene()) return;

        if (snapshot.IsUnityNull()) return;

        currentEnemies.Clear();
        foreach (var dto in snapshot.enemies)
        {
            enemies[dto.Id] = dto;
            currentEnemies[dto.Id] = dto;
        }
        foreach (var enemy in enemies.Values)
        {
            InstantiateVisual(enemy);

        }


        foreach (var key in enemies.Keys.ToList())
        {
            if (!currentEnemies.ContainsKey(key))
            {

                if (visuals.Remove(key, out var gO))
                {
                    Destroy(gO.gameObject);
                }
                enemies.Remove(key);
            }
        }


    }

    private void HandleDamageEvents(DamageEventsMessage msg)
    {
        if (!IsPlayerInGameScene()) return;
        damageEvents.Clear();
        damageEvents.AddRange(msg.damageEventDtos);
        if (damageEvents.Count < 1) return;
        foreach (var dmgEvent in damageEvents)
        {
            if (dmgEvent.Amount < 1) continue;
            if (visuals.TryGetValue(dmgEvent.TargetId, out var visual))
            {
                visual?.HandleDamageEvent(dmgEvent);

                if (dmgEvent.Amount > 0 && ObjectPool.Instance != null)
                {
                    PoolableObject dmgNr = ObjectPool.Instance?.Get(PoolableObjectType.DMG_NR, visual.transform.position, Quaternion.identity);

                    dmgNr.GetComponent<DamageNumber>().SetDamage(dmgEvent.Amount, true);
                }
            }

        }

    }

    //Could Probably be optimized
    private void InstantiateVisual(EnemyDto enemy)
    {

        if (!visuals.ContainsKey(enemy.Id))
        {
            var visual = Instantiate(enemyPrefab, (Vector3)enemy.Position, quaternion.identity, enemyParent).GetComponent<EnemyVisual>();

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
        if (!NetworkClient.active) return;
        if (enemies.Count < 1) return;
        foreach (var enemy in enemies.Values)
        {
            try
            {
                visuals[enemy.Id].transform.position = Vector3.Lerp(visuals[enemy.Id].transform.position, enemy.Position, 2f * Time.deltaTime);
                visuals[enemy.Id].UpdateStats(enemy);
            }
            catch
            {
                Debug.LogError("Missing Enemy Visual");
            }
        }
    }

    private bool IsPlayerInGameScene()
    {
        if (NetworkClient.connection?.identity == null)
            return false;

        return NetworkClient.connection.identity.gameObject.scene.name == "GameScene";
    }

}