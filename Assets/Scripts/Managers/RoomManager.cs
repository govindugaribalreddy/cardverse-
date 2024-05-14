using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This Script used to handle all the button click event in the Room Scene
/// based on the user leve - host/client - it call the respective manager (hostmanager/clientmanager)
/// </summary>
public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }

    public Button nextButton;
    public Button drawButton;

    private bool isHost = false;
    private string username = "";
    private string joincode = "";

    GameSettings Settings = new GameSettings();
    HostManager HostManager;
    ClientManager ClientManager;

    private bool isInitialized = false; // Flag to track initialization

    private async void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        await InitialSetup(); // Wait for initialization
    }

    private async Task InitialSetup()
    {
        nextButton.interactable = false;
        username = PlayerPrefs.GetString("Username");
        joincode = PlayerPrefs.GetString("JoinCode").ToUpper();
        isHost = PlayerPrefs.GetInt("IsHost") == 1;

        if (isHost)
        {
            HostManager = FindObjectOfType<HostManager>();
            if (HostManager == null)
            {
                Debug.LogError("HostManager not found in the scene.");
                return;
            }

            Settings = JsonUtility.FromJson<GameSettings>(PlayerPrefs.GetString("Settings"));
            HostManager.SetData(username, joincode, Settings);
            await HostManager.Authenticate();
        }
        else
        {
            ClientManager = FindObjectOfType<ClientManager>();
            if (ClientManager == null)
            {
                Debug.LogError("ClientManager not found in the scene.");
                return;
            }

            ClientManager.SetData(username, joincode);
            await ClientManager.Authenticate();
        }

        isInitialized = true; // Set initialization flag
    }

    public void NextClick()
    {
        if (!isInitialized) return; // Check if initialization is complete
        nextButton.interactable = false;

        if (isHost)
        {
            HostManager.HandleHostTurn();
        }
        else
        {
            ClientManager.HandleClientTurn();
        }
    }

    public void DrawCardClick()
    {
        if (!isInitialized) return; // Check if initialization is complete

        Debug.Log("Clicked on the Draw button");
        if (isHost)
        {
            HostManager.DrawCardForActivePlayer();
        }
        else
        {
            ClientManager.RequestDrawCard();
        }
    }

    public void ExitGame()
    {
        if (isHost)
        {
            HostManager.SendExitNotice(false);
        }
        else
        {
            ClientManager.SendExitNotice();
        }
    }

    public void SendAlert(string header, string body)
    {
        HandleMainSceneAlert Alert = FindObjectOfType<HandleMainSceneAlert>();
        Alert.DisplayAlert(header, body);
    }
}
