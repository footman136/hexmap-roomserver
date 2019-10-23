using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using GameUtils;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;

public class RoomLogic
{
    [SerializeField] private string _roomName;

    [SerializeField] private long _roomId;

    [SerializeField] private int _maxPlayerCount;

    [SerializeField] private long _creator;

    [SerializeField] private int _curPlayerCount;

    private readonly Dictionary<SocketAsyncEventArgs, PlayerInfo> Players = new Dictionary<SocketAsyncEventArgs, PlayerInfo>();

    public string RoomName => _roomName;
    public long RoomId => _roomId;
    public int MaxPlayerCount => _maxPlayerCount;
    public int CurPlayerCount => Players.Count;
    public long Creator => _creator;

    #region 初始化
    
    public void Init(RoomInfo roomInfo)
    {
        _roomName = roomInfo.RoomName;
        _roomId = roomInfo.RoomId;
        _maxPlayerCount = roomInfo.MaxPlayerCount;
        _creator = roomInfo.Creator;
        _curPlayerCount = 0;
        AddListener();
    }

    public void Fini()
    {
        RemoveListener();
    }

    public void AddListener()
    {
        MsgDispatcher.RegisterMsg((int)ROOM.CreateAtroop, OnCreateATroop);
        MsgDispatcher.RegisterMsg((int)ROOM.DestroyAtroop, OnDestroyATroop);
    }

    public void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM.CreateAtroop, OnCreateATroop);
        MsgDispatcher.UnRegisterMsg((int)ROOM.DestroyAtroop, OnDestroyATroop);
    }
    
    #endregion

    #region 玩家
    
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
    
    #endregion

    
    #region 消息处理

    /// <summary>
    /// 给房间内的所有人(包括自己)
    /// </summary>
    /// <param name="msgId">消息Id</param>
    /// <param name="output">消息</param>
    public void BroadcastMsg(ROOM_REPLY msgId, byte[] output)
    {
        foreach (var keyPair in Players)
        {
            RoomManager.Instance.SendMsg(keyPair.Key, msgId, output);
        }
    }
    
    void OnCreateATroop(SocketAsyncEventArgs args, byte[] bytes)
    {
        CreateATroop input = CreateATroop.Parser.ParseFrom(bytes);
        // 这条消息是在房间内部接收的，所以要判断这条消息是否属于这个房间。如果放在外部判断，可以通过外部的函数找到该房间，然后直接调用该房间的函数
        // 放在哪里都可以，目前放在这里是因为可以测试一下，同一条消息，多个函数注册是否会有BUG
        if (input.RoomId != _roomId)
            return;
        // 转发给房间内的所有玩家
        CreateATroopReply output = new CreateATroopReply()
        {
            Ret = true,
            ActorId = input.ActorId,
            Orientation = input.Orientation,
            OwnerId = input.OwnerId,
            PosX = input.PosX,
            PosZ = input.PosZ,
            Species = input.Species,
        };
        BroadcastMsg(ROOM_REPLY.CreateAtroopReply, output.ToByteArray());
    }
    void OnDestroyATroop(SocketAsyncEventArgs args, byte[] bytes)
    {
        DestroyATroop input = DestroyATroop.Parser.ParseFrom(bytes);
        if (input.RoomId != _roomId)
            return;
        DestroyATroopReply output = new DestroyATroopReply()
        {
            ActorId = input.ActorId,
            OwnerId = input.OwnerId,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.DestroyAtroopReply, output.ToByteArray());
    }
    
    #endregion
}
