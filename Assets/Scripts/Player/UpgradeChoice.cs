public class UpgradeChoice
{
    public ChoiceType ChoiceType { get; }
    public UpgradeChoice()
    {
        
    }
    public UpgradeChoice(ChoiceType choiceType)
    {
        ChoiceType = choiceType;
    }
}
public enum ChoiceType
{
    ABILITY,
    ITEM
}