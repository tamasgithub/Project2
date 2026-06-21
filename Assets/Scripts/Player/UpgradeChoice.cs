using System;

public class UpgradeChoice
{
    public ChoiceType Type;

    public UpgradeChoice( StatName statName , float value , bool flat = true)
    {

        Type = ChoiceType.STAT;
        StatName = statName;
        Value = value;
        IsFlat = flat;
    }
    public UpgradeChoice(AbilityName name )
    {
        Type = ChoiceType.ABILITY;
        AbilityName = name;
    }
    public AbilityName AbilityName;
    public StatName StatName = StatName.MAX_HP;
    public bool IsFlat;
    public bool AbilityIsOwned;
    public float Value;
    public UpgradeChoice()
    {
    }

}
public enum ChoiceType
{
    ABILITY,
    STAT
}
public enum AbilityName
{
    DaggerAbility,
    KnifeAbility,
    BombAbility
}

public enum StatName
{
    MAX_HP,
    PROJECTILE_SIZE,
    MOVEMENTSPEED,
    DAMAGE
}