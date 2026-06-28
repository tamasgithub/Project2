using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class EnemyVisual : MonoBehaviour
{
    public GameObject healthBar;
    public Image healtBarSprite;
    private SpriteRenderer _renderer;
    private MaterialPropertyBlock _propertyBlock;
    public float hitflashSpeed = 0.2f;
    private Tween _hitflash;
    private Color _originalColor;
    private Color _currentColor;

    void Awake()
    {
        _propertyBlock = new MaterialPropertyBlock();
        _renderer = GetComponent<SpriteRenderer>();
        _originalColor = _renderer.color;

    }

    public void UpdateStats(EnemyDto dto)
    {
        if (!healthBar.gameObject.activeSelf) return;
        healtBarSprite.fillAmount = (float) dto.Hp / (float) dto.MaxHp;
    }

    public void HandleDamageEvent(DamageEventDto dto)
    {
        healthBar.gameObject.SetActive(true);
        // DOTween.Kill(_hitflash);
        // // SetSpriteColor(Color.white);
        // _currentColor = Color.white;
        // _renderer.color = Color.white;
        // _hitflash = DOTween.To(() => _currentColor, (x) => { _renderer.color = x; _currentColor = x; }, _originalColor, hitflashSpeed);

    }

    private void SetSpriteColor(Color color)
    {
        _renderer.GetPropertyBlock(_propertyBlock);
        _propertyBlock.SetColor("_Color", color);
        _renderer.SetPropertyBlock(_propertyBlock);
    }
}
