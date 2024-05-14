using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// RoomData model to hold information about the room
[System.Serializable]
public class GameSettings//: INetworkSerializable
{
    public string RoomName;
    public string JoinCode;
    public int MaxPlayers;
    public bool CardRotations;//true for clockwise
    public int PlayersCount;
    public int JokersCount;
    public int DecksCount;
    public string HostName;
    public int PlayerTimeout; //in secs
    public int CardsCount; //Based on the game

    // Constructor to initialize room data
    public GameSettings()
    {
        CardRotations = true;
        JokersCount = 8;
        DecksCount = 1;
        PlayerTimeout = 30;
    }

    //public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    //{
    //    serializer.SerializeValue(ref RoomName);
    //    serializer.SerializeValue(ref JoinCode);
    //    serializer.SerializeValue(ref MaxPlayers);
    //    serializer.SerializeValue(ref CardRotations);
    //    serializer.SerializeValue(ref JokersCount);
    //    serializer.SerializeValue(ref DecksCount);
    //    serializer.SerializeValue(ref HostName);
    //    serializer.SerializeValue(ref IsHost);
    //    serializer.SerializeValue(ref PlayersCount);
    //    serializer.SerializeValue(ref PlayerTimeout);
    //}
}

