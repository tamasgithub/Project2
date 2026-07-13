using Edgegap;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{

    // sadly not possible to pass 2 GameObjects into a function via the onClick interface in the Button inspector
    public TextMeshProUGUI lobbyIdInput;

    // meake extra sure the NetworkManager is found when needed because the main menu is the offline scene that is
    // recreated along with the NetworkManager singleton when the connection to the server is lost
    private SurvivorNetworkManager _networkManager;
    private SurvivorNetworkManager NetworkManager
    {
        get
        {
            if (_networkManager == null)
            {
                _networkManager = FindAnyObjectByType<SurvivorNetworkManager>();
            }
            return _networkManager;
        }
    }

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameObject.SetActive(scene.name == "MainMenuScene");
    }

    public void OpenDialog(GameObject dialogGO)
    {
        dialogGO.SetActive(true);
    }

    public void CloseLobbyDialog(GameObject dialogGO)
    {
        dialogGO.SetActive(false);
    }

    public void RequestLobbyCreation(TextMeshProUGUI nameInput)
    {
        NetworkManager.RequestLobbyCreation(nameInput.text.Replace("\u200B", ""));
    }

    public void RequestLobbyJoining(TextMeshProUGUI nameInput)
    {
        if (int.TryParse(lobbyIdInput.text.Replace("\u200B", ""), System.Globalization.NumberStyles.HexNumber, null, out int value))
        {
            NetworkManager.RequestLobbyJoining(nameInput.text.Replace("\u200B", ""), value);
        }
        else
        {
            Debug.LogWarning($"LobbyId input {lobbyIdInput.text} cannot be parsed as a hex string to int. Expected values are something like 048ABCDE (8 digits between 0 and F)");
        }
    }

    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.ExitPlaymode();
#else
    Application.Quit();
#endif
    }
}
