using Mirror;

public struct LobbyRequestMessage : NetworkMessage
{
    public bool createNew;
    public int lobbyId;
    public string userName;
}

public struct LobbySceneMessage : NetworkMessage
{
    public int lobbyId;
}

public struct LobbySceneReadyMessage : NetworkMessage
{
}

public struct GameStartRequestMessage : NetworkMessage
{
    public int lobbyId;
}

public struct GameSceneMessage : NetworkMessage
{
}

public struct GameSceneReadyMessage : NetworkMessage
{
}
