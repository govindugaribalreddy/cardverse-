using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// This sctipt is used in Configuration scene to process and validate the Room Settings
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] public TMP_InputField MaxPlayers;
    [SerializeField] public TMP_InputField DecksCount;
    [SerializeField] public TMP_InputField JokersCount;
    [SerializeField] public Toggle ClockwiseDirection;
    [SerializeField] public TMP_InputField CardsCount;

    private bool isHost = false;
    private string username = "";
    private string joincode = "";
    private GameSettings Settings = new GameSettings();

    void Start()
    {
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateRoom()
    {
        username = PlayerPrefs.GetString("Username");
        joincode = PlayerPrefs.GetString("JoinCode").ToUpper();
        isHost = PlayerPrefs.GetInt("IsHost") == 1;

        if (InputValidations())
        {
            Settings.MaxPlayers = int.Parse(MaxPlayers.text);
            Settings.DecksCount = int.Parse(DecksCount.text);
            Settings.JokersCount = int.Parse(JokersCount.text);
            Settings.CardRotations = ClockwiseDirection.isOn;
            Settings.CardsCount = int.Parse(CardsCount.text);

            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.SetString("JoinCode", joincode);
            PlayerPrefs.SetInt("IsHost", isHost ? 1 : 0);
            PlayerPrefs.SetString("Settings", JsonUtility.ToJson(Settings));
            PlayerPrefs.SetInt("DecksCount", Settings.DecksCount);
            SceneManager.LoadScene("RoomScene");
        }
    }

    public bool InputValidations()
    {
        if(string.IsNullOrWhiteSpace(MaxPlayers.text) || int.Parse(MaxPlayers.text) < 2)
        {
            SendAlert("Players Count", "Number of players should be more than 1");
            return false;
        }
        else if (int.Parse(MaxPlayers.text) > 10)
        {
            SendAlert("Players Count", "Number of players should be less than 10");
            return false;
        }
        else if (string.IsNullOrWhiteSpace(DecksCount.text) || int.Parse(DecksCount.text) == 0)
        {
            SendAlert("Decks Count", "Number of decks should be more than 0");
            return false;
        }
        else if (int.Parse(DecksCount.text) > 10)
        {
            SendAlert("Decks Count", "Number of decks should be less than 10");
            return false;
        }
        else if (string.IsNullOrWhiteSpace(JokersCount.text) || int.Parse(JokersCount.text) == 0)
        {
            SendAlert("Jokers Count", "Number of jokers should be more than 0");
            return false;
        }
        else if (string.IsNullOrWhiteSpace(CardsCount.text) || int.Parse(CardsCount.text) == 0)
        {
            SendAlert("Cards Count", "Number of cards given to each player should be more than 0");
            return false;
        }
        else if (int.Parse(CardsCount.text) > 8)
        {
            SendAlert("Cards Count", "Number of cards given to each player should be less than 9");
            return false;
        }
        return true;
    }

    public void SendAlert(string header, string body)
    {
        HandleMainSceneAlert Alert = GameObject.FindObjectOfType<HandleMainSceneAlert>();
        Alert.DisplayAlert(header, body);
    }
}
