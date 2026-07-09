using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class Player : Entity
{

    [SyncVar(hook = nameof(OnStringSnycVarChanged))]
    public string userName;
    [SyncVar(hook = nameof(OnLobbyIdChanged))]
    public int lobbyId = -1;
    [SyncVar(hook = nameof(OnLongSnycVarChanged))]
    public long joinedLobbyTimestamp = -1;
    [SyncVar(hook = nameof(OnBoolSnycVarChanged))]
    public bool isReady = false;
    [SyncVar(hook = nameof(OnIsInGameChanged))]
    public bool isInGame = false;


    public static event Action<Player> OnPlayerMovedToLobby;
    public static event Action<Player> OnPlayerMovedToGame;


    public void OnLobbyIdChanged(int oldLobbyId, int lobbyId)
    {
        if (oldLobbyId == lobbyId) return;

        this.lobbyId = lobbyId;
        Debug.Log($"Player {userName}: lobbyId updated {this.lobbyId} -> {lobbyId}");

        Lobby lobby = FindAnyObjectByType<Lobby>();

        if (lobbyId == lobby.LobbyId)
        {
            MoveToClientLobbyScene();
        }
        else
        {
            Debug.Log("lobbyId does not match lobby.lobbyId " + lobby.LobbyId);
        }
    }

    [Client]
    private void MoveToClientLobbyScene()
    {
        Scene scene = SceneManager.GetSceneByName("LobbyScene");
        if (scene == null)
        {
            Debug.LogError($"LobbyScene is null");
            return;
        }
        else if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Scene {scene} is valid={scene.IsValid()} and loaded={scene.isLoaded}!");
            return;
        }
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        Debug.Log($"Player {userName}: moved to the LobbyScene. (lobbyId = {lobbyId})");
        OnPlayerMovedToLobby?.Invoke(this);
    }

    public void OnIsInGameChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"Player {userName}: isInGame updated {oldValue} -> {newValue}");

        if (oldValue && !newValue)
        {
            // move out of the game, back to the lobby?
            // won't be able to start a new game from there though
            // because the game scene is not cleaned up and the lobbyId will stay already taken
            // OnLobbyIdChanged(lobbyId, lobbyId);

            // instead, return to the main menu, by disconnecting
            FindAnyObjectByType<NetworkManager>().StopClient();
            return;
        }


        Lobby lobby = FindAnyObjectByType<Lobby>();
        if (lobbyId == lobby.LobbyId)
        {
            MoveToClientGameScene();
        }
        else
        {
            Debug.Log("lobbyId " + lobbyId + " does not match local lobbyId " + lobby.LobbyId);
        }
    }

    [Client]
    private void MoveToClientGameScene()
    {
        Scene scene = SceneManager.GetSceneByName("GameScene");
        if (scene == null)
        {
            Debug.LogError($"GameScene is null");
            return;
        }
        else if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"Scene {scene} is valid={scene.IsValid()} and loaded={scene.isLoaded}!");
            return;
        }
        SceneManager.MoveGameObjectToScene(gameObject, scene);
        Debug.Log($"Player {userName}: moved to the GameScene. (lobbyId = {lobbyId})");
        OnPlayerMovedToGame?.Invoke(this);
    }

    [Command]
    public void CmdToggleIsReady()
    {
        isReady = !isReady;
    }

    // data is irrelevant, just want to call the generic "any data has changed" callback
    public void OnStringSnycVarChanged(string _, string __) => DataChanged();
    public void OnLongSnycVarChanged(long _, long __) => DataChanged();
    public void OnBoolSnycVarChanged(bool _, bool __) => DataChanged();

}
