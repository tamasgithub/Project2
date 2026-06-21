public class StatModifierFlat : IStatModifier
{
    public float Value { get; }
    public bool IsActive { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public StatModifierFlat(float value)
    {
        Value = value;

    }

    public float Calculate(float input)
    {
        return input += Value;
    }
}