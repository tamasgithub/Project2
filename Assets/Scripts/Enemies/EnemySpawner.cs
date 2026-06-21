using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;

public class EnemySpawner : NetworkBehaviour
{
    public Enemy[] enemies;
    public float spawnFrequency = 3f;
    public float baseSpawnAmount = 10f;
    public float spawnRadius = 5f;
    public Vector2 spawnPosition = Vector2.zero;

    private int waveNumber = 0;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public override void OnStartServer()
    {
        StartCoroutine(PeriodicSpawning());
    }

    

    private IEnumerator PeriodicSpawning()
    {
        Debug.Log("Periodic Spawning started");
        int enemyIndex = 0;
        while (true)
        {
            waveNumber++;
            SpawnInCircle(enemies[enemyIndex++ % enemies.Length], spawnRadius + waveNumber * 0.25f, baseSpawnAmount + waveNumber);
            yield return new WaitForSeconds(spawnFrequency);
        }
    }

    [Server]
    private void SpawnInCircle(Enemy enemy, float spawnRadius, float spawnAmount)
    {
        //Debug.Log($"SpawnInCircle({gameObject},{spawnRadius}, {spawnAmount})");
        for (int i = 0; i < spawnAmount; i++)
        {
            float angle = i * Mathf.PI * 2f / spawnAmount;

            Vector2 position = spawnPosition + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * spawnRadius;

            GameObject newEnemyGO = Instantiate(enemy.gameObject, position, Quaternion.identity);
            newEnemyGO.GetComponent<Enemy>().Level = waveNumber;
            NetworkServer.Spawn(newEnemyGO);
        }
    }


}
