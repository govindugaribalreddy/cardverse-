using System.Text.RegularExpressions;
using Unity.Services.Relay;
using UnityEngine;
using UnityEngine.SceneManagement;


/// <summary>
/// This script is used to navigate the user (host/client) to room scene
/// </summary>
public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    private string joinCode = "";
    private string userName = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
    }

    public void SetData(string username, string joincode)
    {
        this.userName = username;
        this.joinCode = joincode;
    }

    //Start the Relay Server, Generate the Join Code and Host the game
    public void StartHost()
    {
        try
        {
            NavigateToRoomScene(true);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
        catch (System.FormatException e) 
        {
            Debug.Log(e);
        }
    }

    public void StartClient()
    {
        try
        {
            // Some Authentication or checks here
            NavigateToRoomScene(false);
        }
        catch (RelayServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void NavigateToRoomScene(bool isHost)
    {
        if (userName == string.Empty)
        {
            SendAlert("Enter Username", "Please enter the username and try again.");
        }
        else if (userName.Length < 1 || userName.Length > 12)
        {
            SendAlert("Invalid Username", "Username should be between 1 and 12 characters long. Please try again.");
        }
        else if (!Regex.IsMatch(userName, @"^[a-zA-Z0-9_@#]+$"))
        {
            SendAlert("Invalid Username", "Please enter a valid username. Allowed characters are letters, numbers, '@', '_', and '#'.");
        }
        else
        {
            PlayerPrefs.SetString("Username", userName);
            PlayerPrefs.SetString("JoinCode", joinCode);
            PlayerPrefs.SetInt("IsHost", isHost ? 1 : 0);

            if (isHost)
            {
                SceneManager.LoadScene("ConfigurationScene");
            }
            else
            {
                if (joinCode == string.Empty)
                {
                    SendAlert("Enter Join Code", "Please enter the join code and try again.");
                }
                else
                {
                    SceneManager.LoadScene("RoomScene");
                }
            }
        }
    }


    public void SendAlert(string header, string body)
    {
        HandleMainSceneAlert Alert = GameObject.FindObjectOfType<HandleMainSceneAlert>();
        Alert.DisplayAlert(header, body);
    }
}
