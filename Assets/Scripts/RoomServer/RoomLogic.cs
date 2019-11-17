using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Sockets;
using GameUtils;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using Actor;
using AI;
using UnityEditorInternal;

public class RoomLogic
{
    private string _roomName;

    private long _roomId;

    private int _maxPlayerCount;

    private long _creator;

    private int _curPlayerCount;

    public ActorManager ActorManager = new ActorManager();
    public UrbanManager UrbanManager = new UrbanManager();
    public ResManager ResManager = new ResManager();
    

    private readonly Dictionary<SocketAsyncEventArgs, PlayerInfo> PlayersInRoom = new Dictionary<SocketAsyncEventArgs, PlayerInfo>();

    public string RoomName => _roomName;
    public long RoomId => _roomId;
    public int MaxPlayerCount => _maxPlayerCount;
    public int CurPlayerCount => PlayersInRoom.Count;
    public long Creator => _creator;

    #region 初始化
    
    public void Init(NetRoomInfo roomInfo)
    {
        _roomName = roomInfo.RoomName;
        _roomId = roomInfo.RoomId;
        _maxPlayerCount = roomInfo.MaxPlayerCount;
        _creator = roomInfo.Creator;
        _curPlayerCount = 0;

        // 取盘
        LoadCity();
        LoadActor();
        LoadRes();
        
        AddListener();
    }

    public void Fini()
    {
        RemoveListener();
    }

    public void AddListener()
    {
        MsgDispatcher.RegisterMsg((int)ROOM.CityAdd, OnCityAdd);
        MsgDispatcher.RegisterMsg((int)ROOM.CityRemove, OnCityRemove);
        
        MsgDispatcher.RegisterMsg((int)ROOM.ActorAdd, OnActorAdd);
        MsgDispatcher.RegisterMsg((int)ROOM.ActorRemove, OnActorRemove);
        MsgDispatcher.RegisterMsg((int)ROOM.ActorMove, OnActorMove);
        MsgDispatcher.RegisterMsg((int)ROOM.ActorAiState, OnActorAiState);
        MsgDispatcher.RegisterMsg((int)ROOM.UpdateActorPos, OnUpdateActorPos);
        MsgDispatcher.RegisterMsg((int)ROOM.ActorPlayAni, OnActorPlayAni);
        MsgDispatcher.RegisterMsg((int)ROOM.TryCommand, OnTryCommand);
        
        MsgDispatcher.RegisterMsg((int)ROOM.HarvestStart, OnHarvestStart);
        MsgDispatcher.RegisterMsg((int)ROOM.HarvestStop, OnHarvestStop);
        MsgDispatcher.RegisterMsg((int)ROOM.UpdateRes, OnUpdateRes);
        MsgDispatcher.RegisterMsg((int)ROOM.UpdateActionPoint, OnUpdateActionPoint);
        
        MsgDispatcher.RegisterMsg((int)ROOM.FightStart, OnFightStart);
        MsgDispatcher.RegisterMsg((int)ROOM.FightStop, OnFightStop);
    }

    public void RemoveListener()
    {
        MsgDispatcher.UnRegisterMsg((int)ROOM.CityAdd, OnCityAdd);
        MsgDispatcher.UnRegisterMsg((int)ROOM.CityRemove, OnCityRemove);
        
        MsgDispatcher.UnRegisterMsg((int)ROOM.ActorAdd, OnActorAdd);
        MsgDispatcher.UnRegisterMsg((int)ROOM.ActorRemove, OnActorRemove);
        MsgDispatcher.UnRegisterMsg((int)ROOM.ActorMove, OnActorMove);
        MsgDispatcher.UnRegisterMsg((int)ROOM.ActorAiState, OnActorAiState);
        MsgDispatcher.UnRegisterMsg((int)ROOM.UpdateActorPos, OnUpdateActorPos);
        MsgDispatcher.UnRegisterMsg((int)ROOM.ActorPlayAni, OnActorPlayAni);
        MsgDispatcher.UnRegisterMsg((int)ROOM.TryCommand, OnTryCommand);
        
        MsgDispatcher.UnRegisterMsg((int)ROOM.HarvestStart, OnHarvestStart);
        MsgDispatcher.UnRegisterMsg((int)ROOM.HarvestStop, OnHarvestStop);
        MsgDispatcher.UnRegisterMsg((int)ROOM.UpdateRes, OnUpdateRes);
        MsgDispatcher.UnRegisterMsg((int)ROOM.UpdateActionPoint, OnUpdateActionPoint);
        
        MsgDispatcher.UnRegisterMsg((int)ROOM.FightStart, OnFightStart);
        MsgDispatcher.UnRegisterMsg((int)ROOM.FightStop, OnFightStop);
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
        
        LoadRes();
        return true;
    }
    #endregion
    
