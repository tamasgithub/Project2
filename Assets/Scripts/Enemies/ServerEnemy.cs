using System.Collections.Generic;
using UnityEngine;

public class ServerEnemy : ServerEntity
{
    private readonly GameContext _context;
    private readonly IReadOnlyList<LootTableEntry> _lootTable;

    public ServerEnemy(GameContext context, int level, int baseMaxHp, float baseMovementSpeed, IReadOnlyList<LootTableEntry> lootTable)
    {
        _context = context;
        _lootTable = lootTable;
        Level = level > 0 ? level : 1;

        // Same scaling the GameObject based Enemy applied in OnStartServer.
        SetBaseData(baseMaxHp + Level * 1, baseMovementSpeed + Level * 0.1f);
    }

    public override void OnKilled()
    {
        SpawnRandomLoot();
    }

    private void SpawnRandomLoot()
    {
        // Silent returns here cost a long debugging session once; keep them loud.
        if (_context == null) { Debug.LogWarning("No loot: the enemy has no GameContext"); return; }
        if (_context.ObjectPool == null) { Debug.LogWarning("No loot: the lobby has no ObjectPool"); return; }
        if (_lootTable == null || _lootTable.Count == 0) { Debug.LogWarning("No loot: the loot table is empty"); return; }

        float roll = Random.Range(0f, GetTotalProbability());
        float cumulative = 0f;

        foreach (LootTableEntry entry in _lootTable)
        {
            cumulative += entry.probability;

            if (roll <= cumulative)
            {
                PoolableObjectType type = entry.LootType == Loot.LootType.EXP
                    ? PoolableObjectType.EXP
                    : PoolableObjectType.HP_POTION;
                _context.ObjectPool.Get(type, (Vector3)Position, Quaternion.identity);
                return;
            }
        }
    }

    private float GetTotalProbability()
    {
        float sum = 0f;
        foreach (LootTableEntry entry in _lootTable)
            sum += entry.probability;

        return sum;
    }
}
