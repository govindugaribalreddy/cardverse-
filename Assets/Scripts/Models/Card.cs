using UnityEngine;

public class Card
{
    public enum Suit
    {
        Clubs = 0,
        Diamonds = 1,
        Hearts = 2,
        Spades = 3
    }

    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    public Suit CardSuit { get; private set; }
    public Rank CardRank { get; private set; }
    public Sprite CardImage { get; private set; }

    // Constructor to create a card
    public Card(int index, Sprite[] cardSprites)
    {
        CardSuit = (Suit)(index / 13); // There are 4 suits, each with 13 cards
        CardRank = (Rank)(index % 13 + 1); // Ranks are 1 (Ace) through 13 (King)
        CardImage = cardSprites[index]; // Assign the corresponding sprite based on the index
    }

    // Returns a string that represents the card, useful for debugging
    public override string ToString()
    {
        return $"{CardRank} of {CardSuit}";
    }
}
