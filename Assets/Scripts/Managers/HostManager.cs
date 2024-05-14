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
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using NetworkEvent = Unity.Networking.Transport.NetworkEvent;


/// <summary>
/// This script is used to handle all the events of the host
/// Used in Room Scene
/// </summary>
public class HostManager : NetworkBehaviour
{
    public static HostManager Instance { get; private set; }

    public TextMeshProUGUI status;
    public TextMeshProUGUI joinCodeText;
    public TextMeshProUGUI roomName;
    public Button nextButton;
    public Button startButton;
    public Button exitButton;

    public GameObject cardPrefab; 
    public Transform cardsParent; 
    public Transform decksParent;
    public float horizontalStackSpacing = 30f;

    private string message = "";
    private string roomOwner = "";
    private string username = "";
    private string joincode = "";
    private int maxConnections = 5;
    private int clientID = 0;
    private bool pickedCard = false;
    private bool discardedCard = false;
    private List<Hand> PlayerHand = new List<Hand>();

    NetworkDriver hostDriver;
    NativeList<NetworkConnection> serverConnections;

    GameSettings Settings = new GameSettings();
    DataHelper Helper = new DataHelper();
    TurnManager TurnManager = new TurnManager();
    TransportHelper Transport = new TransportHelper();
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

    public async Task Authenticate()
    {
        PlayerManager = GameObject.FindObjectOfType<PlayerManager>();
        RoomManager = GameObject.FindObjectOfType<RoomManager>();
        DeckDisplayManager = GameObject.FindObjectOfType<DeckDisplayManager>();
        DeckDisplayManager.IsHost();
        DeckDisplayManager.SetClientID(this.clientID);

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

        await SetupHost();
    }

