
using System.Xml.XPath;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Player player;
    public Image HpBar;
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
    }
    
    public void UpdateUI()
    {
        Debug.Log($"{player.Hp}  {player.MaxHp}");
        HpBar.fillAmount = (float)player.Hp / (float)player.MaxHp;
    }
}
