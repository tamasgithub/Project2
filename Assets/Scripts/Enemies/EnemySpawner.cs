using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public float spawnFrequency = 3f;
    public float baseSpawnAmount = 10f;
    public float spawnRadius = 5f;
    public Vector2 spawnPosition = Vector2.zero;

    [Header("Enemy base stats (used to live on the Enemy prefab)")]
    public int enemyMaxHp = 1;
    public float enemyMovementSpeed = 2f;

    [Header("Loot table (used to live on the Enemy prefab)")]
    public List<LootTableEntry> lootTable = new()
    {
        new LootTableEntry { LootType = Loot.LootType.EXP, probability = 90f },
        new LootTableEntry { LootType = Loot.LootType.HP_POT, probability = 10f },
    };

    private int waveNumber = 0;

    [ServerCallback]
    void Start()
    {
        StartCoroutine(PeriodicSpawning());
    }

    private IEnumerator PeriodicSpawning()
    {
        // The context is created right after this scene finished loading.
        yield return new WaitUntil(() => GameContext.For(this) != null);
        yield return new WaitForSeconds(4);

        Debug.Log("Periodic Spawning started");
        while (true)
        {
            waveNumber++;
            SpawnInCircle(spawnRadius + waveNumber * 0.25f, baseSpawnAmount + waveNumber);
            yield return new WaitForSeconds(spawnFrequency);
        }
    }

    [Server]
    private void SpawnInCircle(float spawnRadius, float spawnAmount)
    {
        GameContext context = GameContext.For(this);
        if (context == null || context.EnemyManager == null) return;

        for (int i = 0; i < spawnAmount; i++)
        {
            float angle = i * Mathf.PI * 2f / spawnAmount;

            Vector2 position = spawnPosition + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * spawnRadius;

            var enemy = new ServerEnemy(context, waveNumber, enemyMaxHp, enemyMovementSpeed, lootTable);
            enemy.Position = position;

            context.EnemyManager.RegisterEnemy(enemy);
        }
    }
}
