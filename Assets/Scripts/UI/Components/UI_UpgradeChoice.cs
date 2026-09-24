using System;
using System.Linq;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_UpgradeChoice : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private Action _onClick;
    private UpgradeChoice _choice;
    public Transform levels;
    public TextMeshProUGUI text;
    public Image sprite;
    public Color active;
    private Tween _tween;
    public void Load(UpgradeChoice choice, Action onClick, PlayerAbilityController abilities = null, int abilityLevel = 0)
    {
        _choice = choice;
        _onClick = onClick;
        switch (choice.Type)
        {
            case ChoiceType.ABILITY:

                LoadAbility(choice.AbilityName, abilities, abilityLevel);
                text.text = _choice.AbilityName.ToString();

                break;
            case ChoiceType.STAT:
                var op = _choice.IsFlat ? '+' : '*';
                text.text = $"{_choice.StatName}: {op} {_choice.Value}";
                break;
        }

    }
    public void OnPointerClick(PointerEventData eventData)
    {
        _onClick();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        DOTween.Kill(_tween);
        _tween = transform.DOScale(Vector3.one * 1.4f, 0.3f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        DOTween.Kill(_tween);
        _tween = transform.DOScale(Vector3.one, 0.3f);
    }

    private void LoadAbility(AbilityName abilityName, PlayerAbilityController abilities, int level)
    {
        // The dots show how far the player has already levelled this ability, none when they
        // do not own it yet. The level comes from the request now: it is read on the server,
        // where the Ability objects actually live.
        for (int i = 0; i < level && i < levels.childCount; i++)
        {
            levels.GetChild(i).GetComponent<Image>().color = active;
        }

        // The icon comes from the prefab's ability data, which is present on the client too,
        // and is shown whether or not the ability is already owned.
        if (abilities == null) return;
        AbilityData data = abilities.abilityData.FirstOrDefault(x => x.name == abilityName).data;
        if (data != null) sprite.sprite = data.sprite;
    }
}