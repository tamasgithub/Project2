public static class MappingFunctions
{
    static public EnemyDto ToDto(this ServerEnemy enemy)
    {
        return new EnemyDto()
        {
            Id = enemy.id,
            Position = enemy.Position,
            MaxHp = enemy.MaxHp,
            Hp = enemy.Hp,
            DamageEvents = enemy.damageEvents
        };
    }
}