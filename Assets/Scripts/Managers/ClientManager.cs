using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;

/// <summary>
/// This Script that handle all the events and effects of Client
/// Used in Room Scene
/// </summary>
public class ClientManager : NetworkBehaviour
{
    public static ClientManager Instance { get; private set; }

    /// <summary>
    /// Public variables the components are assigned to these variables from the Room Scene
    /// </summary>
    public TextMeshProUGUI status;
    public TextMeshProUGUI joinCodeText;
    public TextMeshProUGUI roomName;
    public Button nextButton;
    public Button exitButton;

    public GameObject cardPrefab; 
    public Transform cardsParent; 
    public Transform decksParent;

    private string message = "";
    private string roomOwner = "";
    private string username = "";
    private string joincode = "";
    private int clientID = 0;
    private bool pickedCard = false;
    private bool discardCard = false;
    private bool isTurn = false;
    private List<Hand> PlayerHand = new List<Hand>();

    NetworkDriver playerDriver;
    NetworkConnection clientConnection;

    DataHelper Helper = new DataHelper();
    PlayerManager PlayerManager;
    RoomManager RoomManager;
    DeckDisplayManager DeckDisplayManager;

    #region Initialize Relay
    public void Start()
    {
        InitialSetup();
    }

    public void InitialSetup()
    {
        nextButton.interactable = false;
        status.text = "";
        joinCodeText.text = "";
        roomName.text = "";
    }

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
        InitialSetup();
    }

    /// <summary>
    /// This method Initialize Client to Relay Server
    /// </summary>
    /// <returns></returns>
    public async Task Authenticate()
    {
        PlayerManager = GameObject.FindObjectOfType<PlayerManager>();
        RoomManager = GameObject.FindObjectOfType<RoomManager>();
        DeckDisplayManager = GameObject.FindObjectOfType<DeckDisplayManager>();

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed In " + AuthenticationService.Instance.PlayerId);
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        else
        {
            Debug.Log("Already signed in as: " + AuthenticationService.Instance.PlayerId);
        }

        await SetupClient();
    }

    /// <summary>
    /// This method add clients to the Game room
    /// </summary>
    /// <returns></returns>
    public async Task SetupClient()
    {
        try
        {
            JoinAllocation playerAllocation = await RelayService.Instance.JoinAllocationAsync(joincode);
            if (playerAllocation == null)
            {
                // Trigger alert for incorrect join code
                RoomManager.SendAlert("Invalid Join Code", "Please check the join code and try again.");
                return;
            }

            var relayServerData = new RelayServerData(playerAllocation, "udp");
            var settings = new NetworkSettings();
            settings.WithRelayParameters(ref relayServerData);

            // Create the Player's NetworkDriver from the NetworkSettings object.
            playerDriver = NetworkDriver.Create(settings);

            // Bind to the Relay server.
            if (playerDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Player client failed to bind");

            }
            else
            {
                Debug.Log("Player client bound to Relay server");
            }

            clientConnection = playerDriver.Connect();
            roomName.text = roomOwner;
            message = "Status: " + username + " have joined the room";
            status.text = message;
            joinCodeText.text = "Join Code: " + joincode;
        }
        catch (RelayServiceException e)
        {
            if (e.Reason == RelayExceptionReason.InvalidRequest || e.Reason == RelayExceptionReason.JoinCodeNotFound)
            {

                RoomManager.SendAlert("Invalid Join Code", "Please check the Join Code and try again");
            }
            else if (e.Reason == RelayExceptionReason.AllocationNotFound)
            {
                RoomManager.SendAlert("Max Players Joined", "Room is Full!");
            }
            else
            {
                RoomManager.SendAlert("Error", "An unexpected error occurred, please try again later.\n" + e.Reason);
            }
        }
    }

    public override void OnDestroy()
    {
        playerDriver.Dispose();
    }

    #endregion

    /// <summary>
    /// This method Send the Client message to the Host
    /// </summary>
    /// <param name="message">The date to be sent</param>
    /// <param name="type">The type of message</param>
    #region Send Message
    public void OnPlayerSendMessage(string message, string type)
    {
        if (!clientConnection.IsCreated)
        {
            Debug.LogError("Player is not connected. No Host client to send message to.");
            return;
        }

        NetworkData data = new NetworkData
        {
            type = type,
            value = message
        };

        byte[] bytes = Helper.TransformDataToBytes(data);

        if (playerDriver.BeginSend(clientConnection, out var writer) == 0)
        {
            NativeArray<byte> nativeBytes = new NativeArray<byte>(bytes.Length, Allocator.Temp);
            nativeBytes.CopyFrom(bytes);

            // Write the NativeArray<byte> to the writer
            writer.WriteBytes(nativeBytes);

            // Release the NativeArray<byte>
            nativeBytes.Dispose();

            playerDriver.EndSend(writer);
        }
        Debug.Log("done");

    }
    #endregion

    #region Update
    void Update()
    {
        // Skip update logic if the Player is not yet bound.
        if (!playerDriver.IsCreated || !playerDriver.Bound)
        {
            return;
        }

        // This keeps the binding to the Relay server alive,
        // preventing it from timing out due to inactivity.
        playerDriver.ScheduleUpdate().Complete();

        // Resolve event queue.
        NetworkEvent.Type eventType;
        while ((eventType = clientConnection.PopEvent(playerDriver, out var stream)) != NetworkEvent.Type.Empty)
        {
            switch (eventType)
            {
                // Handle Relay events.
                case NetworkEvent.Type.Data:
                    HandleClientMessage(stream);
                    break;

                // Handle Connect events.
                case NetworkEvent.Type.Connect:
                    Debug.Log("Player connected to the Host");
                    OnPlayerSendMessage(username, "player-join");
                    break;

                // Handle Disconnect events.
                case NetworkEvent.Type.Disconnect:
                    Debug.Log("Player got disconnected from the Host");
                    RoomManager.SendAlert("Disconnected", "You got disconnected from the server");
                    clientConnection = default(NetworkConnection);
                    break;
            }
        }
    }
    #endregion

    public void SetData(string username, string joincode)
    {
        this.username = username;
        this.joincode = joincode;
    }

    /// <summary>
    /// The method handle the data sent to the client from the Host
    /// </summary>
    /// <param name="stream"></param>
    void HandleClientMessage(DataStreamReader stream)
    {
        NetworkData receivedData = Helper.TransformDataToObject(stream);

        //Got new card info from Host
        if (receivedData.type == "draw-card")
        {
            PlayerCardData cardData = JsonUtility.FromJson<PlayerCardData>(receivedData.value);
            int cardCountInHand = cardsParent.childCount + 1;
            foreach (var card in cardData.Cards)
            {
                Debug.Log("cards in hand for" + username + ": " + cardsParent.childCount);
                Debug.Log("should be less then <" + cardCountInHand);
                if (cardsParent.childCount < cardCountInHand)
                {
                    Debug.Log($"Received card for client: {card.Suit} of {card.Rank}");
                    Vector3 deckPosition = decksParent.position; // Position of the deckParent object
                    Vector3 cardsPosition = cardsParent.position; // Position of the cardsParent object
                    Vector3 startPosition = deckPosition; // Start from the deckParent position
                    Vector3 endPosition = cardsPosition + new Vector3(0, 20 * clientID, 0); // Adjust Y offset based on host's ID
                    // Display the card on the host's UI
                    StartCoroutine(DisplayDrawnCard(cardPrefab, cardsParent, card, startPosition, endPosition));
                    pickedCard = true;
                }
                else
                {
                    Debug.Log("Card limit reached, cannot draw more cards.");
                }
            }
        }

        // when the game starts and host distribute cards to all players
        if (receivedData.type == "deal-cards")
        {
            Debug.Log($"Received cards: {receivedData.value}");
            PlayerCardData cardData = JsonUtility.FromJson<PlayerCardData>(receivedData.value);
            Debug.Log($"Received cards: {cardData}");

            Debug.Log($"Instantiating cards for client {clientID}");
            Vector3 deckPosition = decksParent.position; // Position of the deckParent object
            Vector3 cardsPosition = cardsParent.position; // Position of the cardsParent object
            Vector3 startPosition = deckPosition; // Start from the deckParent position
            Vector3 endPosition = cardsPosition + new Vector3(0, 20 * clientID, 0);
            foreach (var card in cardData.Cards)
            {
                // Directly instantiate cards at their final positions
                StartCoroutine(CreateCard(cardPrefab, cardsParent, card, startPosition, endPosition));
                endPosition += new Vector3(100, 0, 0);
            }
        }

        // when host start the games
        if (receivedData.type == "start-game")
        {
            int deckCount = int.Parse(receivedData.value); // Make sure you send the deckCount as a string
            InitializeDecksOnClient(deckCount);
        }

        // to show the deck images on the table
        else if (receivedData.type == "update-deck-count")
        {
            Debug.Log("Received deck count data: " + receivedData.value);
            if (int.TryParse(receivedData.value, out int deckCount))
            {
                Debug.Log("Parsed deck count: " + deckCount);
                if (DeckDisplayManager != null)
                {
                    DeckDisplayManager.DisplayDecks(deckCount);
                    Debug.Log("Decks displayed successfully");
                }
                else
                {
                    Debug.LogError("DeckDisplayManager not found on scene.");
                }
            }
            else
            {
                Debug.LogError("Failed to parse deck count from received data: " + receivedData.value);
            }
        }

        // when other players discard the card, host send this info to all players
        else if (receivedData.type == "discard-card")
        {
            string[] data = receivedData.value.Split(",");
            string discardedCardRank = data[0];
            string discardedCardSuit = data[1];
            ShowDiscardedCards(discardedCardRank, discardedCardSuit);
        }

        // when a new player joins the room
        if (receivedData.type == "room-info")
        {
            Debug.Log($"Player received msg: {receivedData.value}");
            roomOwner = receivedData.value;
            roomName.text = roomOwner;
        }

        //for any change in the status message - sent from the host
        else if (receivedData.type == "status")
        {
            Debug.Log($"Player received msg: {receivedData.value}");
            message = receivedData.value;
            status.text = message;
        }

        // when any player complete its turn 
        else if (receivedData.type == "change-turn")
        {
            Debug.Log("Current player is : " + receivedData.value);
            if (receivedData.value == username)
            {
                Debug.Log("its your turn now");
                nextButton.interactable = true;
                isTurn = true;
                pickedCard = false;
                discardCard = false;
            }
        }


        else if (receivedData.type == "clientID")
        {
            Debug.Log($"Received clientID: {clientID}");
            Debug.Log($"Player received msg: {receivedData.value}");
            this.clientID = int.Parse(receivedData.value);
            DeckDisplayManager.SetClientID(this.clientID);
        }

        // to sync the other players icons
        else if (receivedData.type == "player-manager-sync")
        {
            Debug.Log("Syncing Players\n" + receivedData.value);

            string[] playerInfoArray = receivedData.value.Split(';');
            Debug.Log("Array of Players count: " + playerInfoArray);
            // Convert the array to a list
            foreach (string clientString in playerInfoArray)
            {
                ClientJoinData client = JsonUtility.FromJson<ClientJoinData>(clientString);
                PlayerManager.SetUsername(int.Parse(client.clientId), client.username);
                PlayerManager.SetPlayerActive(int.Parse(client.clientId));
            }
        }

        // based on number of players arrange the icons
        else if (receivedData.type == "player-manager-arrange")
        {
            if (PlayerManager.IsEmpty())
            {
                PlayerManager.ArrangePlayers(int.Parse(receivedData.value), clientID);
            }
        }

        // start the blinking effect of current player
        else if (receivedData.type == "player-manager-current-start")
        {
            pickedCard = false;
            discardCard = false;
            PlayerManager.StartTurn(int.Parse(receivedData.value));
        }

        // end the blinking effect of current player after its turn
        else if (receivedData.type == "player-manager-current-end")
        {
            PlayerManager.EndTurn(int.Parse(receivedData.value));
        }

        // when host or other player exits the game
        else if (receivedData.type == "exit")
        {
            exitButton.gameObject.SetActive(false);
            if (int.Parse(receivedData.value) == 1)
            {
                RoomManager.SendAlert("Game Over", "One Player have left the room\nGame Over!");
            }
            else
            {
                RoomManager.SendAlert("Game Over", "Host have left the room\nGame Over!");
            }
        }
    }


    public void InitializeDecksOnClient(int deckCount)
    {
        if (DeckDisplayManager != null)
        {
            DeckDisplayManager.DisplayDecks(deckCount);
        }
    }

    /// <summary>
    /// Animate the distribution of cards
    /// </summary>
    /// <param name="cardsParent"></param>
    /// <param name="spacing"></param>
    /// <returns></returns>
    public IEnumerator MoveCardsTogether(Transform cardsParent, float spacing)
    {
        // Get the HorizontalLayoutGroup component
        HorizontalLayoutGroup layoutGroup = cardsParent.GetComponent<HorizontalLayoutGroup>();

        // Calculate the total width based on child count and spacing
        float totalWidth = layoutGroup.spacing * (cardsParent.childCount - 1);

        // Move cards together over time
        float duration = 1.0f; // Duration of the animation
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Calculate the current spacing based on interpolation
            float currentSpacing = Mathf.Lerp(spacing, -50, elapsed / duration);

            // Update the spacing in the HorizontalLayoutGroup
            layoutGroup.spacing = currentSpacing;

            yield return null;
        }

        // Ensure spacing is set to the final value after animation
        layoutGroup.spacing = -50;
    }


    /// <summary>
    /// When one of the player exits the game
    /// </summary>
    public void SendExitNotice()
    {
        SceneManager.LoadScene("MainScene");
        playerDriver.Disconnect(clientConnection);
    }


    /// <summary>
    /// Arrangement of cards after sorting or picking or discard
    /// </summary>
    public void RearrangeCards()
    {
        int childCount = cardsParent.childCount;
        Vector2 offset = new Vector2(50f, 0f); // Adjust the offset as needed

        for (int i = 0; i < childCount; i++)
        {
            Transform cardTransform = cardsParent.GetChild(i);
            Vector2 newPosition = new Vector2(i * offset.x, 0f);
            cardTransform.localPosition = newPosition;
        }
    }


    /// <summary>
    /// When Player clicks on Pass Turn Button
    /// </summary>
    public void HandleClientTurn()
    {
        int curentClientCardCount = 3;
        Debug.Log(cardsParent.childCount + " Not Equal to" + curentClientCardCount);
        if (pickedCard && !discardCard)
        {
            // Notify the user to discard a card
            Debug.Log("Please discard a card before ending your turn.");
            // Activate the next button so the player can try to end their turn again after discarding
            nextButton.interactable = true;
            // Add more logic here if needed to handle the discard action
        }
        else if (!pickedCard && discardCard)
        {
            // Notify the user to discard a card
            Debug.Log("Please discard a card before ending your turn.");
            // Activate the next button so the player can try to end their turn again after discarding
            nextButton.interactable = true;
            // Add more logic here if needed to handle the discard action
        }
        else if(pickedCard && discardCard || !pickedCard && !discardCard)
        {
            isTurn = false;
            RearrangeCards();
            status.text = username + " made move";
            OnPlayerSendMessage(username, "made-move");
        }
    }


    /// <summary>
    /// When Client Clicks on Draw button - host sends the new card info and this method is invoked
    /// </summary>
    /// <param name="cardPrefab"></param>
    /// <param name="cardsParent"></param>
    /// <param name="cardData"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public IEnumerator DisplayDrawnCard(GameObject cardPrefab, Transform cardsParent, SimpleCard cardData, Vector3 start, Vector3 end)
    {
        Transform lastCard = cardsParent.GetChild(cardsParent.childCount - 1); // Get the last child transform
        Vector3 lastCardPosition = lastCard.position + new Vector3(50, 0, 0);
        GameObject cardGO = Instantiate(cardPrefab, start, Quaternion.identity, cardsParent);
        int spriteIndex = ((int)cardData.Suit * 13) + (int)cardData.Rank - 1;
        cardGO.GetComponent<Image>().sprite = DeckDisplayManager.cardSprites[spriteIndex];

        Hand handCard = new Hand();
        handCard.Card = cardGO;
        handCard.Rank = cardData.Rank;
        handCard.Suit = cardData.Suit;
        PlayerHand.Add(handCard);

        // Animate the card movement from start to end positions
        float duration = 3.0f; // Duration of the animation
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardGO.transform.position = Vector3.Lerp(start, lastCardPosition, elapsed / duration);
            yield return null;

        }
    }


    /// <summary>
    /// When user clicks of Draw button
    /// </summary>
    public void RequestDrawCard()
    {
        int currentCardCount = cardsParent.childCount; // Count current cards in hand
        Debug.Log("Current card count for " + username + ": " + currentCardCount); // Log the current card count for the username
        Debug.Log("Current Settings.CardsCount + one for" + username + ": " + currentCardCount + 1);
        Debug.Log(username);
        if (!pickedCard)
        {
            OnPlayerSendMessage(username, "draw-request");
        }
        else
        {
            Debug.Log("Not allowed to draw a card either due to turn rules or card count limits.");
        }
    }


    /// <summary>
    /// When Other players discard a card, this method displays the discarded card
    /// </summary>
    public GameObject discardedCardsContainer;
    public void ShowDiscardedCards(string rank, string suit)
    {
        int spriteIndex = 0;
        if (suit == "Clubs")
        {
            spriteIndex = GetClubRank(rank);
        }
        else
        {
            spriteIndex = ((int)Enum.Parse(typeof(Card.Suit), suit) * 13) + (int)Enum.Parse(typeof(Card.Rank), rank) - 1;
        }

        GameObject cardGO = Instantiate(cardPrefab, Vector3.zero, Quaternion.identity, discardedCardsContainer.transform);
        cardGO.transform.localScale = Vector3.one;  // Ensure the card scales correctly
        cardGO.GetComponent<Image>().sprite = DeckDisplayManager.cardSprites[spriteIndex];
        Vector3 sumVector = new Vector3(0f, 0f, 0f);

        foreach (Transform child in discardedCardsContainer.transform)
        {
            sumVector += child.position;
        }

        Vector3 groupCenter = sumVector / discardedCardsContainer.transform.childCount;
        cardGO.transform.position = groupCenter;

    }

    public int GetClubRank(string rank)
    {
        switch (rank)
        {
            case "Ace": return 0;
            case "Two": return 1;
            case "Three": return 2;
            case "Four": return 3;
            case "Five": return 4;
            case "Six": return 5;
            case "Seven": return 6;
            case "Eight": return 7;
            case "Nine": return 8;
            case "Ten": return 9;
            case "Jack": return 10;
            case "Queen": return 11;
            case "king": return 12;
        }
        return 12;
    }


    public float horizontalStackSpacing = 30f;
    /// <summary>
    /// This method create a card gameobject and display it on the board
    /// </summary>
    /// <param name="cardPrefab">Card with sprite</param>
    /// <param name="cardsParent">The parent game object</param>
    /// <param name="cardData">Card info</param>
    /// <param name="start">start position</param>
    /// <param name="end">end position</param>
    /// <returns></returns>
    IEnumerator CreateCard(GameObject cardPrefab, Transform cardsParent, SimpleCard cardData, Vector3 start, Vector3 end)
    {
        GameObject cardGO = Instantiate(cardPrefab, start, Quaternion.identity, cardsParent);
        cardGO.transform.localScale = Vector3.one;
        if (DeckDisplayManager != null)
        {
            int spriteIndex = ((int)cardData.Suit * 13) + (int)cardData.Rank - 1;
            if (spriteIndex >= 0 && spriteIndex < DeckDisplayManager.cardSprites.Length)
            {
                cardGO.GetComponent<Image>().sprite = DeckDisplayManager.cardSprites[spriteIndex];
                Hand handCard = new Hand();
                handCard.Card = cardGO;
                handCard.Rank = cardData.Rank;
                handCard.Suit = cardData.Suit;
                PlayerHand.Add(handCard);
            }
            else
            {
                Debug.LogError("Invalid sprite index.");
            }
        }
        else
        {
            Debug.LogError("DeckDisplayManager not found in the scene.");
        }
        float duration = 3.0f;
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardGO.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;
        }

        StartCoroutine(MoveCardsTogether(cardsParent, horizontalStackSpacing));
        cardGO.transform.SetParent(cardsParent, false);
    }


    /// <summary>
    /// When the current player discard a card this method send that info to host
    /// </summary>
    /// <param name="id"></param>
    public void ClientDiscardCard(int id)
    {
        discardCard = true;
        Hand removedHand = new Hand();
        foreach (Hand hand in PlayerHand)
        {
            if (hand.Card.gameObject.GetInstanceID() == id)
            {
                Debug.Log("Dicarded card rank: " + hand.Rank);
                Debug.Log("Dicarded card suit: " + hand.Suit);
                removedHand = hand;
                break;
            }
        }
        PlayerHand.Remove(removedHand);
        string data = removedHand.Rank.ToString() + "," + removedHand.Suit.ToString() + "," + this.username;
        OnPlayerSendMessage(data, "discard-card");
    }


    /// <summary>
    /// To check if player can discard a card
    /// </summary>
    /// <returns></returns>
    public bool ClientDiscardValid()
    {
        return !discardCard && isTurn;
    }


    /// <summary>
    /// Used to serialize the card info from host
    /// </summary>
    [Serializable]
    public struct SimpleCard
    {
        public Card.Suit Suit;
        public Card.Rank Rank;
    }

    /// <summary>
    /// Used to serialize the initial player hand that host distributed
    /// </summary>
    [Serializable]
    public class PlayerCardData
    {
        public int PlayerID;
        public string Username;
        public List<SimpleCard> Cards;
    }

    /// <summary>
    /// Used to Store the player cards in Hand
    /// </summary>
    public class Hand
    {
        public GameObject Card;
        public Card.Suit Suit;
        public Card.Rank Rank;
    }
}


