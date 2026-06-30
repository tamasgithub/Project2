public interface IStatModifier
{
    public bool IsActive{ get; set; }
    public float Calculate(float input);
}
