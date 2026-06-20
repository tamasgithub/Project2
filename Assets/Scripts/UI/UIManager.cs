
using System.Xml.XPath;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Player player;
    public Image HpBar;
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
        if (!player.isOwned) {
            Destroy(gameObject);
            return;
        }
        player = p;
        player.OnStatChanged += UpdateUI;
        player.OnLevelUp += ShowUpgradeChoices;
    }

    public void UpdateUI()
    {
        Debug.Log($"{player.Hp}  {player.MaxHp}");
        HpBar.fillAmount = (float)player.Hp / (float)player.MaxHp;
    }

    public void ShowUpgradeChoices(UpgradeRequest request)
    {
        
        foreach (var choice in request.choices)
        {
            var c = Instantiate(upgradeChoicePrefab, upgradeChoices);
            c.GetComponent<UI_UpgradeChoice>().Load(choice,
            () =>
            {
                player.RpcSubmitUpgradeChoice(choice);

                foreach (Transform child in upgradeChoices.transform)
                {
                    Debug.Log("Test");
                    Destroy(child.gameObject);
                }     
            }
            );
        }
    }
    

}
