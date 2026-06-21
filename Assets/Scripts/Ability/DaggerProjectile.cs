using Mirror;
using UnityEngine;

public class DaggerProjectile : Projectile
{
    protected override void LoadBaseStats(int level, AbilityData abilityData)
    {
        if (abilityData is DaggerAbilityData daggerData)
        {
            baseSpeed = daggerData.baseSpeed; 
            maxLifeTime = daggerData.maxLifeTime;
            damage = Mathf.RoundToInt(Mathf.Pow(1.5f, level) * daggerData.baseDamage);
            pierce = level - 1;
        } else
        {
            Debug.LogError("Dagger ability was created with other AbilityData than DaggerAbilitData!");
        }
    }
}
