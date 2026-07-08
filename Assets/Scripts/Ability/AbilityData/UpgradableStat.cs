using UnityEngine;
using System.Linq;

[System.Serializable]
public class UpgradableStat<T> : IUpgradableStat
{
    private T _value;
    public T Value => _value;
    public UpgradableStat()
    {
        _value = steps.FirstOrDefault().Value;
    }
    public void Upgrade(int level)
    {
        var old = _value;
        for (int i = 0; i < steps.Length; i++)
        {
            if (steps[i].Level <= level)
            {
                _value = steps[i].Value;
            }
        }
        Debug.Log($"Upgraded Stat  {old} => {_value}");
    }
    public UpgradePair<T>[] steps = new UpgradePair<T>[1];

}

[System.Serializable]
public struct UpgradePair<T>
{
    public int Level;
    public T Value;
}