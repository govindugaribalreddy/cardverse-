using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Player Manager script is used to show the user icons, blinking effects, blur effect and positioning of the icons
/// Used in Room Scene
/// </summary>
public class PlayerManager : MonoBehaviour
{

    public GameObject Dealer;
    public GameObject Player1;
    public GameObject Player2;
    public GameObject Player3;
    public GameObject Player4;
    public GameObject Player5;
    public GameObject Player6;
    public GameObject Player7;
    public GameObject Player8;
    public GameObject Player9;
    public GameObject Player10;
    public TMP_Text DialogText;
    public Image DialogImage;

    private List<GameObject> Players = new List<GameObject>();
    private List<string> Usernames = new List<string>();
    private Color startColor = new Color(0, 1, 0); // Green color
    private Color endColor = Color.white; // White color
    private float blinkDuration = 0.5f; // How long each blink phase lasts
    private Coroutine currentBlinkingCoroutine;
  
    void Start()
    {
        StartCoroutine(ShowDialogWithAnimation());
    }

    public bool IsEmpty()
    {
        if (Players.Count == 0)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private IEnumerator ShowDialogWithAnimation()
    {
        yield return new WaitForSeconds(3f); // Wait for 3 seconds

        // Enable the dialog box
        DialogText.text = "";
        Dealer.SetActive(true);
        // Get the text to display
        string message = "Welcome to the Game room!";
        StartCoroutine(ChangeDealorMessage(message));
        yield return new WaitForSeconds(4f);

        message = "Game will begin shortly...";
        StartCoroutine(ChangeDealorMessage(message));
    }

    private IEnumerator ChangeDealorMessage(string message)
    {
        DialogImage.enabled = true;
        StartCoroutine(AnimateMessage(message));
        yield return new WaitForSeconds(4f);
        DialogText.text = "";
        DialogImage.enabled = false;
    }

    private IEnumerator AnimateMessage(string message)
    {
        // Show each letter with animation
        for (int i = 0; i < message.Length; i++)
        {
            // Get the current text
            string partialMessage = message.Substring(0, i + 1);

            // Update the text component
            DialogText.text = partialMessage;

            // Wait for a short duration before showing the next letter
            yield return new WaitForSeconds(0.1f); // Adjust the duration as needed
        }
    }

    public void ArrangePlayers(int max, int clientID)
    {
        switch (max)
        {
            case 2:
                Players.Add(Player5);
                Players.Add(Player6);
                break;

            case 3:
                Players.Add(Player3);
                Players.Add(Player5);
                Players.Add(Player6);
                break;

            case 4:
                Players.Add(Player3);
                Players.Add(Player5);
                Players.Add(Player6);
                Players.Add(Player8);
                break;

            case 5:
                Players.Add(Player1);
                Players.Add(Player3);
                Players.Add(Player5);
                Players.Add(Player6);
                Players.Add(Player8);
                break;

            case 6:
                Players.Add(Player1);
                Players.Add(Player3);
                Players.Add(Player5);
                Players.Add(Player6);
                Players.Add(Player8);
                Players.Add(Player10);
                break;

            case 7:
                Players.Add(Player1);
                Players.Add(Player3);
                Players.Add(Player4);
                Players.Add(Player5);
                Players.Add(Player7);
                Players.Add(Player8);
                Players.Add(Player10);
                break;

            case 8:
                Players.Add(Player1);
                Players.Add(Player3);
                Players.Add(Player4);
                Players.Add(Player5);
                Players.Add(Player6);
                Players.Add(Player7);
                Players.Add(Player8);
                Players.Add(Player10);
                break;

            case 9:
                Players.Add(Player1);
                Players.Add(Player2);
                Players.Add(Player3);
                Players.Add(Player4);
                Players.Add(Player5);
                Players.Add(Player6);
                Players.Add(Player7);
                Players.Add(Player8);
                Players.Add(Player10);
                break;

            case 10:
                Players.Add(Player1);
                Players.Add(Player2);
                Players.Add(Player3);
                Players.Add(Player4);
                Players.Add(Player5);
                Players.Add(Player6);
                Players.Add(Player7);
                Players.Add(Player8);
                Players.Add(Player9);
                Players.Add(Player10);
                break;
        }
        SetOpacity(clientID);
    }

    public void SetPlayerActive(int index)
    {
        Players[index].SetActive(true);
    }

    public void SetPlayersActiveTillIndex(int max)
    {
        for(int i = 0; i < max; i++)
        {
            TMP_Text usernameText = Players[i].transform.Find("username").GetComponent<TMP_Text>();
            usernameText.text = Usernames[i];
            Players[i].SetActive(true);
        }
    }

    private IEnumerator BlinkingEffect(int index)
    {
        Image bgImage = Players[index].transform.Find("bg").GetComponent<Image>();
        bgImage.color = startColor;
        while (true)
        {
            bgImage.enabled = !bgImage.enabled; // Toggle the visibility
            yield return new WaitForSeconds(blinkDuration); // Wait for blink duration
        }
    }

    private void ResetPlayerBackgrounds()
    {
        foreach (var player in Players)
        {
            Image bgImage = player.transform.Find("bg").GetComponent<Image>();
            bgImage.enabled = true; // You can also set to a default color if needed
        }
    }

    public void SetOpacity(int index)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            Image avatar = Players[i].transform.Find("avatar").GetComponent<Image>();
            if (i == index)
            {
                Players[i].transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
            }
            else
            {
                Color avatarColor = avatar.color;
                avatarColor.a = 0.5f;
                avatar.color = avatarColor;
            }
        }
    }

    public void StartTurn(int index)
    {
        if (currentBlinkingCoroutine != null)
        {
            StopCoroutine(currentBlinkingCoroutine);
            ResetPlayerBackgrounds(); // Reset all backgrounds before starting a new turn
        }
        currentBlinkingCoroutine = StartCoroutine(BlinkingEffect(index));
    }

    public void EndTurn(int index)
    {
        if (currentBlinkingCoroutine != null)
        {
            StopCoroutine(currentBlinkingCoroutine);
            currentBlinkingCoroutine = null;
        }

        Image bgImage = Players[index].transform.Find("bg").GetComponent<Image>();
        bgImage.color = endColor;
        bgImage.enabled = true; // Ensure the image is visible when the turn ends
    }

    public void SetUsername(int index, string name)
    {
        TMP_Text usernameText = Players[index].transform.Find("username").GetComponent<TMP_Text>();
        Debug.Log("User: " + name + " Index: " + index);
        Usernames.Add(name);
        // Set the username text
        usernameText.text = name;
    }

    public string GetUsername(int index)
    {
        return Usernames[index];
    }


    void Update()
    {
        
    }
}
