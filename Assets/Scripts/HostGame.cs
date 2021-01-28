using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HostGame : MonoBehaviour
{
    [SerializeField]
    private uint _roomSize = 6;

    private string _roomName;
    private NetworkManager _networkManager;

    private void Start()
    {
        _networkManager = NetworkManager.singleton;
    }

    public void SetRoomName(string roomName)
    {
        _roomName = roomName;
    }

    public void CreateRoom()
    {
        if (!string.IsNullOrEmpty(_roomName))
        {
            Debug.Log("Creating Room " + _roomName + " with room for " + _roomSize + " players");

            //Create Room
        }
    }

}
