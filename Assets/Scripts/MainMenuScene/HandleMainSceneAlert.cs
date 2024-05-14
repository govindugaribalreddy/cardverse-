using UnityEngine;
using TMPro;
using UnityEngine.UI;


/// <summary>
/// This script is used to display the Alert Popup in MainScene, ConfigurationScene and RoomScene
/// </summary>
public class HandleMainSceneAlert : MonoBehaviour
{
    private static HandleMainSceneAlert instance;
    public GameObject alertPopupPrefab; 
    public GameObject Menu;
    public TMP_Text headingText; 
    public TMP_Text bodyText; 

    void Start()
    {
        alertPopupPrefab.SetActive(false);
    }

    /// <summary>
    /// This method is used in Main Scene and Configurations Scene
    /// It is used during the input validations scenario.
    /// </summary>
    /// <param name="heading"></param>
    /// <param name="body"></param>
    public void DisplayAlert(string heading, string body)
    {
        // Set the heading and body text
        headingText.text = heading;
        bodyText.text = body;

        // Make the alert popup visible
        alertPopupPrefab.SetActive(true);
        Menu.SetActive(false);
    }

    /// <summary>
    /// This method is only used in Room scene.
    /// It is used to ask a confirmation from the user during the exit scenario
    /// </summary>
    public void ExitConfirmation()
    {
        headingText.text = "Exit Game";
        bodyText.text = "Are you sure you want to exit this Game Room?";

        // Make the alert popup visible
        alertPopupPrefab.SetActive(true);
        Image closeButton = alertPopupPrefab.transform.Find("Close Popup").GetComponent<Image>();
        closeButton.gameObject.SetActive(true);
        Button exitButton = alertPopupPrefab.transform.Find("ExitGame").GetComponent<Button>();
        exitButton.gameObject.SetActive(true);
    }

    /// <summary>
    /// This method is used in Room scene.
    /// It is used when user card drawing limit reaches - when users hand is full.
    /// </summary>
    public void HandFull()
    {
        headingText.text = "Full Hand";
        bodyText.text = "Your hand is full. \nDiscard a card from your hand to take a new one.";
        Image mainBtn = alertPopupPrefab.transform.Find("Nav To Main").GetComponent<Image>();
        Image exitBtn = alertPopupPrefab.transform.Find("ExitGame").GetComponent<Image>();

        mainBtn.gameObject.SetActive(false);
        exitBtn.gameObject.SetActive(false);

        // Make the alert popup visible
        alertPopupPrefab.SetActive(true);
        Image closeButton = alertPopupPrefab.transform.Find("Close Popup").GetComponent<Image>();
        closeButton.gameObject.SetActive(true);
    }
}
