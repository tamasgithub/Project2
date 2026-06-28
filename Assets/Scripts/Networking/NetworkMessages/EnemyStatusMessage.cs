using System.Collections.Generic;
using Mirror;

public struct EnemyStatusMessage : NetworkMessage
{
    public List<EnemyDto> enemies;
}