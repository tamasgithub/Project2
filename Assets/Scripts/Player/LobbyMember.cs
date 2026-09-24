using System;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
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
        this.lobbyId = lobbyId;
        Debug.Log($"Player {userName}: lobbyId updated {this.lobbyId} -> {lobbyId}");

        if (ShouldJoinLobby())
        {
            MoveToClientLobbyScene();
        }
        else
        {
            Debug.Log("lobbyId does not match lobby.lobbyId");
        }
    }

    private bool ShouldJoinLobby()
    {
        Lobby lobby = FindAnyObjectByType<Lobby>();
        return lobby != null && lobbyId == lobby.LobbyId;
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

        if (isOwned)
        {
            SceneViewControl.ActivateOnly(scene);
            BindUiToSceneCamera(scene);
            ShowInGameHud(false);
        }

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


        Lobby lobby = FindAnyObjectByType<Lobby>(FindObjectsInactive.Include);
        if (lobbyId == lobby.LobbyId)
        {
            MoveToClientGameScene();
            GetComponent<PlayerInputController>().enabled = true;
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

        // Only the local player drives the camera. This used to hang off the static
        // OnPlayerMovedToGame event, with every player instance on the client subscribing a
        // lambda that pointed the camera at itself, so whichever player moved in last took over
        // everyone's camera.
        if (isOwned)
        {
            SceneViewControl.ActivateOnly(scene);
            BindUiToSceneCamera(scene);
            FollowWithLocalCamera(scene);
            ShowInGameHud(true);
        }

        OnPlayerMovedToGame?.Invoke(this);
    }

    /// <summary>
    /// Shows or hides the in game HUD: hp bar, xp bar and the upgrade cards all hang under the
    /// player's UI canvas. The player object already exists while its owner sits in the lobby,
    /// so the HUD has to be switched off there.
    ///
    /// The Canvas component is disabled rather than the GameObject, because UIManager sits on
    /// that same object and has to keep running to stay subscribed to the player's events.
    /// </summary>
    [Client]
    private void ShowInGameHud(bool visible)
    {
        UIManager hud = GetComponentInChildren<UIManager>(true);
        if (hud == null) return;

        Canvas canvas = hud.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.enabled = visible;
        }
    }

    /// <summary>
    /// Points the player's canvases at the camera of the scene it just entered.
    ///
    /// A Screen Space - Camera canvas renders nothing while its camera is disabled, and
    /// SceneViewControl disables the cameras of every scene the local player is not in. So the
    /// binding has to follow the player instead of being taken once from Camera.main.
    /// </summary>
    [Client]
    private void BindUiToSceneCamera(Scene scene)
    {
        Camera sceneCamera = HierarchyUtility.FindInScene<Camera>(scene);
        if (sceneCamera == null)
        {
            Debug.LogError($"No camera in {scene.name}: the player's UI cannot render there");
            return;
        }

        foreach (Canvas canvas in GetComponentsInChildren<Canvas>(true))
        {
            canvas.worldCamera = sceneCamera;
        }
    }

    [Client]
    private void FollowWithLocalCamera(Scene scene)
    {
        CameraController camera = HierarchyUtility.FindInScene<CameraController>(scene);
        if (camera == null)
        {
            Debug.LogError("No CameraController in the GameScene, the local player has nothing following it.");
            return;
        }

        camera.POI = transform;
        Debug.Log($"Camera now follows the local player {userName}");
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
