using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;

public struct EnemyDto
{
    public string Id;
    public Vector2 Position;
    public int MaxHp;
    public int Hp;
    public List<DamageEvent> DamageEvents;
    public bool isDead;
}

