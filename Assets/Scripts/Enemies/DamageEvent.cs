using System;

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

[Flags]
public enum DamageFlag
{
    NONE = 0b_0000_0000,
    PLAYER = 0b_0000_0001,
    ENEMY = 0b_0000_0010,
    POISON = 0b_0000_0100,
    BLEED = 0b_0000_1000,
}