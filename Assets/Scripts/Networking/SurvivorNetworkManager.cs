using System;
using Mirror;
using UnityEngine;

public class SurvivorNetworkManager : NetworkManager
{
    public static event Action<NetworkConnectionToClient> PlayerJoined;
    public static event Action<NetworkConnectionToClient> PlayerLeft;

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);
        PlayerJoined?.Invoke(conn);
    }

    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        // Invoke BEFORE base, because base destroys the player object.
        PlayerLeft?.Invoke(conn);

        base.OnServerDisconnect(conn);
    }

}
