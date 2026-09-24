using System.Collections.Generic;
using System.Linq;

/// <summary>One ability's level as it was on the server when the level up happened.</summary>
[System.Serializable]
public struct AbilityLevel
{
    public AbilityName name;
    public int level;
}

/// <summary>
/// The upgrade options the server offered for one level up, as the client sees them.
///
/// The options are rolled on the server (see <see cref="Roll"/>) and kept there; the client
/// only displays them and answers with the index it picked.
/// </summary>
public class UpgradeRequest
{
    public readonly List<UpgradeChoice> choices;

    // The Ability objects live on the server only, so a client cannot read their level from
    // them. They travel with the offer instead.
    private readonly Dictionary<AbilityName, int> abilityLevels = new();

    private static readonly List<UpgradeChoice> pool = new()
    {
        //Abilities
        new UpgradeChoice( AbilityName.DaggerAbility),
        new UpgradeChoice( AbilityName.BombAbility),
        new UpgradeChoice( AbilityName.KnifeAbility),
        new UpgradeChoice(AbilityName.ChakramAbility),
        //Stats
        new UpgradeChoice(StatName.MAX_HP, 1),
        new UpgradeChoice(StatName.MOVEMENTSPEED,1.5f, false),
        new UpgradeChoice(StatName.PROJECTILE_SIZE,1.2f,false),
        new UpgradeChoice(StatName.DAMAGE, 1),
        new UpgradeChoice(StatName.PIERCE, 1),
        new UpgradeChoice(StatName.AREA_OF_EFFECT, 1.2f, false),
    };

    /// <summary>Server side: picks the options for one level up.</summary>
    public static UpgradeChoice[] Roll(int count)
    {
        var rnd = new System.Random();
        return pool.OrderBy(x => rnd.Next()).Take(count).ToArray();
    }

    public UpgradeRequest(UpgradeChoice[] choices, AbilityLevel[] levels)
    {
        this.choices = choices != null ? choices.ToList() : new List<UpgradeChoice>();

        if (levels != null)
        {
            foreach (AbilityLevel entry in levels)
            {
                abilityLevels[entry.name] = entry.level;
            }
        }
    }

    /// <summary>Level of an ability the player already owns, 0 otherwise.</summary>
    public int LevelOf(AbilityName name) =>
        abilityLevels.TryGetValue(name, out int level) ? level : 0;
}
