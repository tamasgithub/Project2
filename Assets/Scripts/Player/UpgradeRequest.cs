using System.Collections.Generic;

public class UpgradeRequest
{
    public List<UpgradeChoice> choices = new();

    public void GenerateRequest()
    {
        this.choices = new List<UpgradeChoice>()
        {
            new UpgradeChoice(ChoiceType.ABILITY),
            new UpgradeChoice(ChoiceType.ABILITY),
            new UpgradeChoice(ChoiceType.ABILITY)
        };
    }
    public UpgradeRequest()
    {
        GenerateRequest();
    }
    public UpgradeRequest(List<UpgradeChoice> choices)
    {
        this.choices = choices;
    }


}