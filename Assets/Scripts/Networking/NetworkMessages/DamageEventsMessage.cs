using System.Collections.Generic;
using Mirror;

public struct DamageEventsMessage : NetworkMessage
{
    public List<DamageEventDto> damageEventDtos;
}