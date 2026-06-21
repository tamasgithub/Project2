
using UnityEngine;

[CreateAssetMenu (menuName = "AbilityData/DaggerData")]
public class DaggerAbilityData : PeriodicAbilityData
{
    public GameObject daggerPrefab;
    public float baseSpeed;
    public float maxLifeTime;
    public int baseDamage;
}