    #region 存盘/取盘

    /// <summary>
    /// 保存基础信息,仅供查看
    /// </summary>
    /// <param name="args"></param>
    public void SaveCommonInfo(SocketAsyncEventArgs args)
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Infos";
        string info = $"Total City Count:{UrbanManager.AllCities.Count} | Total Actor Count:{ActorManager.AllActors.Count} | Total Res Count:{ResManager.AllRes.Count}";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, info);
        
        // 本玩家身上的物体的数量
        PlayerInfo pi = GetPlayerInRoom(args);
        if (pi == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic SaveCommonInfo Error - player not found!");
            return;
        }

        long ownerId = pi.Enter.TokenId;
        keyName = $"Infos-{ownerId}";
        info = $"City Count:{UrbanManager.CountOfThePlayer(ownerId)}/{UrbanManager.AllCities.Count} | Actor Count:{ActorManager.CountOfThePlayer(ownerId)}/{ActorManager.AllActors.Count} | Res Count:{ResManager.AllRes.Count}";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, info);
    }

    /// <summary>
    /// 保存该地图上的资源数据
    /// </summary>
    public void SaveRes()
    {
        byte[] resBytes = ResManager.SaveBuffer();
        if (resBytes.Length > 1024 * 32)
        {
            ServerRoomManager.Instance.Log($"RoomLogic Save resrouce Error - save resrouce buffer is too large:{resBytes.Length} bytes");
        }

        string tableName = $"MAP:{RoomId}";
        string keyName = $"Resources";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, resBytes );

        ServerRoomManager.Instance.Log($"RoomLogic Save resource OK - Res Count:{ResManager.AllRes.Count}");
    }

    /// <summary>
    /// 读取地图上的资源数据
    /// </summary>
    public void LoadRes()
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Resources";
        byte[] resBytes = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        if (resBytes == null)
        {
            ServerRoomManager.Instance.Log($"RoomLogic LoadRes Error - Resource Data not found in Redis! 如果是新战场则不是错误! - Key:{keyName}");
            return;
        }

        ServerRoomManager.Instance.Log(!ResManager.LoadBuffer(resBytes, resBytes.Length)
            ? "RoomLogic LoadRes Error - Resource LoadBuffer Failed!"
            : $"RoomLogic LoadRes OK - 资源个数：{ResManager.AllRes.Count}");
    }

    /// <summary>
    /// 保存该玩家的城市数据
    /// </summary>
    public void SaveCity()
    {
        byte[] cityBytes = UrbanManager.SaveBuffer();
        if (cityBytes.Length > 1024 * 8)
        {
            ServerRoomManager.Instance.Log($"RoomLogic SaveCity Error - save buffer is too large:{cityBytes.Length} bytes");
        }

        string tableName = $"MAP:{RoomId}";
        string keyName = $"Cities";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, cityBytes);
        ServerRoomManager.Instance.Log(
            $"RoomLogic SaveCity OK - City Count:{UrbanManager.AllCities.Count}");
    }

    /// <summary>
    /// 读取该玩家的城市数据
    /// </summary>
    public void LoadCity()
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Cities";
        byte[] cityBytes = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        if (cityBytes == null)
        {
            ServerRoomManager.Instance.Log($"RoomLogic LoadCity Error - City Data not found in Redis! 如果是新战场则不是错误! - Key:{keyName}");
            return;
        }

        ServerRoomManager.Instance.Log(!UrbanManager.LoadBuffer(cityBytes, cityBytes.Length)
            ? "RoomLogic LoadCity Error - City LoadBuffer Failed!"
            : $"RoomLogic LoadCity OK - 城市个数：{UrbanManager.AllCities.Count}");
    }

    /// <summary>
    /// 保存该玩家的单元数据
    /// </summary>
    public void SaveActor()
    {
        byte[] actorBytes = ActorManager.SaveBuffer();
        if (actorBytes.Length > 1024 * 32)
        {
            ServerRoomManager.Instance.Log($"RoomLogic SaveActor Error - save buffer is too large:{actorBytes.Length} bytes");
        }
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Actors";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, actorBytes );
        
        ServerRoomManager.Instance.Log($"RoomLogic SaveActor OK - City Count:{ActorManager.AllActors.Count}");
    }

    /// <summary>
    /// 读取该玩家的单元数据
    /// </summary>
    public void LoadActor()
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Actors";
        byte[] actorBytes = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        if (actorBytes == null)
        {
            ServerRoomManager.Instance.Log($"RoomLogic LoadActor Error - Actor Data not found in Redis! 如果是新战场则不是错误! - Key:{keyName}");
            return;
        }

        ServerRoomManager.Instance.Log(!ActorManager.LoadBuffer(actorBytes, actorBytes.Length, this)
            ? "RoomLogic LoadActor Error - Actor LoadBuffer Failed!"
            : $"RoomLogic LoadActor OK - 单元个数：{ActorManager.AllActors.Count}");
    }

    public void SavePlayer(PlayerInfo pi)
    {
        if (pi == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic SavePlayer Error - Player not found!");
            return;
        }

        byte[] playerBytes = pi.SaveBuffer();
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Player-{pi.Enter.TokenId}";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, playerBytes );
    
        ServerRoomManager.Instance.Log($"RoomLogic SavePlayer OK - Player:{pi.Enter.Account}");
    }

    public bool LoadPlayer(PlayerInfo pi)
    {
        if (pi == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic LoadPlayer Error - Player not found!");
            return false;
        }
        
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Player-{pi.Enter.TokenId}";
        byte[] playerBytes = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        if (playerBytes == null)
        {
            ServerRoomManager.Instance.Log($"RoomLogic LoadPlayer Error - Player Data not found in Redist! 如果是新玩家则不是错误! - Player:{pi.Enter.Account} - Key:{keyName}");
            return false;
        }

        bool ret = pi.LoadBuffer(playerBytes, playerBytes.Length);
        string msg;
        if (ret)
        {
            msg = $"RoomLogic LoadPlayer OK - Player:{pi.Enter.Account}";
        }
        else
        {
            msg = $"RoomLogic LoadPlayer Error - Player LoadBuffer Failed! - Player:{pi.Enter.Account}";
        }

        ServerRoomManager.Instance.Log(msg);
        return true;
    }
    
    #endregion
    
    #region 玩家
    
    /// <summary>
    /// 房间里的[玩家管理器]PlayerInRoom是对整个房间服务器里的[玩家管理器]Players的引用
    /// </summary>
    /// <param name="args"></param>
    /// <param name="pi"></param>
    public void AddPlayerToRoom(SocketAsyncEventArgs args, PlayerInfo pi)
    {
        if (!LoadPlayer(pi))
        { // 如果没有存盘,则读取初始数据
            var csv = CsvDataManager.Instance.GetTable("battle_init");
            int wood = csv.GetValueInt(1, "PlayerInit_Wood");
            int food = csv.GetValueInt(1, "PlayerInit_Food");
            int iron = csv.GetValueInt(1, "PlayerInit_Iron");
            int actionPointMax = csv.GetValueInt(1, "MaxActionPoint");
            pi.AddWood(wood);
            pi.AddFood(food);
            pi.AddIron(iron);
            pi.SetActionPointMax(actionPointMax);
            pi.AddActionPoint(actionPointMax);
            pi.TimeSinceLastSave = DateTime.Now.ToFileTime();
            pi.TimeSinceLastRestoreActionPoint = 0;
        }
        
        // 玩家进入以后,根据该玩家[离开游戏]的时间,到[现在]的时间差(秒),计算出应该恢复多少的行动点数, 一次性恢复之
        ServerRoomManager.Instance.RestoreActionPointAfterLoading(pi);

        PlayersInRoom[args] = pi;
        _curPlayerCount = PlayersInRoom.Count;
        ServerRoomManager.Instance.Log($"RoomLogic AddPlayer OK - 玩家进入战场! Player:{pi.Enter.Account}");
    }

    public void RemovePlayerFromRoom(SocketAsyncEventArgs args)
    {
        var pi = GetPlayerInRoom(args);
        SavePlayer(pi);
        
        if (PlayersInRoom.ContainsKey(args))
        {
            PlayersInRoom.Remove(args);
        }
        else
        {
            ServerRoomManager.Instance.Log($"RoomLogic - RemovePlayer - Player not found!");
        }
        _curPlayerCount = PlayersInRoom.Count;
        ServerRoomManager.Instance.Log($"RoomLogic RemovePlayer OK - 玩家离开战场! Player:{pi.Enter.Account}");
    }

    public PlayerInfo GetPlayerInRoom(SocketAsyncEventArgs args)
    {
        if (PlayersInRoom.ContainsKey(args))
        {
            return PlayersInRoom[args];
        }

        return null;
    }

    #endregion
    
    /// <summary>
    /// 给房间内的所有人(包括自己)
    /// </summary>
    /// <param name="msgId">消息Id</param>
    /// <param name="output">消息</param>
    public void BroadcastMsg(ROOM_REPLY msgId, byte[] output)
    {
        foreach (var keyPair in PlayersInRoom)
        {
            ServerRoomManager.Instance.SendMsg(keyPair.Key, msgId, output);
        }
    }
    
    #region 消息处理 - 城市
    
    private void OnCityAdd(SocketAsyncEventArgs args, byte[] bytes)
    {
        CityAdd input = CityAdd.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        // 删除建造城市的开拓者
        {
            bool ret = ActorManager.RemoveActor(input.CreatorId);

            ActorRemoveReply output = new ActorRemoveReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.CreatorId,
                Ret = ret,
            };
            BroadcastMsg(ROOM_REPLY.ActorRemoveReply, output.ToByteArray());
        }

        if (input.CellIndex == 0)
        {
            Debug.LogError("OnCityAdd Fuck!!! City position is lost!!!");
        }
        
        bool isCapital = UrbanManager.CountOfThePlayer(input.OwnerId) == 0; // 第一座城市是都城
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
        
        {
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
            BroadcastMsg(ROOM_REPLY.CityAddReply, output.ToByteArray());
        }
    }

    private void OnCityRemove(SocketAsyncEventArgs args, byte[] bytes)
    {
        CityRemove input = CityRemove.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        bool ret = UrbanManager.RemoveCity(input.CityId);
        CityRemoveReply output = new CityRemoveReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            CityId = input.CityId,
            Ret = ret,
        };
        BroadcastMsg(ROOM_REPLY.CityRemoveReply, output.ToByteArray());
    }
    
    #endregion
    
    #region 消息处理 - 单元

    private void OnActorAdd(SocketAsyncEventArgs args, byte[] bytes)
    {
        ActorAdd input = ActorAdd.Parser.ParseFrom(bytes);
        // 这条消息是在房间内部接收的，所以要判断这条消息是否属于这个房间。如果放在外部判断，可以通过外部的函数找到该房间，然后直接调用该房间的函数
        // 放在哪里都可以，目前放在这里是因为可以测试一下，同一条消息，多个函数注册是否会有BUG
        if (input.RoomId != _roomId)
            return;

        if (!PlayersInRoom.ContainsKey(args))
        {
            ActorAddReply output = new ActorAddReply()
            {
                Ret = false,
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.ActorAddReply, output.ToByteArray());
            ServerRoomManager.Instance.Log($"MSG: ActorAdd - 当前玩家不在本本战场！战场名:{RoomName} - 玩家个数:{PlayersInRoom.Count}");
        }
        else
        {
            if (input.CellIndex == 0)
            {
                Debug.LogError("OnActorAdd Fuck!!! Actor position is lost!!!");
            }
            ActorBehaviour ab = new ActorBehaviour()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                PosX = input.PosX,
                PosZ = input.PosZ,
                CellIndex = input.CellIndex,
                Orientation = input.Orientation,
                Species = input.Species,
                ActorInfoId = input.ActorInfoId,
            };
            ab.LoadFromTable();
            ActorManager.AddActor(ab, this);
            
            // 转发给房间内的所有玩家
            ActorAddReply output = new ActorAddReply()
            {
                RoomId = ab.RoomId,
                OwnerId = ab.OwnerId,
                ActorId = ab.ActorId,
                Orientation = ab.Orientation,
                PosX = ab.PosX,
                PosZ = ab.PosZ,
                CellIndex = ab.CellIndex,
                Species = ab.Species,
                ActorInfoId = ab.ActorInfoId,
                
                Name = ab.Name,
                Hp = ab.Hp,
                HpMax = ab.HpMax,
                AttackPower = ab.AttackPower,
                DefencePower = ab.DefencePower,
                Speed = ab.Speed,
                FieldOfVision = ab.FieldOfVision,
                ShootingRange = ab.ShootingRange,
                
                AttackDuration = ab.AttackDuration,
                AttackInterval = ab.AttackInterval,
                AmmoBase = ab.AmmoBase,
                AmmoBaseMax = ab.AmmoBaseMax,
                
                Ret = true,
            };
            BroadcastMsg(ROOM_REPLY.ActorAddReply, output.ToByteArray());
        }
    }
    private void OnActorRemove(SocketAsyncEventArgs args, byte[] bytes)
    {
        ActorRemove input = ActorRemove.Parser.ParseFrom(bytes);
        if (input.RoomId != _roomId)
            return;
        
        bool ret = ActorManager.RemoveActor(input.ActorId);
        
        ActorRemoveReply output = new ActorRemoveReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            Ret = ret,
        };
        BroadcastMsg(ROOM_REPLY.ActorRemoveReply, output.ToByteArray());
    }
    
    private void OnActorMove(SocketAsyncEventArgs args, byte[] bytes)
    {
        ActorMove input = ActorMove.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        if (input.CellIndexFrom == 0 || input.CellIndexTo == 0)
        {
            Debug.LogError("OnActorMove Fuck!!! Actor position is lost!!!");
        }
        ActorMoveReply output = new ActorMoveReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            CellIndexFrom = input.CellIndexFrom,
            CellIndexTo = input.CellIndexTo,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.ActorMoveReply, output.ToByteArray());
    }

    private void OnActorAiState(SocketAsyncEventArgs args, byte[] bytes)
    {
        ActorAiState input = ActorAiState.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        if (input.CellIndexFrom == 0)
        {
            Debug.LogError("OnActorAiState Fuck!!! Actor position is lost!!!");
        }
        
        // 更新单元Ai信息,在服务器的ActorBehaviour里保存一份
        var ab = ActorManager.GetActor(input.ActorId);
        if (ab != null)
        {
            ab.AiState = input.State;
            ab.AiTargetId = input.TargetId;
            ab.CellIndex = input.CellIndexFrom;
            ab.AiCellIndexTo = input.CellIndexTo;
            ab.Orientation = input.Orientation;
        }
        
        ActorAiStateReply output = new ActorAiStateReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            TargetId = input.TargetId,
            State = input.State,
            CellIndexFrom = input.CellIndexFrom,
            CellIndexTo = input.CellIndexTo,
            Orientation = input.Orientation,
            Speed = input.Speed,
            Ret = true,
        };
        
        BroadcastMsg(ROOM_REPLY.ActorAiStateReply, output.ToByteArray());
    }

    private void OnUpdateActorPos(SocketAsyncEventArgs args, byte[] bytes)
    {
        UpdateActorPos input = UpdateActorPos.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        if (input.CellIndex == 0)
        {
            Debug.LogError("OnUpdateActorPos Fuck!!! Actor position is lost!!!");
        }
        var ab = ActorManager.GetActor(input.ActorId);
        if (ab != null)
        {
            ab.PosX = input.PosX;
            ab.PosZ = input.PosZ;
            ab.CellIndex = input.CellIndex;
            ab.Orientation = input.Orientation;
        }
        
        // 这个消息不用返回了
    }

    private void OnActorPlayAni(SocketAsyncEventArgs args, byte[] bytes)
    {
        ActorPlayAni input = ActorPlayAni.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        ActorPlayAniReply output = new ActorPlayAniReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            AiState = input.AiState,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.ActorPlayAniReply, output.ToByteArray());
    }

    private void OnTryCommand(SocketAsyncEventArgs args, byte[] bytes)
    {
        TryCommand input = TryCommand.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        PlayerInfo pi = GetPlayerInRoom(args);
        if (pi == null || pi.Enter.TokenId != input.OwnerId)
        {
            string msg = "在服务器没有找到本玩家!";
            TryCommandReply output = new TryCommandReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.TryCommandReply, output.ToByteArray());
            ServerRoomManager.Instance.Log("RoomLogic OnTryCommand Error - " + msg);
            return;
        }
        
        var csv = CsvDataManager.Instance.GetTable("command_id");
        int actionPointCost = csv.GetValueInt(input.CommandId, "ActionPointCost");
        if (actionPointCost > 0)
        {
            bool ret = true;
            string msg = "";
            if (actionPointCost != input.ActionPointCost)
            { // 服务器校验一下
                msg = $"行动点数服务器与客户端不一致! ({input.ActionPointCost} : {actionPointCost})";
                ret = false;
            }

            if (pi.ActionPoint < input.ActionPointCost)
            {
                msg = "行动点不足, 请稍后再试!";
                ret = false;
            }

            if(!ret)
            {
                TryCommandReply output = new TryCommandReply()
                {
                    RoomId = input.RoomId,
                    OwnerId = input.OwnerId,
                    Ret = false,
                    ErrMsg = msg,
                };
                ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.TryCommandReply, output.ToByteArray());
                ServerRoomManager.Instance.Log("RoomLogic OnTryCommand Error - " + msg);
                return;
            }
            // 扣除行动点数
            pi.AddActionPoint(-input.ActionPointCost);
        }

        {
            // 行动点发生变化,要通知客户端
            UpdateActionPointReply output = new UpdateActionPointReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActionPoint = pi.ActionPoint,
                ActionPointMax = pi.ActionPointMax,
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.UpdateActionPointReply, output.ToByteArray());
            TryCommandReply output2 = new TryCommandReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.TryCommandReply, output2.ToByteArray());
            ServerRoomManager.Instance.Log($"RoomLogic OnTryCommand OK - Permission granted! - CommandId");
        }
    }
    
    #endregion
    
    #region 消息处理 - 资源采集

    private void OnHarvestStart(SocketAsyncEventArgs args, byte[] bytes)
    {
        HarvestStart input = HarvestStart.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        if (input.CellIndex == 0)
        {
            Debug.LogError("OnHarvestStart Fuck!!! Actor position is lost!!!");
        }
        HarvestStartReply output = new HarvestStartReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            CellIndex = input.CellIndex,
            ResType = input.ResType,
            ResRemain = input.ResRemain,
            DurationTime = input.DurationTime,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.HarvestStartReply, output.ToByteArray());
    }
    private void OnHarvestStop(SocketAsyncEventArgs args, byte[] bytes)
    {
        HarvestStop input = HarvestStop.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过

        if (input.CellIndex == 0)
        {
            Debug.LogError("OnHarvestStop Fuck!!! Actor position is lost!!!");
        }
        // 修改地图上的资源数据
        var hr = ResManager.GetRes(input.CellIndex);
        if (hr == null)
        {
            hr = new ResInfo();
            ResManager.AddRes(input.CellIndex, hr);
        }

        hr.SetAmount((ResInfo.RESOURCE_TYPE)input.ResType, input.ResRemain);
        
        // 修改玩家身上的资源数据
        PlayerInfo pi = GetPlayerInRoom(args);
        if (pi == null || pi.Enter.TokenId != input.OwnerId)
        {
            ServerRoomManager.Instance.Log($"RoomLogic OnHarvestStop Error - player not found!{input.OwnerId}");
            return;
        }
        
        switch ((ResInfo.RESOURCE_TYPE)input.ResType)
        {
            case ResInfo.RESOURCE_TYPE.WOOD:
                pi.AddWood(input.ResHarvest);
                break;
            case ResInfo.RESOURCE_TYPE.FOOD:
                pi.AddFood(input.ResHarvest);
                break;
            case ResInfo.RESOURCE_TYPE.IRON:
                pi.AddIron(input.ResHarvest);
                break;
        }
        
        HarvestStopReply output = new HarvestStopReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            ActorId = input.ActorId,
            CellIndex = input.CellIndex,
            ResType = input.ResType,
            ResRemain = input.ResRemain,
            ResHarvest = input.ResHarvest,
            Ret = true,
        };
        BroadcastMsg(ROOM_REPLY.HarvestStopReply, output.ToByteArray());
        
        // 发送给客户端刷新玩家身上的资源数量
        UpdateResReply output2 = new UpdateResReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            Ret = true,
            Wood = pi.Wood,
            Food = pi.Food,
            Iron = pi.Iron,
        };
        ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.UpdateResReply, output2.ToByteArray());
        
        // 存盘,效率有点低,但是先这样了
        SaveRes();
    }

    private void OnUpdateRes(SocketAsyncEventArgs args, byte[] bytes)
    {
        UpdateRes input = UpdateRes.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        PlayerInfo pi = GetPlayerInRoom(args);
        if (pi == null || pi.Enter.TokenId != input.OwnerId)
        {
            ServerRoomManager.Instance.Log($"RoomLogic OnUpdateRes Error - player not found!{input.OwnerId}");
            return;
        }
        UpdateResReply output = new UpdateResReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            Ret = true,
            Wood = pi.Wood,
            Food = pi.Food,
            Iron = pi.Iron,
        };
        ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.UpdateResReply, output.ToByteArray());
    }

    private void OnUpdateActionPoint(SocketAsyncEventArgs args, byte[] bytes)
    {
        UpdateActionPoint input = UpdateActionPoint.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        PlayerInfo pi = GetPlayerInRoom(args);
        if (pi == null || pi.Enter.TokenId != input.OwnerId)
        {
            ServerRoomManager.Instance.Log($"RoomLogic OnUpdateActionPoint Error - player not found!{input.OwnerId}");
            return;
        }
        UpdateActionPointReply output = new UpdateActionPointReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            Ret = true,
            ActionPoint = pi.ActionPoint,
            ActionPointMax = pi.ActionPointMax,
        };
        ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.UpdateActionPointReply, output.ToByteArray());
    }
    
    #endregion
    
    #region 消息处理 - 战斗

    private void OnFightStart(SocketAsyncEventArgs args, byte[] bytes)
    {
        FightStart input = FightStart.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        var attacker = ActorManager.GetActor(input.ActorId);
        if (attacker == null)
        {
            FightStartReply output = new FightStartReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                Ret = false,
                ErrMsg = "Attacker not found!",
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.FightStartReply, output.ToByteArray());
            return;
        }
        
        if (attacker.AmmoBase <= 0)
        { // 3.1-弹药基数不足, 无法攻击
            attacker.AmmoBase = 0;
            FightStartReply output = new FightStartReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                Ret = false,
                ErrMsg = "弹药基数不足, 无法攻击!",
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.FightStartReply, output.ToByteArray());
            return;
        }

        {
            FightStartReply output = new FightStartReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                TargetId = input.TargetId,
                SkillId = input.SkillId,
                Ret = true,
            };
            // 广播
            BroadcastMsg(ROOM_REPLY.FightStartReply, output.ToByteArray());
        }
    }

    /// <summary>
    /// 注意,因为这条消息里有删除Actor的操作,所以不能放在ActorBehaviour里去响应,除非找到更好的方法.Nov.15.2019. Liu Gang.
    /// </summary>
    /// <param name="args"></param>
    /// <param name="bytes"></param>
    private void OnFightStop(SocketAsyncEventArgs args, byte[] bytes)
    {
        FightStop input = FightStop.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        
        /////////////////
        // 1-攻击者
        var attacker = ActorManager.GetActor(input.ActorId);
        if (attacker == null)
        {
            FightStopReply output = new FightStopReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                Ret = false,
                ErrMsg = "Attacker not found!",
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.FightStopReply, output.ToByteArray());
            return;
        }
        
        // 2-防御者
        var defender = ActorManager.GetActor(input.TargetId);
        if (defender == null)
        {
            FightStopReply output = new FightStopReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                Ret = false,
                ErrMsg = "Defender not found!",
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.FightStopReply, output.ToByteArray());
            return;
        }
        
        // 3-计算弹药基数
        bool isFightAgain = false;
        if (attacker.AmmoBase <= 0)
        { // 3.1-弹药基数不足, 无法攻击
            attacker.AmmoBase = 0;
            FightStopReply output = new FightStopReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                Ret = false,
                ErrMsg = "弹药基数不足, 无法攻击!",
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.FightStopReply, output.ToByteArray());
            return;
        }
        else
        {
            attacker.AmmoBase--;
            if (attacker.AmmoBase > 0)
                isFightAgain = true;
        }
        
        // 4-战斗计算 - 减法公式
        int damage = (int)Mathf.CeilToInt(attacker.AttackPower - defender.DefencePower);
        if (damage == 0)
            damage = 1;
        defender.Hp = defender.Hp - damage;
        
        
        // 5-如果已经死亡
        bool isEnemyDead = false;
        if (defender.Hp <= 0)
        {
            defender.Hp = 0;
            isEnemyDead = true;
            isFightAgain = false;
        }

        /////////////////
        {// 10-血量, 群发, 挨打者
            UpdateActorInfoReply output = new UpdateActorInfoReply()
            {
                RoomId = defender.RoomId,
                OwnerId = defender.OwnerId,
                ActorId = defender.ActorId,
                Hp = defender.Hp,
                AmmoBase = defender.AmmoBase,
                Ret = true,
            };
            BroadcastMsg(ROOM_REPLY.UpdateActorInfoReply, output.ToByteArray());
        }
        {// 11-弹药基数, 群发, 攻击者
            UpdateActorInfoReply output = new UpdateActorInfoReply()
            {
                RoomId = attacker.RoomId,
                OwnerId = attacker.OwnerId,
                ActorId = attacker.ActorId,
                Hp = attacker.Hp,
                AmmoBase = attacker.AmmoBase,
                Ret = true,
            };
            BroadcastMsg(ROOM_REPLY.UpdateActorInfoReply, output.ToByteArray());
        }
        {// 12-飙血, 群发
            SprayBloodReply output = new SprayBloodReply()
            {
                RoomId = defender.RoomId,
                OwnerId = defender.OwnerId,
                ActorId = defender.ActorId,
                Damage = damage,
                Ret = true,
            };
            BroadcastMsg(ROOM_REPLY.SprayBloodReply, output.ToByteArray());
            ServerRoomManager.Instance.Log($"飙血 - {defender.ActorId}");
        }
        
        /////////////////
        {// 20-本次攻击结束
            FightStopReply output = new FightStopReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                TargetId = input.TargetId,
                IsEnemyDead = isEnemyDead,
                FightAgain = isFightAgain,
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.FightStopReply, output.ToByteArray());
        }

        if(isEnemyDead)
        {// 21-挨打者死了, 发送删除单位的消息
            ActorRemoveReply output = new ActorRemoveReply()
            {
                RoomId = defender.RoomId,
                OwnerId = defender.OwnerId,
                ActorId = defender.ActorId,
                DieType = 1,
                Ret = true,
            };
            ActorManager.RemoveActor(defender.ActorId);
            BroadcastMsg(ROOM_REPLY.ActorRemoveReply, output.ToByteArray());
        }
    }
    
    #endregion
    
}