    public async Task SetupHost()
    {
        try
        {
            Settings = JsonUtility.FromJson<GameSettings>(PlayerPrefs.GetString("Settings"));
            maxConnections = Settings.MaxPlayers - 1;
            TurnManager.SetDirection(Settings.CardRotations);
            // Request a new allocation from the Relay service
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);

            // Get a join code for the newly allocated server that clients can use to join
            this.joincode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            joinCodeText.text = "Join Code: " + joincode;
            message = username + " have created the room";
            roomOwner = username + "'s Room";
            status.text = message;
            roomName.text = roomOwner;

            var relayServerData = new RelayServerData(allocation, "udp");

            // Create NetworkSettings using the Relay server data.
            var settings = new NetworkSettings();
            settings.WithRelayParameters(ref relayServerData);

            // Create the Host's NetworkDriver from the NetworkSettings.
            hostDriver = NetworkDriver.Create(settings);

            // Bind to the Relay server.
            if (hostDriver.Bind(NetworkEndPoint.AnyIpv4) != 0)
            {
                Debug.LogError("Host client failed to bind");
            }
            else
            {
                if (hostDriver.Listen() != 0)
                {
                    Debug.LogError("Host client failed to listen");
                }
                else
                {
                    Debug.Log("Host client bound to Relay server");
                }

            }

            serverConnections = new NativeList<NetworkConnection>(maxConnections, Allocator.Persistent);
            TurnManager.AddPlayer(username); //adding host as first player
            PlayerManager.ArrangePlayers(maxConnections + 1, clientID);
            PlayerManager.SetPlayerActive(clientID);
            PlayerManager.SetUsername(clientID, username);
        }
        catch (RelayServiceException e)
        {
            RoomManager.SendAlert("Error", "An unexpected error occurred, please try again later.\n" + e.Reason);
        }
    }

    public void DisplayDecksOnHost()
    {
        int deckCount = PlayerPrefs.GetInt("DecksCount", 1);

        if (DeckDisplayManager != null)
        {
            DeckDisplayManager.DisplayDecks(deckCount);
        }
        else
        {
            Debug.LogError("DeckDisplayManager not found!");
        }
    }


    public override void OnDestroy()
    {
        if (hostDriver.IsCreated)
        {
            hostDriver.Dispose();
        }

        Debug.Log("Disposing server connections");
        if (serverConnections.IsCreated)
        {
            Debug.Log("Server connections count: " + serverConnections.Length);
            serverConnections.Dispose();
        }
        else
        {
            Debug.Log("Server connections already disposed or never created");
        }
    }


    public void SetData(string username, string joincode, GameSettings settings)
    {
        this.username = username;
        this.joincode = joincode;
        this.Settings = settings;
    }
    #endregion

    #region Send Message
    public void OnHostSendMessage(string message, string type)
    {
        if (serverConnections.Length == 0)
        {
            Debug.LogError("No players connected to send messages to.");
            return;
        }
        try
        {
            Transport.OnHostSendMessage(hostDriver, serverConnections, message, type);
        }
        catch (Exception e)
        {
            RoomManager.SendAlert("Error", "An unexpected error occurred, please try again later.\n" + e.Message);
        }
    }

    #endregion


    #region Update
    void Update()
    {
        // Skip update logic if the Host is not yet bound.
        if (!hostDriver.IsCreated || !hostDriver.Bound)
        {
            return;
        }

        // This keeps the binding to the Relay server alive, preventing it from timing out due to inactivity.
        hostDriver.ScheduleUpdate().Complete();

        Transport.CleanHostConnections(serverConnections);

        // Accept incoming client connections.
        NetworkConnection incomingConnection;
        while ((incomingConnection = hostDriver.Accept()) != default(NetworkConnection))
        {
            // Adds the requesting Player to the serverConnections list.
            // This also sends a Connect event back the requesting Player,
            // as a means of acknowledging acceptance.
            Debug.Log("Accepted an incoming connection.");
            serverConnections.Add(incomingConnection);
        }

        // Process events from all connections.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            Assert.IsTrue(serverConnections[i].IsCreated);

            // Resolve event queue.
            NetworkEvent.Type eventType;
            while ((eventType = hostDriver.PopEventForConnection(serverConnections[i], out var stream)) != NetworkEvent.Type.Empty)
            {
                switch (eventType)
                {
                    // Handle Relay events.
                    case NetworkEvent.Type.Data:
                        HandleHostMessage(stream);
                        break;

                    // Handle Disconnect events.
                    case NetworkEvent.Type.Disconnect:
                        Debug.Log("Server received disconnect from client");
                        SendExitNotice(true);
                        serverConnections[i] = default(NetworkConnection);
                        break;
                }
            }
        }
    }
    #endregion
    
    void HandleHostMessage(DataStreamReader stream)
    {
        NetworkData receivedData = Helper.TransformDataToObject(stream);

        Debug.Log($"Server received msg: {receivedData.value}");

        if (receivedData.type == "draw-request")
        {
            string username = receivedData.value; // Username sent directly as the value
            if (TurnManager.IsTurn(username))
            {
                Card drawnCard = DeckDisplayManager.DrawCard(); // Simulated deck management to draw a card
                SendDrawnCardToClient(drawnCard, username);
            }
        }

        else if (receivedData.type == "discard-card")
        {
            string[] data = receivedData.value.Split(",");
            string discardedCardRank = data[0];
            string discardedCardSuit = data[1];
            DeckDisplayManager.RemoveCard(discardedCardRank, discardedCardSuit);
            string message = data[2] + " discarded card " + discardedCardRank + " of " + discardedCardSuit;
            OnHostSendMessage(message, "status");
            this.status.text = message;
            ShowDiscardedCards(discardedCardRank, discardedCardSuit);
            OnHostSendMessage(receivedData.value, "discard-card");
        }

        if (receivedData.type == "player-join")
        {
            string joinedUsername = receivedData.value;
            string stat = joinedUsername + " joined the room";
            status.text = stat;
            //syncing players
            TurnManager.AddPlayer(joinedUsername);
            Transport.OnHostSendMessageToClient(hostDriver, serverConnections, (TurnManager.playerOrder.Count - 1).ToString(), "clientID", (TurnManager.playerOrder.Count - 1));
            OnHostSendMessage(roomOwner, "room-info");
            OnHostSendMessage(stat, "status");
            OnHostSendMessage((maxConnections + 1).ToString(), "player-manager-arrange");

            //syncing players
            SyncPlayers();
            if (TurnManager.GetCount() == maxConnections + 1)
            {
                startButton.gameObject.SetActive(true);
            }
            Debug.Log("Added Client at position" + TurnManager.GetPlayerPosition(receivedData.value));
        }

        else if (receivedData.type == "status")
        {
            status.text = receivedData.value;
            OnHostSendMessage(receivedData.value, "status");

        }

        else if (receivedData.type == "made-move")
        {
            //finding which player made move
            int position = TurnManager.GetPlayerPosition(receivedData.value);

            //ending that player turn (making its hover background as white)
            string stat = receivedData.value + " made move";
            status.text = stat;
            OnHostSendMessage(stat, "status");
            PlayerManager.EndTurn(position);

            //Saying to end the player turn to all clients
            OnHostSendMessage(position.ToString(), "player-manager-current-end");

            //finding next player
            TurnManager.NextTurn();
            string currentUser = TurnManager.GetCurrentPlayer();
            position = TurnManager.GetPlayerPosition(currentUser);

            //Starting its turn (making its hover backgroudn as green)
            PlayerManager.StartTurn(position);

            //Saying to start this player turn to all clients
            OnHostSendMessage(position.ToString(), "player-manager-current-start");

            if (currentUser == username)
            {
                nextButton.interactable = true;
                pickedCard = false;
                discardedCard = false;
                Debug.Log("its your turn now");
            }
            else
            {
                OnHostSendMessage(currentUser, "change-turn");
            }
        }
    }

    private void PrintServerConnections()
    {
        Debug.Log("Current server connections:");
        for (int i = 0; i < serverConnections.Length; i++)
        {
            Debug.Log($"Connection {i}: {serverConnections[i]}, IsCreated: {serverConnections[i].IsCreated}");
        }
    }

    void SendDrawnCardToClient(Card drawnCard, string username)
    {
        int playerID = TurnManager.GetPlayerId(username); // Get the player ID
        Debug.Log($"Attempting to send card to playerID: {playerID}");

        // Adjust the playerID for 0-based index used in serverConnections
        int connectionIndex = playerID - 1;
        Debug.Log($"Converted playerID to connection index: {connectionIndex}");
        PrintServerConnections();

        // Check if the adjusted index is within the range of serverConnections
        if (connectionIndex >= 0 && connectionIndex < serverConnections.Length && serverConnections[connectionIndex].IsCreated)
        {
            PlayerCardData cardData = new PlayerCardData
            {
                PlayerID = playerID, // Keep the original playerID for consistency
                Username = username,
                Cards = new List<SimpleCard>
            {
                new SimpleCard
                {
                    Suit = drawnCard.CardSuit,
                    Rank = drawnCard.CardRank
                }
            }
            };
            string message = JsonUtility.ToJson(cardData);
            Debug.Log($"Serialized cardData: {message}");
            Debug.Log($"Sending card - Suit: {drawnCard.CardSuit}, Rank: {drawnCard.CardRank}");
            // Use the adjusted index to send the message
            Transport.SendSingleMessageToClient(hostDriver, serverConnections[connectionIndex], message, "draw-card");
        }
        else
        {
            Debug.LogError($"Invalid playerID {playerID} or connection index {connectionIndex} not established.");
        }
    }

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

    public void HandleHostTurn()
    {
        Debug.Log(cardsParent.childCount + " Not Equal to" + Settings.CardsCount);
        if (pickedCard && !discardedCard)
        {
            // Notify the user to discard a card
            Debug.Log("Please discard a card before ending your turn.");
            // Activate the next button so the player can try to end their turn again after discarding
            nextButton.interactable = true;
            // Add more logic here if needed to handle the discard action
        }
        else if (discardedCard && !pickedCard)
        {
            // Notify the user to discard a card
            Debug.Log("Please pick a card before ending your turn.");
            // Activate the next button so the player can try to end their turn again after discarding
            nextButton.interactable = true;
            // Add more logic here if needed to handle the discard action
        }
        else if(pickedCard && discardedCard || !pickedCard && !discardedCard)
        {
            // Rearrange the remaining cards in a horizontal layout
            RearrangeCards();
            PlayerManager.EndTurn(clientID);
            OnHostSendMessage(clientID.ToString(), "player-manager-current-end");
            
            //TurnManager.ResetCardDraw(username);
            TurnManager.NextTurn();
            string stat = username + " made move";
            status.text = stat;
            OnHostSendMessage(stat, "status");

            string currentUser = TurnManager.GetCurrentPlayer();
            int position = TurnManager.GetPlayerPosition(currentUser);
            PlayerManager.StartTurn(position);
            OnHostSendMessage(position.ToString(), "player-manager-current-start");
            OnHostSendMessage(currentUser, "change-turn");
        }
    }

    public void SyncPlayers()
    {
        string jsonString = "";
        foreach (KeyValuePair<string, int> Player in TurnManager.playerOrder)
        {
            string client = "{\"username\":\"" + Player.Key + "\",\"clientId\":" + Player.Value + "}";
            jsonString += client + ";";

            PlayerManager.SetUsername(Player.Value, Player.Key);
            PlayerManager.SetPlayerActive(Player.Value);
        }
        jsonString = jsonString.TrimEnd(';');
        OnHostSendMessage(jsonString, "player-manager-sync");
    }

    public void StartGame()
    {
        startButton.gameObject.SetActive(false);
        OnHostSendMessage("Host started the Game", "status");
        nextButton.interactable = true;
        PlayerManager.StartTurn(clientID);
        OnHostSendMessage(clientID.ToString(), "player-manager-current-start");
        DisplayDecksOnHost();
        // Assuming deckCount is the variable that holds the number of decks
        int deckCount = PlayerPrefs.GetInt("DecksCount", 1);
        // Convert deckCount to string since network messages are typically strings
        OnHostSendMessage(deckCount.ToString(), "start-game");
    }

    public void SendExitNotice(bool fromClient)
    {
        if (fromClient)
        {
            OnHostSendMessage("1", "exit");
            RoomManager.SendAlert("Game Over", "One Player have left the room\nGame Over!");
            exitButton.gameObject.SetActive(false);
        }
        else
        {
            if (serverConnections.Length > 0)
            {
                OnHostSendMessage("0", "exit");
            }
            ExitGame();
        }
    }

    public void ExitGame()
    {
        SceneManager.LoadScene("MainScene");
    }

    public bool HostDiscardValid()
    {
        return !discardedCard && TurnManager.IsTurn(this.username);
    }

    public void DealCardsToPlayers(List<Card> deck)
    {
        // Shuffle and deal cards as previously defined...
        Vector3 deckPosition = decksParent.position; // Position of the deckParent object
        Vector3 cardsPosition = cardsParent.position; // Position of the cardsParent object
        foreach (var player in TurnManager.playerOrder)
        {
            if (deck.Count >= Settings.CardsCount)
            {
                List<Card> cardsToDeal = deck.GetRange(0, Settings.CardsCount);
                DeckDisplayManager.RemoveCardFromShuffledDeck(cardsToDeal);

                // Log which cards are dealt to which player
                Debug.Log($"Dealing {string.Join(", ", cardsToDeal.Select(card => $"{card.CardRank} of {card.CardSuit}"))} to {player.Key} (Client ID: {player.Value})");

                // Serialize card data for transmission
                PlayerCardData cardData = new PlayerCardData
                {
                    PlayerID = player.Value,
                    Username = player.Key,
                    Cards = cardsToDeal.Select(card => new SimpleCard { Suit = card.CardSuit, Rank = card.CardRank }).ToList()
                };
                string message = JsonUtility.ToJson(cardData);

                // Send card data to each client
                Transport.OnHostSendMessageToClient(hostDriver, serverConnections, message, "deal-cards", player.Value);

                // Display cards for the host if the player is the host
                if (player.Key == username)
                {
                    Vector3 startPosition = deckPosition; // Start from the deckParent position
                    Vector3 endPosition = cardsPosition + new Vector3(0, 20 * clientID, 0); // Adjust Y offset based on host's ID

                    foreach (var card in cardsToDeal)
                    {
                        StartCoroutine(CreateCard(cardPrefab, new SimpleCard { Suit = card.CardSuit, Rank = card.CardRank }, startPosition, endPosition));
                        endPosition += new Vector3(100, 0, 0);
                    }

                    // Move the cards together after animation
                    StartCoroutine(MoveCardsTogether(cardsParent, horizontalStackSpacing));
                }
            }
        }
    }

    public IEnumerator MoveCardsTogether(Transform cardsParent, float spacing)
    {
        // Get the HorizontalLayoutGroup component
        HorizontalLayoutGroup layoutGroup = cardsParent.GetComponent<HorizontalLayoutGroup>();

        // Calculate the total width based on child count and spacing
        float totalWidth = layoutGroup.spacing * (cardsParent.childCount - 1);

        // Move cards together over time
        float duration = 1f; // Duration of the animation
        float elapsed = 0;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            // Calculate the current spacing based on interpolation
            float currentSpacing = Mathf.Lerp(spacing, -50, elapsed / duration);

            // Update the spacing in the HorizontalLayoutGroup
            layoutGroup.spacing = currentSpacing;

            yield return null;
            Debug.Log("card moved");
        }

        // Ensure spacing is set to the final value after animation
        layoutGroup.spacing = -50;
    }

    public void DrawCardForActivePlayer()
    {
        if (TurnManager.IsTurn(username)) // Make sure it's the current player's turn
        {
            int currentCardCount = cardsParent.childCount; // Get the current number of cards in hand
            if (!pickedCard)
            {
                Card drawnCard = DeckDisplayManager.DrawCard();
                if (drawnCard != null)
                { 
                    Vector3 deckPosition = decksParent.position; // Position of the deckParent object
                    Vector3 cardsPosition = cardsParent.position; // Position of the cardsParent object
                    Vector3 startPosition = deckPosition; // Start from the deckParent position
                    Vector3 endPosition = cardsPosition + new Vector3(0, 20 * clientID, 0); // Adjust Y offset based on host's ID+
                    // Display the card on the host's UI
                    StartCoroutine(DisplayDrawnCard(cardPrefab, new SimpleCard { Suit = drawnCard.CardSuit, Rank = drawnCard.CardRank}, startPosition, endPosition));
                    pickedCard = true;
                    //TurnManager.IncrementCardDraw(username); // Increment the draw count for the player
                }
            }
            else
            {
                Debug.Log("Cannot draw more cards this turn or card limit reached.");
            }
        }
    }

    public IEnumerator DisplayDrawnCard(GameObject cardPrefab, SimpleCard cardData, Vector3 start, Vector3 end)
    {
        Transform lastCard = cardsParent.GetChild(cardsParent.childCount - 1); // Get the last child transform

        Vector3 lastCardPosition = lastCard.position + new Vector3(50, 0, 0);

        GameObject cardGO = Instantiate(cardPrefab, start, Quaternion.identity, cardsParent);
        Hand handCard = new Hand();
        handCard.Card = cardGO;
        handCard.Rank = cardData.Rank;
        handCard.Suit = cardData.Suit;
        PlayerHand.Add(handCard); int spriteIndex = ((int)cardData.Suit * 13) + (int)cardData.Rank - 1;
        cardGO.GetComponent<Image>().sprite = DeckDisplayManager.cardSprites[spriteIndex];
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

    public void HostDiscardCard(int id)
    {
        discardedCard = true;
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
        DeckDisplayManager.RemoveCard(removedHand.Rank.ToString(), removedHand.Suit.ToString());
        string data = removedHand.Rank.ToString() + "," + removedHand.Suit.ToString();
        string message = this.username + " discarded card " + removedHand.Rank.ToString() + " of " + removedHand.Suit.ToString();
        OnHostSendMessage(message, "status");
        OnHostSendMessage(data, "discard-card");
    }


    [Serializable]
    public class PlayerCardData
    {
        public int PlayerID;
        public string Username;
        public List<SimpleCard> Cards;
    }


    [Serializable]
    public struct SimpleCard
    {
        public Card.Suit Suit;
        public Card.Rank Rank;
    }

    [Serializable]
    public class CardData
    {
        public List<SimpleCard> Cards;
    }

    public class Hand
    {
        public GameObject Card;
        public Card.Suit Suit;
        public Card.Rank Rank;
    }

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
        Debug.Log(spriteIndex + " here");

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
        return 0;
    }

    public IEnumerator CreateCard(GameObject cardPrefab, SimpleCard cardData, Vector3 start, Vector3 end)
    {
        GameObject cardGO = Instantiate(cardPrefab, start, Quaternion.identity, cardsParent);
        cardGO.transform.localScale = Vector3.one;  // Ensure the card scales correctly

        // Set the sprite based on card data
        if (DeckDisplayManager != null)
        {
            // Assuming cardData.Suit and cardData.Rank are indices to the cardSprites array
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
        // Animate the card movement from start to end positions
        float duration = 3.0f; // Duration of the animation
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cardGO.transform.position = Vector3.Lerp(start, end, elapsed / duration);
            yield return null;

        }
        // Move the cards together after animation
        StartCoroutine(MoveCardsTogether(cardsParent, horizontalStackSpacing));

        // Ensure the card is under the layout control after animation
        cardGO.transform.SetParent(cardsParent, false);
    }
}