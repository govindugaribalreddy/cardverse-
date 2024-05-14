using System;
using System.Text;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

/// <summary>
/// This class process the data and transform it to usable format
/// </summary>
public class DataHelper
{
    NetworkDriver Driver;

    public DataHelper()
    {
    }

    public byte[] TransformDataToBytes(NetworkData data)
    {
        string json = JsonUtility.ToJson(data);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        return bytes;
    }

    public NetworkData TransformDataToObject(DataStreamReader stream)
    {
        NativeArray<byte> receivedBytes = new NativeArray<byte>(stream.Length, Allocator.Temp);

        // Read bytes from the stream into the NativeArray<byte>
        stream.ReadBytes(receivedBytes);

        // Convert the NativeArray<byte> to a regular byte array
        byte[] byteArray = new byte[receivedBytes.Length];
        receivedBytes.CopyTo(byteArray);

        // Convert the byte array to a string using UTF-8 encoding
        string jsonString = Encoding.UTF8.GetString(byteArray);

        // Deserialize the JSON string into a NetworkData object
        NetworkData receivedData = JsonUtility.FromJson<NetworkData>(jsonString);

        // Dispose the NativeArray<byte>
        receivedBytes.Dispose();// Now you can access the receivedData object

        return receivedData;
    }

}
