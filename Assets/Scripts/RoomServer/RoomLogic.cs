using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using GameUtils;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using Actor;
using AI;
using Google.Protobuf.Reflection;
using UnityEditorInternal;

public class RoomLogic
{
    private string _roomName;

    private long _roomId;

    private int _maxPlayerCount;

    private long _creator;

    private int _curPlayerCount;

    public HexmapHelper HexmapHelper = new HexmapHelper();
    public ActorManager ActorManager = new ActorManager();
    public UrbanManager UrbanManager = new UrbanManager();
    

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
        MsgDispatcher.RegisterMsg((int)ROOM.TroopMove, OnTroopMove);
        MsgDispatcher.RegisterMsg((int)ROOM.TroopAiState, OnTroopAiState);
        MsgDispatcher.RegisterMsg((int)ROOM.AskForCities, OnAskForCities);
        MsgDispatcher.RegisterMsg((int)ROOM.CityAdd, OnCityAdd);
        MsgDispatcher.RegisterMsg((int)ROOM.CityRemove, OnCityRemove);
    }

    public void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM.CreateAtroop, OnCreateATroop);
        MsgDispatcher.UnRegisterMsg((int)ROOM.DestroyAtroop, OnDestroyATroop);
        MsgDispatcher.UnRegisterMsg((int)ROOM.TroopMove, OnTroopMove);
        MsgDispatcher.UnRegisterMsg((int)ROOM.TroopAiState, OnTroopAiState);
        MsgDispatcher.UnRegisterMsg((int)ROOM.AskForCities, OnAskForCities);
        MsgDispatcher.UnRegisterMsg((int)ROOM.CityAdd, OnCityAdd);
        MsgDispatcher.UnRegisterMsg((int)ROOM.CityRemove, OnCityRemove);
    }
    
    #endregion

    #region 地图

    public bool SetMap(byte[] mapdata)
    {
        // Oct.24.2019. Liu Gang
        // 服务器读取地图数据这条路暂时搁置。因为地图数据这里与Unity的显示架构（MonoBehaviour）密切相关，这会导致两个问题：
        // 1，这个地图数据结构是chunk模式的，HexCell都会从属于某个chunk，就算去掉chunk，unit的移动也会与此有关，剥离代码需要较长时间
        // 2，因为很难做成数据/渲染分开的模式（如果要修改也需要较长的时间），这会导致服务器和客户端要同时维护两套代码
        // ——所以，这个工作，我建议放在后面进行，比如确定使用java或者go语言来制作服务器的时候，专门给地图制作数据
        // _hexmapHelper.Load(mapdata);
        return true;
    }

    public void SavePlayer(SocketAsyncEventArgs args)
    {
        PlayerInfo pi = GetPlayer(args);
        if (pi == null)
        {
            RoomManager.Instance.Log("SavePlayer Error - player not found!");
            return;
        }

        long ownerId = pi.Enter.TokenId;
        
        // 保存该玩家的城市数据
        byte[] cityBytes = UrbanManager.SaveBuffer();
        if (cityBytes.Length > 1024 * 8)
        {
            RoomManager.Instance.Log($"SavePlayer Error - save buffer is too large:{cityBytes.Length} bytes");
        }

        string tableName = $"MAP:{RoomId}";
        string keyName = $"Cities";
        RoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, cityBytes );
    }

    public void LoadPlayer(SocketAsyncEventArgs args)
    {
        PlayerInfo pi = GetPlayer(args);
        if (pi == null)
        {
            RoomManager.Instance.Log("LoadPlayer Error - player not found!");
            return;
        }

        long ownerId = pi.Enter.TokenId;
        
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Cities";
        byte[] cityBytes = RoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        if (cityBytes != null)
        {
            if (!UrbanManager.LoadBuffer(cityBytes, cityBytes.Length))
            {
                RoomManager.Instance.Log("RoomLogic LoadPlayer Error - LoadBuffer Failed!");
            }
            else
            {
                RoomManager.Instance.Log($"RoomLogic LoadPlayer OK - 城市个数：{UrbanManager.Cities.Count}");
            }
        }
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

    public PlayerInfo GetPlayer(SocketAsyncEventArgs args)
    {
        if (Players.ContainsKey(args))
        {
            return Players[args];
        }

        return null;
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

        if (!Players.ContainsKey(args))
        {
            CreateATroopReply output = new CreateATroopReply()
            {
                Ret = false,
            };
            RoomManager.Instance.SendMsg(args, ROOM_REPLY.CreateAtroopReply, output.ToByteArray());
            RoomManager.Instance.Log($"MSG: CreateATroop - 当前玩家不在本房间！房间名:{RoomName} - 玩家个数:{Players.Count}");
        }
        else
        {
            ActorManager.AddActor(input.RoomId, input.OwnerId, input.ActorId, input.PosX, input.PosZ,
                input.Orientation, input.Species);
            
            // 转发给房间内的所有玩家
            CreateATroopReply output = new CreateATroopReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                Orientation = input.Orientation,
                PosX = input.PosX,
                PosZ = input.PosZ,
                Species = input.Species,
                Ret = true,
            };
            BroadcastMsg(ROOM_REPLY.CreateAtroopReply, output.ToByteArray());
        }
    }
    void OnDestroyATroop(SocketAsyncEventArgs args, byte[] bytes)
    {
        DestroyATroop input = DestroyATroop.Parser.ParseFrom(bytes);
        if (input.RoomId != _roomId)
            return;
        
        ActorManager.RemoveActor(input.ActorId);
        
        DestroyATroopReply output = new DestroyATroopReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.DestroyAtroopReply, output.ToByteArray());
    }
    
    private void OnTroopMove(SocketAsyncEventArgs args, byte[] bytes)
    {
        TroopMove input = TroopMove.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        TroopMoveReply output = new TroopMoveReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            PosFromX = input.PosFromX,
            PosFromZ = input.PosFromZ,
            PosToX = input.PosToX,
            PosToZ = input.PosToZ,
            Speed = input.Speed,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.TroopMoveReply, output.ToByteArray());
    }

    private void OnTroopAiState(SocketAsyncEventArgs args, byte[] bytes)
    {
        TroopAiState input = TroopAiState.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        TroopAiStateReply output = new TroopAiStateReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            State = input.State,
            PosFromX = input.PosFromX,
            PosFromZ = input.PosFromZ,
            PosToX = input.PosToX,
            PosToZ = input.PosToZ,
            Speed = input.Speed,
            Ret = true,
        };        
        BroadcastMsg(ROOM_REPLY.TroopAiStateReply, output.ToByteArray());
    }

    private void OnAskForCities(SocketAsyncEventArgs args, byte [] bytes)
    {
        AskForCities input = AskForCities.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        PlayerInfo pi = GetPlayer(args);
        if (pi == null)
        {
            AskForCitiesReply output = new AskForCitiesReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                Ret = false,
            };
            RoomManager.Instance.SendMsg(args, ROOM_REPLY.AskForCitiesReply, output.ToByteArray());
            return;
        }

        long OwnerId = pi.Enter.TokenId;

        long capitalCityId = 0;
        foreach (var keyValue in UrbanManager.Cities)
        {
            UrbanCity city = keyValue.Value;
            if (city.IsCapital && city.OwnerId == OwnerId)
                capitalCityId = city.CityId;
            CityAddReply output = new CityAddReply()
            {
                RoomId = city.RoomId,
                OwnerId = city.OwnerId,
                CityId = city.CityId,
                PosX = city.PosX,
                PosZ = city.PosZ,
                CellIndex = city.CellIndex,
                CityName = city.CityName,
                CitySize = city.CitySize,
                IsCapital = city.IsCapital,
                Ret = true,
            }; 
            RoomManager.Instance.SendMsg(args, ROOM_REPLY.CityAddReply, output.ToByteArray());
        }

        {
            AskForCitiesReply output = new AskForCitiesReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                TotalCityCount = UrbanManager.Cities.Count,
                MyCityCount = UrbanManager.GetMyCityCount(OwnerId),
                CapitalCityId = capitalCityId,
                Ret = true,
            };
            RoomManager.Instance.SendMsg(args, ROOM_REPLY.AskForCitiesReply, output.ToByteArray());
        }
    }

    private void OnCityAdd(SocketAsyncEventArgs args, byte[] bytes)
    {
        CityAdd input = CityAdd.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        bool isCapital = UrbanManager.GetMyCityCount(input.OwnerId) == 0; // 第一座城市是都城
        CityAddReply output = new CityAddReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            CityId = input.CityId,
            PosX = input.PosX,
            PosZ = input.PosZ,
            CellIndex = input.CellIndex,
            CityName = input.CityName,
            CitySize = input.CitySize,
            IsCapital = isCapital,
            Ret = true,
        };
        UrbanCity city = new UrbanCity()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            CityId = input.CityId,
            PosX = input.PosX,
            PosZ = input.PosZ,
            CellIndex = input.CellIndex,
            CityName = input.CityName,
            CitySize = input.CitySize,
            IsCapital = isCapital,
        };
        UrbanManager.AddCity(city);
        BroadcastMsg(ROOM_REPLY.CityAddReply, output.ToByteArray());
    }

    private void OnCityRemove(SocketAsyncEventArgs args, byte[] bytes)
    {
        CityRemove input = CityRemove.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        CityRemoveReply output = new CityRemoveReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            CityId = input.CityId,
            Ret = true,
        };
        UrbanManager.RemoveCity(input.CityId);
        BroadcastMsg(ROOM_REPLY.CityRemoveReply, output.ToByteArray());
    }
        
    #endregion
}
