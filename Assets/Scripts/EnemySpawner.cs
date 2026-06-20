using System;
using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject[] enemies;
    public float spawnFrequency = 3f;
    public float spawnAmount = 10f;
    public float spawnRadius = 5f;
    public Vector2 spawnPosition = Vector2.zero;

    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        StartCoroutine(PeriodicSpawning());
    }

    

    private IEnumerator PeriodicSpawning()
    {
        int enemyIndex = 0;
        while (true)
        {
            yield return new WaitForSeconds(spawnFrequency);
            SpawnInCircle(enemies[enemyIndex], spawnRadius, spawnAmount);
        }
    }

    private void SpawnInCircle(GameObject gameObject, float spawnRadius, float spawnAmount)
    {
        for (int i = 0; i < spawnAmount; i++)
        {
            float angle = i * Mathf.PI * 2f / spawnAmount;

            Vector2 position = spawnPosition + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * spawnRadius;

            Instantiate(gameObject, position, Quaternion.identity);
        }
    }


}
