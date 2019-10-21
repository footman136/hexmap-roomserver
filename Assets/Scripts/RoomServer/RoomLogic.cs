using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Protobuf.Room;
using UnityEngine;

public class RoomLogic : MonoBehaviour
{
    [SerializeField] private string _roomName;

    [SerializeField] private long _roomId;

    [SerializeField] private int _maxPlayerCount;

    [SerializeField] private long _creator;

    [SerializeField] private int _curPlayerCount;

    private Dictionary<SocketAsyncEventArgs, PlayerInfo> Players;

    public string RoomName => _roomName;
    public long RoomId => _roomId;
    public int MaxPlayerCount => _maxPlayerCount;
    public int CurPlayerCount => Players.Count;
    public long Creator => _creator;
    

    private void Awake()
    {
        Players = new Dictionary<SocketAsyncEventArgs, PlayerInfo>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Init(RoomInfo roomInfo)
    {
        _roomName = roomInfo.RoomName;
        _roomId = roomInfo.RoomId;
        _maxPlayerCount = roomInfo.MaxPlayerCount;
        _creator = roomInfo.Creator;
        _curPlayerCount = 0;
    }

    public void AddPlayer(SocketAsyncEventArgs args, long tokenId, string account)
    {
        PlayerInfo pi = new PlayerInfo()
        {
            Enter = new PlayerEnter()
            {
                TokenId = tokenId,
                Account = account,
            },
            RoomId = _roomId,
            IsCreatedByMe = tokenId == _creator,
        };
        Players[args] = pi;
        _curPlayerCount = Players.Count;
    }

    public void RemovePlayer(SocketAsyncEventArgs args)
    {
        if (Players.ContainsKey(args))
        {
            Players.Remove(args);
        }
        else
        {
            RoomManager.Instance.Log($"RoomLogic - RemovePlayer - Player not found!");
        }
        _curPlayerCount = Players.Count;
    }

    public void UpdateRoomInfoToLobby()
    {
        
    }

    public void BroadcastMsg(byte[] bytes)
    {
        
    }
}
