using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Used to close the alert popup
/// </summary>
public class ClosePopup : MonoBehaviour
{
    public GameObject Popup;
    public GameObject Menu;
    public void Close()
    {
        Popup.SetActive(false);
        Menu.SetActive(true);
        Button exitButton = Popup.transform.Find("ExitGame").GetComponent<Button>();
        if (exitButton != null)
        {
            exitButton.gameObject.SetActive(true);
        }
    }
}
