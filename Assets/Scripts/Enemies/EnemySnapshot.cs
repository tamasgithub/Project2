using System.Collections.Generic;
using Mirror;

public struct EnemySnapshot : NetworkMessage
{
    public List<EnemyDto> enemies;
}