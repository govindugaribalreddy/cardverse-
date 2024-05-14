using System;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// This class is used to to send the data to different host/client using the Network Connection Object
/// </summary>
public class TransportHelper
{
    DataHelper Helper = new DataHelper();

    public void OnPlayerSendMessage(NetworkDriver playerDriver, NetworkConnection clientConnection, string message, string type)
    {
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
    }

    public void OnHostSendMessage(NetworkDriver hostDriver, NativeList<NetworkConnection> serverConnections, string message, string type)
    {
        NetworkData data = new NetworkData
        {
            type = type,
            value = message
        };

        byte[] bytes = Helper.TransformDataToBytes(data);

        // In this sample, we will simply broadcast a message to all connected clients.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            if (hostDriver.BeginSend(serverConnections[i], out var writer) == 0)
            {
                NativeArray<byte> nativeBytes = new NativeArray<byte>(bytes.Length, Allocator.Temp);
                nativeBytes.CopyFrom(bytes);

                // Write the NativeArray<byte> to the writer
                writer.WriteBytes(nativeBytes);

                // Release the NativeArray<byte>
                nativeBytes.Dispose();
                hostDriver.EndSend(writer);
            }
        }
    }

    public void OnHostSendMessageToClient(NetworkDriver hostDriver, NativeList<NetworkConnection> serverConnections, string message, string type, int id)
    {
        NetworkData data = new NetworkData
        {
            type = type,
            value = message
        };

        byte[] bytes = Helper.TransformDataToBytes(data);

        // In this sample, we will simply broadcast a message to all connected clients.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            if ((i == id - 1) && hostDriver.BeginSend(serverConnections[i], out var writer) == 0)
            {
                NativeArray<byte> nativeBytes = new NativeArray<byte>(bytes.Length, Allocator.Temp);
                nativeBytes.CopyFrom(bytes);

                // Write the NativeArray<byte> to the writer
                writer.WriteBytes(nativeBytes);

                // Release the NativeArray<byte>
                nativeBytes.Dispose();
                hostDriver.EndSend(writer);
            }
        }
    }

    public void SendSingleMessageToClient(NetworkDriver hostDriver, NetworkConnection clientConnection, string message, string type)
    {
        NetworkData data = new NetworkData
        {
            type = type,
            value = message
        };

        byte[] bytes = Helper.TransformDataToBytes(data);

        if (hostDriver.BeginSend(clientConnection, out var writer) == 0)
        {
            NativeArray<byte> nativeBytes = new NativeArray<byte>(bytes.Length, Allocator.Temp);
            nativeBytes.CopyFrom(bytes);

            // Write the NativeArray<byte> to the writer
            writer.WriteBytes(nativeBytes);

            // Release the NativeArray<byte>
            nativeBytes.Dispose();

            // Complete sending the message
            hostDriver.EndSend(writer);
        }
        else
        {
            Debug.LogError("Failed to begin sending message to client");
        }
    }


    public void CleanHostConnections(NativeList<NetworkConnection> serverConnections)
    {
        // Clean up stale connections.
        for (int i = 0; i < serverConnections.Length; i++)
        {
            if (!serverConnections[i].IsCreated)
            {
                serverConnections.RemoveAt(i);
                --i;
            }
        }
    }
}
