using Mirror;

public class EntityData
{

    public int MaxHp { get; private set; } = 1;
    public int Hp { get; private set; } = 1;
    public float MovementSpeed { get; private set; } = 1f;
    
    public EntityData(int maxHp, float movementSpeed)
    {
        MaxHp = maxHp;
        Hp = maxHp;
        MovementSpeed = movementSpeed;
    }
}