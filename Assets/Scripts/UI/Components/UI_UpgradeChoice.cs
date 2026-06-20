using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UI_UpgradeChoice : MonoBehaviour, IPointerClickHandler
{
    private Action _onClick;
    private UpgradeChoice _choice;
    public void Load(UpgradeChoice choice, Action onClick)
    {
        _choice = choice;
        _onClick = onClick;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        _onClick();
    }
}