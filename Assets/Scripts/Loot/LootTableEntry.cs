// Moved here from the deleted GameObject based Enemy, which carried its loot table as a
// serialized field on the prefab. ServerEnemy is a plain C# object, so the table is
// configured on the EnemySpawner instead.
[System.Serializable]
public struct LootTableEntry
{
    public Loot.LootType LootType;
    public float probability;
}
