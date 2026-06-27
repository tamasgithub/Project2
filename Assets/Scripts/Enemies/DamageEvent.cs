public struct DamageEvent
{
    public int amount;
    public DamageFlag flag;
    
    public DamageEvent(int amount, DamageFlag flag = DamageFlag.NONE)
    {
        this.amount = amount;
        this.flag = flag;
    }
}

public enum DamageFlag
{
    NONE,
    PLAYER,
    ENEMY,
    STATUSEFFECT,
}