using System;
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
    public Color active;
    private Tween _tween;
    public void Load(UpgradeChoice choice, Action onClick)
    {
        _choice = choice;
        _onClick = onClick;
        switch (choice.Type)
        {
            case ChoiceType.ABILITY:
                // LoadAbility();
                text.text = _choice.AbilityName.ToString();
                break;
            case ChoiceType.STAT:
                var op = _choice.IsFlat ? '+' : '*';
                text.text = $"{ _choice.StatName}: {op} {_choice.Value}";
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

    // private void LoadAbility()
    // {
    //     for (int i = 0; i <= _choice.Ability.Level; i++)
    //     {
    //         levels.GetChild(i).GetComponent<Image>().color = active;
    //     }
    // }
}