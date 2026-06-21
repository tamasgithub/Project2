using UnityEngine;

[CreateAssetMenu(menuName = "AbilityData/BombData")]
public class BombAbilityData : PeriodicAbilityData
{
    public GameObject bombPrefab;
    public float maxLifeTime;
    public int baseDamage;
    public Gradient lifeTimeColorGradient;
    public GameObject explosionPrefab;
    public float explosionSize;
    public float explosionVisualDuration;
}
