public class StatModifierPercent : IStatModifier
{
    public float Value { get; }
    public bool IsActive { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public StatModifierPercent(float value){
        Value = value;
    }
    public float Calculate(float input)
    {
        return input *= Value;
    }
}