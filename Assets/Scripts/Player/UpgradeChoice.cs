public class UpgradeChoice
{
    public ChoiceType ChoiceType { get; }

    public Ability Ability{ get; }
    public UpgradeChoice() { }
    public UpgradeChoice(Ability ability = null)
    {
        if(ability != null)
        {
            ChoiceType = ChoiceType.ABILITY;
            Ability = ability;
            return;
        }
        
    }
}
public enum ChoiceType
{
    ABILITY,
    STAT
}