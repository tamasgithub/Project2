
using System.Linq;
using System.Xml.XPath;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Player player;
    public Image HpBar;
    public Image XpBar;
    [Header("Upgrades People, Upgrades!")]
    public GameObject upgradeChoicePrefab;
    public Transform upgradeChoices;
    void Start()
    {
        GetComponent<Canvas>().worldCamera = Camera.main;
        Load();

    }
    public void Load()
    {

        var p = GetComponentInParent<Player>();
        if (!player.isOwned)
        {
            Destroy(gameObject);
            return;
        }
        player = p;
        player.OnStatChanged += UpdateUI;
        player.OnXpChanged += UpdateXpBar;
        player.OnLevelUp += ShowUpgradeChoices;
    }

    public void UpdateUI()
    {
        HpBar.fillAmount = (float)player.Hp / (float)player.MaxHp;
    }

    private void UpdateXpBar(long currentXp, long xpToNextLevel)
    {
        XpBar.fillAmount = Mathf.Clamp01((float)currentXp / (float)xpToNextLevel);
    }

    public void ShowUpgradeChoices(UpgradeRequest request)
    {

        foreach (var choice in request.choices)
        {
            var card = Instantiate(upgradeChoicePrefab, upgradeChoices);
            Ability ability = null;
            if (choice.Type == ChoiceType.ABILITY && request.abilities.Exists(x => x.AbilityName == choice.AbilityName))
            {
                ability = request.abilities.FirstOrDefault(x => x.AbilityName == choice.AbilityName);
            }
            card.GetComponent<UI_UpgradeChoice>().Load(choice,
            () =>
            {
                player.CmdSubmitUpgradeChoice(choice);

                foreach (Transform child in upgradeChoices.transform)
                {
                    Destroy(child.gameObject);
                }
            }
            , ability
            );
        }
    }


}
