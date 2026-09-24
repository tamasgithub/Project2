
using System.Collections.Generic;
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

    // Level ups that arrived while a choice was still on screen. They are offered one after
    // another instead of piling their cards onto the ones already shown.
    private readonly Queue<UpgradeRequest> pendingUpgrades = new();
    private bool upgradeVisible;

    void Start()
    {
        // The canvas camera is bound by Player whenever it enters a scene. Camera.main here
        // would be whichever camera happens to be enabled while the player still sits in the
        // lobby, and that one gets disabled as soon as the game scene takes over.
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
        player.OnLevelUp += EnqueueUpgradeChoices;
    }

    public void UpdateUI()
    {
        HpBar.fillAmount = (float)player.Hp / (float)player.MaxHp;
    }

    private void UpdateXpBar(long currentXp, long xpToNextLevel)
    {
        XpBar.fillAmount = Mathf.Clamp01((float)currentXp / (float)xpToNextLevel);
    }

    public void EnqueueUpgradeChoices(UpgradeRequest request)
    {
        pendingUpgrades.Enqueue(request);

        if (!upgradeVisible)
        {
            ShowNextUpgrade();
        }
    }

    private void ShowNextUpgrade()
    {
        ClearUpgradeCards();

        if (pendingUpgrades.Count == 0)
        {
            upgradeVisible = false;
            return;
        }

        upgradeVisible = true;
        UpgradeRequest request = pendingUpgrades.Dequeue();
        PlayerAbilityController abilities = player.GetComponent<PlayerAbilityController>();

        for (int i = 0; i < request.choices.Count; i++)
        {
            UpgradeChoice choice = request.choices[i];
            int choiceIndex = i; // captured per card, not shared by the closures
            int level = choice.Type == ChoiceType.ABILITY ? request.LevelOf(choice.AbilityName) : 0;

            GameObject card = Instantiate(upgradeChoicePrefab, upgradeChoices);
            card.GetComponent<UI_UpgradeChoice>().Load(
                choice,
                () => OnChoiceSelected(choiceIndex),
                abilities,
                level);
        }
    }

    private void OnChoiceSelected(int choiceIndex)
    {
        // Only the index goes to the server: it still holds the options it offered.
        player.CmdSubmitUpgradeChoice(choiceIndex);
        ShowNextUpgrade();
    }

    private void ClearUpgradeCards()
    {
        foreach (Transform child in upgradeChoices)
        {
            // Deactivate as well: Destroy only takes effect at the end of the frame, and the
            // next set of cards is created right away.
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
