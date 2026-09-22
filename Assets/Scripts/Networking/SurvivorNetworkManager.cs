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
    // A lobby owns a lobby scene and, once started, a game scene. Both are unloaded and the
    // lobby id is released again as soon as the last connection of that lobby is gone; see
    // TryCloseLobby.
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
	
    // Host mode is deliberately not supported: in one process that is both server and client,
    // "which GameScene is mine?" has two different answers, and every scene lookup would need
    // a special case. Run a server (or a server build) and connect with separate clients.
    public override void OnStartHost()
    {
        Debug.LogError("Host mode is not supported. Start a server and connect with a separate client instead.");
        StartCoroutine(RefuseHostMode());
    }

    private IEnumerator RefuseHostMode()
    {
        yield return null;
        StopHost();
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

    public override void OnStopServer()
    {
        // OnStartServer subscribes; without this a restarted server would run OnPlayerLeft
        // once per previous run.
        SurvivorNetworkManager.PlayerLeft -= OnPlayerLeft;
        base.OnStopServer();
    }

    private void OnPlayerLeft(NetworkConnectionToClient conn)
    {
        if (conn.authenticationData is int leftLobbyId
            && games.TryGetValue(leftLobbyId, out Scene leftGameScene)
            && conn.identity != null)
        {
            GameContext.For(leftGameScene)?.DetachPlayer(conn.identity.GetComponent<Player>());
        }

        foreach (List<NetworkConnectionToClient> lobbyMembersList in clientsInLobbies.Values)
        {
            lobbyMembersList.RemoveAll(member => member == conn);
        }
        foreach (List<NetworkConnectionToClient> lobbyMembersList in clientsInGames.Values)
        {
            lobbyMembersList.RemoveAll(member => member == conn);
        }

        if (conn.authenticationData is int lobbyId)
        {
            TryCloseLobby(lobbyId);
        }
    }

    /// <summary>
    /// Drops a lobby once its last connection is gone. Without this the server keeps simulating
    /// an empty game scene forever and the lobby id stays taken for the lifetime of the process.
    /// </summary>
    [Server]
    private void TryCloseLobby(int lobbyId)
    {
        bool lobbyOccupied = clientsInLobbies.TryGetValue(lobbyId, out List<NetworkConnectionToClient> inLobby) && inLobby.Count > 0;
        bool gameOccupied = clientsInGames.TryGetValue(lobbyId, out List<NetworkConnectionToClient> inGame) && inGame.Count > 0;

        if (lobbyOccupied || gameOccupied) return;
        if (!clientsInLobbies.ContainsKey(lobbyId)) return; // already closed

        StartCoroutine(CloseLobby(lobbyId));
    }

    [Server]
    private IEnumerator CloseLobby(int lobbyId)
    {
        Debug.Log($"Closing empty lobby {lobbyId:X}");

        // Removing the reservation first releases the id, and tells a CreateLobby or CreateGame
        // coroutine that is still loading a scene that nobody is waiting for it any more.
        clientsInLobbies.Remove(lobbyId);
        clientsInGames.Remove(lobbyId);

        if (games.Remove(lobbyId, out Scene gameScene) && gameScene.IsValid() && gameScene.isLoaded)
        {
            // Unloading destroys the scene's objects, which takes the GameContext, the pools and
            // every spawned NetworkIdentity of this lobby with it.
            yield return SceneManager.UnloadSceneAsync(gameScene);
        }

        if (lobbies.Remove(lobbyId, out Scene lobbyScene) && lobbyScene.IsValid() && lobbyScene.isLoaded)
        {
            yield return SceneManager.UnloadSceneAsync(lobbyScene);
        }

        yield return Resources.UnloadUnusedAssets();
    }

    [Server]
    void OnLobbyRequest(NetworkConnectionToClient conn, LobbyRequestMessage msg)
    {
        Debug.Log($"Server received LobbyRequestMessage {msg}");
        int lobbyId;

        if (msg.createNew)
        {
            // Reserve the id here and not in the coroutine: loading the scene takes several
            // frames, and a second request in that window must not draw the same number.
            // CloseLobby removing this entry is what releases the id again.
            while (clientsInLobbies.ContainsKey(lobbyId = UnityEngine.Random.Range(0, int.MaxValue)))
            {
                Debug.Log($"Random lobbyId {lobbyId} already taken");
            }
            clientsInLobbies[lobbyId] = new List<NetworkConnectionToClient>();
            StartCoroutine(CreateLobby(conn, lobbyId));
        }
        else
        {
            lobbyId = msg.lobbyId;
            if (!lobbies.ContainsKey(lobbyId))
                return;

            // Joining a lobby whose game is already running would strand the player: the game
            // cannot be started a second time, so they would sit in a lobby scene forever and
            // keep it from being closed once the players in the game are gone.
            if (games.ContainsKey(lobbyId))
            {
                Debug.Log($"Refusing to join lobby {lobbyId:X}: its game has already started");
                return;
            }

            if (clientsInLobbies.TryGetValue(lobbyId, out List<NetworkConnectionToClient> members)
                && members.Count >= maxPlayersPerLobby)
            {
                Debug.Log($"Refusing to join lobby {lobbyId:X}: it is full");
                return;
            }

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

        // Scene objects of an additively loaded scene are not spawned automatically.
        NetworkServer.SpawnObjects();

        // One camera and one AudioListener per lobby scene, none of which the server renders.
        SceneViewControl.Disable(scene);

        // The creator can disconnect while the scene loads; CloseLobby then already released
        // the id and nobody is coming for this scene.
        if (!clientsInLobbies.ContainsKey(lobbyId))
        {
            Debug.Log($"Lobby {lobbyId:X} was abandoned while its scene loaded, unloading it again");
            yield return SceneManager.UnloadSceneAsync(scene);
            yield break;
        }

        lobbies[lobbyId] = scene;

        // The server needs this as well: the Player events are static, so each lobby scene has
        // to be able to tell its own players from those of every other lobby in this process.
        Lobby lobby = HierarchyUtility.FindInScene<Lobby>(scene);
        if (lobby != null)
        {
            lobby.LobbyId = lobbyId;
        }

        conn.Send(new LobbySceneMessage
        {
            lobbyId = lobbyId
        });
    }

    [Server]
    IEnumerator JoinLobby(NetworkConnectionToClient conn, int lobbyId)
    {
        yield return null;

        // Membership is registered in exactly one place, MovePlayerToLobby. Adding the
        // connection here as well put joining players into the list twice: they received every
        // lobby broadcast twice (two GameSceneMessages, hence two additively loaded game
        // scenes) and one Remove never took them out again.
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

            if (!lobbies.TryGetValue(lobbyId, out Scene scene)
                || !clientsInLobbies.TryGetValue(lobbyId, out List<NetworkConnectionToClient> members))
            {
                Debug.LogWarning($"Client {conn} reported ready for lobby {lobbyId:X}, which no longer exists");
                return;
            }

            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, scene);
            NetworkServer.RebuildObservers(conn.identity, true);
            Debug.Log($"Server: Setting {player.gameObject.name}'s lobby id to {lobbyId}");
            if (!members.Contains(conn))
            {
                members.Add(conn);
            }
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

        // Scene objects of an additively loaded scene are not spawned automatically.
        NetworkServer.SpawnObjects();

        if (!clientsInLobbies.TryGetValue(lobbyId, out List<NetworkConnectionToClient> members) || members.Count == 0)
        {
            Debug.Log($"Lobby {lobbyId:X} ran empty while its game scene loaded, unloading it again");
            yield return SceneManager.UnloadSceneAsync(scene);
            yield break;
        }

        games[lobbyId] = scene;
        clientsInGames[lobbyId] = new List<NetworkConnectionToClient>();

        // Everything this lobby's scene needs instead of the former static singletons.
        GameContext.Create(scene, lobbyId);

        foreach (NetworkConnectionToClient conn in members.ToArray())
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

            if (!games.TryGetValue(lobbyId, out Scene scene)
                || !clientsInGames.TryGetValue(lobbyId, out List<NetworkConnectionToClient> gameMembers))
            {
                Debug.LogWarning($"Client {conn} reported ready for the game of lobby {lobbyId:X}, which no longer exists");
                return;
            }

            SceneManager.MoveGameObjectToScene(conn.identity.gameObject, scene);
            NetworkServer.RebuildObservers(conn.identity, true);
            Debug.Log($"Server: Setting isInGame of {player.gameObject.name} to true");
            if (clientsInLobbies.TryGetValue(lobbyId, out List<NetworkConnectionToClient> lobbyMembers))
            {
                lobbyMembers.RemoveAll(member => member == conn);
            }
            if (!gameMembers.Contains(conn))
            {
                gameMembers.Add(conn);
            }
            player.isInGame = true;

            // The player was spawned outside this scene, so its context bound components
            // (AreaTrigger, DamageSource) have to be rebound to the lobby it just entered.
            GameContext.For(scene)?.AttachPlayer(player);

            ShowSceneToObservers(scene);
        }
        else
        {
            Debug.LogError($"Client {conn} sent LobbySceneReadyMessage but the player object is null!");
        }
    }

    /// <summary>
    /// Recomputes the observers of everything already spawned in a game scene.
    ///
    /// Mirror decides who observes an object when it is spawned. The object pool is filled while
    /// the game scene loads, which is before any player has been moved into it, so those objects
    /// spawn with zero observers -- pooled loot and damage numbers then never reach any client.
    /// Doing this explicitly once per joining player is deterministic, and cheap enough: it runs
    /// only when somebody enters the game.
    /// </summary>
    [Server]
    private void ShowSceneToObservers(Scene scene)
    {
        foreach (NetworkIdentity identity in HierarchyUtility.FindAllInScene<NetworkIdentity>(scene))
        {
            // netId 0 means Mirror has not spawned it, so there is nothing to show yet.
            if (identity.netId != 0)
            {
                NetworkServer.RebuildObservers(identity, false);
            }
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
        if (!NetworkClient.active)
        {
            StartClient();
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
            StartClient();
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

        Debug.Log($"Loading game scene");
        yield return SceneManager.LoadSceneAsync("GameScene", LoadSceneMode.Additive);
        NetworkClient.Send(new GameSceneReadyMessage());
    }

    #endregion
}