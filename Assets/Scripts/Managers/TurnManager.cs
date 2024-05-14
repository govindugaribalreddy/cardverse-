using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turn Manager handles the turn-based implementation across all players
/// </summary>
public class TurnManager
{

    public Dictionary<string, int> playerOrder = new Dictionary<string, int>(); // Username and corresponding turn order
    private int currentPlayerIndex = 0; // Index of the current player in the turn order
    private bool direction = true; //clockwise by default
    public Dictionary<string, int> cardsDrawnThisTurn = new Dictionary<string, int>();



    public int GetPlayerId(string username)
    {
        if (playerOrder.ContainsKey(username))
        {
            int id = playerOrder[username];
            Debug.Log($"GetPlayerId: Username '{username}' has ID '{id}'.");
            return id;
        }
        Debug.LogError($"GetPlayerId: Username '{username}' not found in playerOrder.");
        throw new Exception("Player not found");
    }


    // Add players to the turn order
    public void AddPlayer(string playerName)
    {
        if (!playerOrder.ContainsKey(playerName))
        {
            playerOrder.Add(playerName, playerOrder.Count); // Assign turn order based on the order they join
        }
        Debug.Log($"Player '{playerName}' added with ID {playerOrder[playerName]}");

    }

    public void SetDirection(bool direction)
    {
        this.direction = direction;
    }

    // Get the name of the current player whose turn it is
    public string GetCurrentPlayer()
    {
        foreach (var entry in playerOrder)
        {
            if (entry.Value == currentPlayerIndex)
            {
                return entry.Key;
            }
        }
        return null;
    }

    public bool IsTurn(string username)
    {
        foreach (var entry in playerOrder)
        {
            if (entry.Key == username)
            {
                bool isTurn = (entry.Value == currentPlayerIndex);
                Debug.Log($"IsTurn: It is '{isTurn}' that it's '{username}' turn.");
                return isTurn;
            }
        }
        Debug.LogWarning($"IsTurn: Username '{username}' not found.");
        return false;
    }


    public int GetCount()
    {
        return playerOrder.Count;
    }

    // Move to the next player's turn
    public void NextTurn()
    {
        string currentPlayer = GetCurrentPlayer();
        //ResetCardDraw(currentPlayer);
        if (!direction)
        {
            currentPlayerIndex = (currentPlayerIndex + 1) % playerOrder.Count;
        }
        else
        {
            currentPlayerIndex--;
            if (currentPlayerIndex < 0)
            {
                currentPlayerIndex = playerOrder.Count - 1;
            }
        }
    }

    public int GetPlayerPosition(string username)
    {
        foreach (var entry in playerOrder)
        {
            if (entry.Key == username)
            {
                return entry.Value;
            }
        }
        return -1;
    }

}
