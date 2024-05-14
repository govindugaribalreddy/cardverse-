using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This scirpt handles all the events related to the Deck
/// Used in Room Scene
/// </summary>
public class DeckDisplayManager : MonoBehaviour
{
    public GameObject deckPrefab; // Prefab with an Image for the back of a deck
    public Transform deckParent; // Parent transform where decks will be instantiated
    public Sprite[] cardSprites; // Array of 52 card sprites, assigned in the inspector

    int clientID = 0;

    private List<List<Card>> decks = new List<List<Card>>();
    List<Card> combinedDeck = new List<Card>();
    List<Card> originalDeck = new List<Card>();
    List<Card> discardedDeck = new List<Card>();
    private bool isHost = false;


    void Start()
    {

    }

    private void Update()
    {
        
    }

    public void SetClientID(int id)
    {
        clientID = id;
    }

    public void IsHost()
    {
        this.isHost = true;
    }

    // Method to draw a card from any of the merged decks
    public Card DrawCard()
    {
        foreach (List<Card> deck in decks)
        {
            if (deck.Count > 0)
            {
                Card card = deck[0];
                deck.RemoveAt(0);
                Debug.Log($"Card drawn: {card}");
                return card;
            }
        }
        // If all decks are empty
        Debug.LogWarning("Attempted to draw a card, but all decks are empty!");
        return null;
    }

    // Helper method to get the sprite of a card
    public Sprite GetCardSprite(Card.Suit suit, Card.Rank rank)
    {
        // Assuming you have a way to convert suit and rank to index
        int index = ((int)suit) * 13 + (int)rank - 1;
        return cardSprites[index];
    }

    // Call this method externally with the number of decks needed
    public void DisplayDecks(int count)
    {
        ClearDecks();
        InitializeDecks(count);
    }

    private void ClearDecks()
    {
        foreach (Transform child in deckParent)
        {
            Destroy(child.gameObject);
        }
        decks.Clear(); // Also clear the internal list of decks
    }

    private void InitializeDecks(int numberOfDecks)
    {
        for (int d = 0; d < numberOfDecks; d++)
        {
            List<Card> deck = new List<Card>();
            for (int i = 0; i < 52; i++)
            {
                deck.Add(new Card(i, cardSprites));
            }
            decks.Add(deck);
            CreateDeckVisual();

            // Debugging statement to log the count of cards
            Debug.Log($"Deck {d + 1} initialized with {deck.Count} cards.");
        }
    }

    private void CreateDeckVisual()
    {
        GameObject deckGO = Instantiate(deckPrefab, deckParent);
    }

    public void ShuffleAndDealCards()
    {
        // Merge all decks into one
        foreach (List<Card> deck in decks)
        {
            combinedDeck.AddRange(deck);
        }

        // Shuffle the combined deck
        System.Random rng = new System.Random();
        int n = combinedDeck.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            Card value = combinedDeck[k];
            combinedDeck[k] = combinedDeck[n];
            combinedDeck[n] = value;
        }

        // Now the deck is shuffled, let the HostManager handle the distribution
        originalDeck = combinedDeck;
        HostManager.Instance.DealCardsToPlayers(combinedDeck);
    }

    public void RemoveCardFromShuffledDeck(List<Card> cards)
    {
        foreach(Card removedCard in cards)
        {
            foreach(Card card in combinedDeck)
            {
                if(card.CardRank == removedCard.CardRank && card.CardSuit == removedCard.CardSuit)
                {
                    combinedDeck.Remove(card);
                    break;
                }
            }
        }
    }

    public void RemoveCard(string rank, string suit)
    {
        Card removedCard = null;
        //can be optimized
        foreach (Card card in originalDeck)
        {
            if (card.CardRank.ToString() == rank && card.CardSuit.ToString() == suit)
            {
                removedCard = card;
                Debug.Log("Player Discarded Card with Rank: " + rank);
                Debug.Log("Player Discarded Card with Suit: " + suit);
                break;
            }
        }
        combinedDeck.Remove(removedCard);
        discardedDeck.Add(removedCard);
    }

    public bool DiscardValid()
    {
        if (isHost)
        {
            return HostManager.Instance.HostDiscardValid();
        }
        else
        {
            return ClientManager.Instance.ClientDiscardValid();
        }
    }

    public void DiscardCard(int id)
    {
        if (isHost)
        {
            HostManager.Instance.HostDiscardCard(id);
        }
        else
        {
            ClientManager.Instance.ClientDiscardCard(id);
        }
    }

}
