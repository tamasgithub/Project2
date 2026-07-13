using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SurvivorNetworkManager : NetworkManager
{
    public int minPlayersPerLobby = 2;
    public int maxPlayersPerLobby = 5;
    // the lobby scene for a given lobby id might be empty if all players are in the game scene
    // TODO: clean up empty game scenes as noone can enter there anymore, as well as empty lobby scenes that have no corresponding game scene
    [Scene] public string lobbyScene;
    private Dictionary<int, Scene> lobbies;
    private Dictionary<int, List<NetworkConnectionToClient>> clientsInLobbies;
    [Scene] public string gameScene;
    private Dictionary<int, Scene> games;
    private Dictionary<int, List<NetworkConnectionToClient>> clientsInGames;

    // used by the client
    private LobbyRequestMessage lobbyRequestMessage;

    // these only work on server side
    // on client side, rely on OnClientStart/OnClientStop -> Action.Invoke
    public static event Action<NetworkConnectionToClient> PlayerJoined;
    public static event Action<NetworkConnectionToClient> PlayerLeft;

    public override void Start()
    {
        base.Start();
        if (Application.isBatchMode)
        {
           StartServer();
            Debug.Log("Server Started"); 
        }
        
    }
	
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

    public override void OnClientDisconnect()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    #region SERVER

    public override void OnStartServer()
    {
        SurvivorNetworkManager.PlayerLeft += OnPlayerLeft;

        NetworkServer.RegisterHandler<LobbyRequestMessage>(OnLobbyRequest);
        NetworkServer.RegisterHandler<LobbySceneReadyMessage>(MovePlayerToLobby);
        NetworkServer.RegisterHandler<GameStartRequestMessage>(OnGameStartRequest);
        NetworkServer.RegisterHandler<GameSceneReadyMessage>(MovePlayerToGame);
        lobbies = new();
        clientsInLobbies = new();
        games = new();
        clientsInGames = new();
    }

    private void OnPlayerLeft(NetworkConnectionToClient conn)
    {
        foreach (List<NetworkConnectionToClient> lobbyMembersList in clientsInLobbies.Values)
        {
            lobbyMembersList.Remove(conn);
        }
        foreach (List<NetworkConnectionToClient> lobbyMembersList in clientsInGames.Values)
        {
            lobbyMembersList.Remove(conn);
        }
    }

    [Server]
    void OnLobbyRequest(NetworkConnectionToClient conn, LobbyRequestMessage msg)
    {
        Debug.Log($"Server received LobbyRequestMessage {msg}");
        int lobbyId;

        if (msg.createNew)
        {
            while (lobbies.ContainsKey(lobbyId = UnityEngine.Random.Range(0, int.MaxValue)))
            {
                Debug.Log($"Random lobbyId {lobbyId} already taken");
            }
            StartCoroutine(CreateLobby(conn, lobbyId));
        }
        else
        {
            lobbyId = msg.lobbyId;
            if (!lobbies.ContainsKey(lobbyId))
                return;

            StartCoroutine(JoinLobby(conn, lobbyId));
        }
        Player player = conn.identity.GetComponent<Player>();
        string userName = string.IsNullOrWhiteSpace(msg.userName) ? "Player" + conn.connectionId : msg.userName;
        player.gameObject.name = userName;
        player.userName = userName;
        player.joinedLobbyTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        conn.authenticationData = lobbyId;
    }

    [Server]
    IEnumerator CreateLobby(NetworkConnectionToClient conn, int lobbyId)
    {
        yield return SceneManager.LoadSceneAsync(lobbyScene, LoadSceneMode.Additive);
        Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

        lobbies[lobbyId] = scene;
        clientsInLobbies[lobbyId] = new List<NetworkConnectionToClient>();

        conn.Send(new LobbySceneMessage
        {
            lobbyId = lobbyId
        });
    }

    [Server]
    IEnumerator JoinLobby(NetworkConnectionToClient conn, int lobbyId)
    {
        yield return null;

        clientsInLobbies[lobbyId].Add(conn);

        conn.Send(new LobbySceneMessage
        {
            lobbyId = lobbyId
        });
    }

    [Server]
    void MovePlayerToLobby(NetworkConnectionToClient conn, LobbySceneReadyMessage msg)
    {
        if (conn.identity != null)
        {
            int lobbyId = (int)conn.authenticationData;
            Player player = conn.identity.GetComponent<Player>();
            Scene scene = lobbies[lobbyId];
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, scene);
            NetworkServer.RebuildObservers(conn.identity, true);
            Debug.Log($"Server: Setting {player.gameObject.name}'s lobby id to {lobbyId}");
            clientsInLobbies[lobbyId].Add(conn);
            player.lobbyId = lobbyId;
            conn.identity.AssignClientAuthority(conn);
        }
        else
        {
            Debug.LogError($"Client {conn} sent LobbySceneReadyMessage but the player object is null!");
        }
    }

    [Server]
    void OnGameStartRequest(NetworkConnectionToClient conn, GameStartRequestMessage msg)
    {
        Debug.Log($"Server received GameStartRequestMessage {msg}");

        int lobbyId = msg.lobbyId;

        if (!ValidateGameStartRequest(conn.identity.GetComponent<Player>(), lobbyId))
        {
            return;
        }
        

        StartCoroutine(CreateGame(lobbyId));
    }

    [Server]
    private bool ValidateGameStartRequest(Player requestor, int lobbyId)
    {
        if (!lobbies.ContainsKey(lobbyId) || !clientsInLobbies.ContainsKey(lobbyId))
        {
            Debug.LogError("Game start requested for unknown lobby " + lobbyId + "!");
            return false;
        }
        if (games.ContainsKey(lobbyId))
        {
            Debug.LogError("Game start requested for lobby " + lobbyId + " where the game already started!");
            return false;
        }
        foreach (NetworkConnectionToClient conn in clientsInLobbies[lobbyId])
        {
            Player player = conn.identity.GetComponent<Player>();
            if (!player.isReady)
            {
                Debug.LogWarning("Game start requested for lobby with unready player");
                return false;
            }
            if (player != requestor && player.joinedLobbyTimestamp < requestor.joinedLobbyTimestamp)
            {
                Debug.LogError("Game start requested for lobby " + lobbyId + " where the requestor " + requestor + " is not the lobby owner!");
                return false;
            }
        }
        return true;
    }

    [Server]
    IEnumerator CreateGame(int lobbyId)
    {
        yield return SceneManager.LoadSceneAsync(gameScene, LoadSceneMode.Additive);
        Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

        games[lobbyId] = scene;
        clientsInGames[lobbyId] = new List<NetworkConnectionToClient>();

        HierarchyUtility.FindInScene<EnemyManager>(scene).lobbyId = lobbyId;

        foreach (NetworkConnectionToClient conn in clientsInLobbies[lobbyId])
        {
            conn.Send(new GameSceneMessage());
        }
    }

    [Server]
    void MovePlayerToGame(NetworkConnectionToClient conn, GameSceneReadyMessage msg)
    {
        if (conn.identity != null)
        {
            int lobbyId = (int)conn.authenticationData;
            Player player = conn.identity.GetComponent<Player>();
            Scene scene = games[lobbyId];
            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, scene);
            NetworkServer.RebuildObservers(conn.identity, true);
            Debug.Log($"Server: Setting isInGame of {player.gameObject.name} to true");
            clientsInLobbies[lobbyId].Remove(conn);
            clientsInGames[lobbyId].Add(conn);
            player.isInGame = true;
        }
        else
        {
            Debug.LogError($"Client {conn} sent LobbySceneReadyMessage but the player object is null!");
        }
    }

    public void SendToClientsInLobby<T>(T msg, int lobbyId) where T : struct, NetworkMessage
    {
        foreach (NetworkConnectionToClient conn in clientsInLobbies[lobbyId])
        {
            conn.Send(msg);
        }
    }

    public void SendToClientsInGame<T>(T msg, int lobbyId) where T : struct, NetworkMessage
    {
        foreach (NetworkConnectionToClient conn in clientsInGames[lobbyId])
        {
            conn.Send(msg);
        }
    }

    #endregion

    #region CLIENT

    public override void OnStartClient()
    {
        NetworkClient.ReplaceHandler<LobbySceneMessage>(OnLobbySceneMessage, false);
        NetworkClient.ReplaceHandler<GameSceneMessage>(OnGameSceneMessage, false);
    }

    public void RequestLobbyCreation(string userName)
    {
        if (!NetworkServer.activeHost)
        {
            GetComponent<NetworkManager>().StartClient();
        }
        lobbyRequestMessage = new LobbyRequestMessage
        {
            createNew = true,
            lobbyId = -1,
            userName = userName
        };
        StartCoroutine(WaitForConnectionAndSendLobbyRequest());
    }

    public void RequestLobbyJoining(string userName, int lobbyId)
    {
        if (!NetworkClient.active)
        {
            GetComponent<NetworkManager>().StartClient();
        }
        lobbyRequestMessage = new LobbyRequestMessage
        {
            createNew = false,
            lobbyId = lobbyId,
            userName = userName
        };
        StartCoroutine(WaitForConnectionAndSendLobbyRequest());
    }

    IEnumerator WaitForConnectionAndSendLobbyRequest()
    {
        yield return new WaitUntil(() =>
            NetworkClient.isConnected &&
            NetworkClient.ready &&
            NetworkClient.localPlayer != null
        );

        NetworkClient.Send(lobbyRequestMessage);
    }

    [Client]
    void OnLobbySceneMessage(LobbySceneMessage msg)
    {
        StartCoroutine(HandleLobbyScene(msg));
    }

    [Client]
    IEnumerator HandleLobbyScene(LobbySceneMessage msg)
    {
        Debug.Log($"Client received {msg}");
        if (NetworkClient.activeHost || (NetworkClient.active && NetworkServer.active))
        {
            // don't handle scenes at all, the server part does everything already
            NetworkClient.Send(new LobbySceneReadyMessage());
        }
        else
        {
            Debug.Log($"Loading lobby scene");
            yield return SceneManager.LoadSceneAsync("LobbyScene", LoadSceneMode.Additive);
            Lobby lobby = FindAnyObjectByType<Lobby>();

            if (lobby == null)
            {
                Debug.LogError("Lobby component not found in loaded scene.");
                yield break;
            }

            lobby.LobbyId = msg.lobbyId;
            NetworkClient.Send(new LobbySceneReadyMessage());
        }
    }

    [Client]
    public void RequestGameStart(int lobbyId)
    {
        NetworkClient.Send(new GameStartRequestMessage
        {
            lobbyId = lobbyId
        });
    }

    [Client]
    void OnGameSceneMessage(GameSceneMessage msg)
    {
        StartCoroutine(HandleGameScene(msg));
    }

    [Client]
    IEnumerator HandleGameScene(GameSceneMessage msg)
    {
        Debug.Log($"Client received {msg}");
        if (NetworkClient.activeHost || (NetworkClient.active && NetworkServer.active))
        {
            // don't handle scenes at all, the server part does everything already
            NetworkClient.Send(new GameSceneReadyMessage());
        }
        else
        {
            Debug.Log($"Loading game scene");
            yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
            NetworkClient.Send(new GameSceneReadyMessage());
        }
    }

    #endregion
}