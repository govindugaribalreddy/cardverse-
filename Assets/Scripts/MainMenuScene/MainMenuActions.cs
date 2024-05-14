using TMPro;
using UnityEngine;

/// <summary>
/// This script is used in Main Scene to Navigate to Configuration Scene or Room Scene
/// </summary>
public class MainMenuActions : MonoBehaviour
{

    public static RelayManager RelayManager { get; set; }
    [SerializeField] private TMP_InputField username;
    [SerializeField] private TMP_InputField joincode;

    private void Start()
    {
        RelayManager = new RelayManager();
    }

    /// <summary>
    /// This method is invoked when user pressed the Create Game button on Main Screen
    /// </summary>
    public void CreateRoom()
    {
        RelayManager.SetData(username.text, "");
        RelayManager.StartHost();
    }

    /// <summary>
    /// This method is invoked when user pressed the Join Game button on Main Screen
    /// </summary>
    public void JoinRoom()
    {
        RelayManager.SetData(username.text, joincode.text);
        RelayManager.StartClient();
    }
}
