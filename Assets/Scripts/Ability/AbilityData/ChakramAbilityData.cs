using UnityEngine;

[CreateAssetMenu (menuName = "AbilityData/ChakramData")]
public class ChakramAbilityData : AbilityData
{
    public GameObject orbital;
    public UpgradableStat<int> orbitalCount = new UpgradableStat<int>();
    public UpgradableStat<float> hoverDuration = new UpgradableStat<float>();
}