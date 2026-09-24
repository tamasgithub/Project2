/// <summary>
/// One offered upgrade. A struct so Mirror can serialize it: the server rolls the options and
/// sends them to the owning client, which answers with an index into that list.
/// </summary>
[System.Serializable]
public struct UpgradeChoice
{
    public ChoiceType Type;
    public AbilityName AbilityName;
    public StatName StatName;
    public bool IsFlat;
    public float Value;

    public UpgradeChoice(StatName statName, float value, bool flat = true) : this()
    {
        Type = ChoiceType.STAT;
        StatName = statName;
        Value = value;
        IsFlat = flat;
    }

    public UpgradeChoice(AbilityName name) : this()
    {
        Type = ChoiceType.ABILITY;
        AbilityName = name;
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
    BombAbility,
    ChakramAbility
}

public enum StatName
{
    MAX_HP,
    PROJECTILE_SIZE,
    MOVEMENTSPEED,
    DAMAGE,
    // Appended, never inserted: the value is serialized into the upgrade offer.
    PIERCE,
    AREA_OF_EFFECT
}
