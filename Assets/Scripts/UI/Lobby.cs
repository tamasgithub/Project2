using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Lobby : MonoBehaviour
{
    public TextMeshProUGUI lobbyIdText;
    public Sprite readyIcon;
    public Sprite notReadyIcon;
    public Button startButton;

    SurvivorNetworkManager networkManager;
    public int _lobbyId = -1;
    public int LobbyId
    {
        get => _lobbyId; set
        {
            _lobbyId = value;
            lobbyIdText.SetText("Lobby " + _lobbyId.ToString("X"));
        }
    }
    // sorted by joined timestamp
    private Transform playersContainer;
    private SortedList<long, Player> playersInLobby;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        networkManager = FindAnyObjectByType<SurvivorNetworkManager>();
        playersInLobby = new();
        playersContainer = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "Players");
        Debug.Log("playersContainer " + playersContainer);
        Player.OnPlayerMovedToLobby += HandlePlayerJoinedLobby;
        Player.OnPlayerDisconnected += CleanupLobby;
        Player.OnPlayerDataChanged += OnPlayerDataChanged;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameObject.SetActive(scene.name == "LobbyScene");
    }

    private void OnDestroy()
    {
        Player.OnPlayerMovedToLobby -= HandlePlayerJoinedLobby;
        Player.OnPlayerDisconnected -= CleanupLobby;
        Player.OnPlayerDataChanged -= OnPlayerDataChanged;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void HandlePlayerJoinedLobby(Player newPlayer)
    {
        foreach (var p in playersInLobby.Values)
        {
            if (p == newPlayer)
            {
                // player already in lobby
                return;
            }
        }
        if (playersInLobby.Count >= networkManager.maxPlayersPerLobby)
        {
            Debug.LogError("Server should not move a player into an already full lobby!");
            return;
        }

        playersInLobby.Add(newPlayer.joinedLobbyTimestamp, newPlayer);
        if (NetworkClient.active)
        {
            UpdatePlayerUIs();
        }
    }

    private void CleanupLobby(Player playerLeft)
    {
        Debug.Log("Cleanup lobby after player " + playerLeft.userName + " left");
        // assumes the key is unique (time of joining in ms)
        long keyToRemove = -1;

        foreach (var kv in playersInLobby)
        {
            if (kv.Value == playerLeft)
            {
                keyToRemove = kv.Key;
                break;
            }
        }

        if (keyToRemove != -1)
        {
            playersInLobby.Remove(keyToRemove);
        }

        if (NetworkClient.active)
        {
            UpdatePlayerUIs();
        }
    }

    private void OnPlayerDataChanged(Player p)
    {
        // resort the list, because the lobby joined timestamp might have changed
        var entries = playersInLobby.Values.ToList();
        playersInLobby.Clear();
        foreach (Player player in entries)
        {
            playersInLobby.Add(player.joinedLobbyTimestamp, player);
        }


        if (NetworkClient.active)
        {
            UpdatePlayerUIs();
        }
    }

    [Client]
    private void UpdatePlayerUIs()
    {
        //Debug.Log("Update player UIs for " + playersInLobby.Count + " players");
        for (int i = 0; i < networkManager.maxPlayersPerLobby; i++)
        {
            Transform playerUI = playersContainer.GetChild(i).GetChild(0);
            Image[] icons = playerUI.GetComponentsInChildren<Image>(true);
            bool ready = false;
            if (i < playersInLobby.Count)
            {
                Player player = playersInLobby.ElementAt(i).Value;


                playerUI.GetComponent<TextMeshProUGUI>().SetText(player.userName);
                ready = player.isReady;

            }
            else
            {
                playerUI.GetComponent<TextMeshProUGUI>().SetText("");
            }

            foreach (Image icon in icons)
            {
                // (de)activate icons
                icon.gameObject.SetActive(i < playersInLobby.Count);
            }
            icons[icons.Length - 1].sprite = ready ? readyIcon : notReadyIcon;
        }
        if (playersInLobby.Count > 0)
        {
            bool localPlayerIsOwner = playersInLobby.ElementAt(0).Value.GetComponent<NetworkIdentity>() == NetworkClient.localPlayer;
            startButton.gameObject.SetActive(localPlayerIsOwner);
            startButton.interactable = playersInLobby.Values.All(p => p.isReady);
        }
    }

    [Client]
    public void CopyLobbyId()
    {
        GUIUtility.systemCopyBuffer = _lobbyId.ToString("X");
    }

    [Client]
    public void ToggleIsReady()
    {
        NetworkClient.localPlayer.GetComponent<Player>().CmdToggleIsReady();
    }

    [Client]
    public void RequestGameStart()
    {
        networkManager.RequestGameStart(LobbyId);
    }


}
