using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using GameUtils;
using Google.Protobuf;
using Protobuf.Room;
using UnityEngine;
using Actor;
using AI;
using UnityEngine.Assertions.Must;

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
    

    private readonly Dictionary<long, PlayerInfoInRoom> PlayersInRoom = new Dictionary<long, PlayerInfoInRoom>();

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

        Load();
        
        AddListener();
        
        DateTime nowTime = DateTime.Now;
        ServerRoomManager.Instance.Log($"RoomLogic Init - Battle-room Opened - {_roomName}.{nowTime.ToShortDateString()} {nowTime.ToShortTimeString()}");
    }

    public void Fini()
    {
        RemoveListener();

        Save();
        
        DateTime nowTime = DateTime.Now;
        ServerRoomManager.Instance.Log($"RoomLogic Fini - Battle-room Closed - {_roomName}.{nowTime.ToShortDateString()} {nowTime.ToShortTimeString()}");
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

    private const float _TIME_INTERVAL_SAVE = 600f; // 每隔5分钟存盘一次
    private float _timeNow;
    public void Tick()
    {
        if (_timeNow >= _TIME_INTERVAL_SAVE)
        {
            _timeNow = 0;
            Save();
            return;
        }

        _timeNow += Time.deltaTime;
        
        // [定时恢复行动点] (会不会有多线程问题? 这里是主线程运行的)
        foreach (var keyValue in PlayersInRoom)
        {
            var pi = keyValue.Value;
            pi?.Tick();
        }

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

    private void Load()
    {
        // 取盘
        LoadCity();
        LoadActor();
        LoadRes();
        LoadAllPlayers();
    }

    private void Save()
    {
        SaveCity();
        SaveActor();
        SaveRes();
        SaveAllPlayers();
        DateTime nowTime = DateTime.Now;
        ServerRoomManager.Instance.Log($"RoomLogic - Game saved. {nowTime.ToShortDateString()} {nowTime.ToShortTimeString()}");
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
            ServerRoomManager.Instance.Log($"RoomLogic LoadRes Error - Resource Data not found in Redis! It is not an error if it is a new battlefiled! - Key:{keyName}"); //  如果是新战场则不是错误! 
            return;
        }

        ServerRoomManager.Instance.Log(!ResManager.LoadBuffer(resBytes, resBytes.Length)
            ? "RoomLogic LoadRes Error - Resource LoadBuffer Failed!"
            : $"RoomLogic LoadRes OK - Count of Res ：{ResManager.AllRes.Count}"); // 资源个数 
    }

    /// <summary>
    /// 保存该玩家的城市数据
    /// </summary>
    private void SaveCity()
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
            ServerRoomManager.Instance.Log($"RoomLogic LoadCity Error - City Data not found in Redis! It is not an error if it is a new battlefiled! - Key:{keyName}");//  如果是新战场则不是错误!
            return;
        }

        ServerRoomManager.Instance.Log(!UrbanManager.LoadBuffer(cityBytes, cityBytes.Length)
            ? "RoomLogic LoadCity Error - City LoadBuffer Failed!"
            : $"RoomLogic LoadCity OK - Count of Cites ：{UrbanManager.AllCities.Count}"); // 城市个数
    }

    /// <summary>
    /// 保存该玩家的单元数据
    /// </summary>
    private void SaveActor()
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
            ServerRoomManager.Instance.Log($"RoomLogic LoadActor Error - Actor Data not found in Redis! It is not an error if it is a new battlefiled! - Key:{keyName}");//  如果是新战场则不是错误!
            return;
        }

        ServerRoomManager.Instance.Log(!ActorManager.LoadBuffer(actorBytes, actorBytes.Length, this)
            ? "RoomLogic LoadActor Error - Actor LoadBuffer Failed!"
            : $"RoomLogic LoadActor OK - Count of Actors ：{ActorManager.AllActors.Count}"); // 单元个数
    }

    private void SaveAllPlayers()
    {
        foreach (var keyValue in PlayersInRoom)
        {
            var piir = keyValue.Value;
            SavePlayer(piir);
            SavePlayerDebugInfo(piir);
        }
    }

    private void LoadAllPlayers()
    {
        // 1-从redis里把本房间里所有的玩家的id都读出来
        List<long> playerIdList = LoadAllPlayerIdsInRoom();
        // 2-创建实例, 然后把每个玩家在redis的存盘都读出来
        for (int i = 0; i < playerIdList.Count; ++i)
        {
            PlayerInfoInRoom piir = new PlayerInfoInRoom();
            if (piir != null)
            {
                piir.Enter.TokenId = playerIdList[i];
            }

            if (LoadPlayer(piir))
            { // 可能存在没有存盘的情况, 比如玩家刚刚进入游戏, 而服务器还没有存盘, 
                PlayersInRoom[playerIdList[i]] = piir;
                piir.Offline(); // 这时候没人在线
            }
        }
    }

    public void SavePlayer(PlayerInfoInRoom piirir)
    {
        if (piirir == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic SavePlayer Error - Player not found!");
            return;
        }

        byte[] playerBytes = piirir.SaveBuffer();
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Player-{piirir.Enter.TokenId}";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, playerBytes );
    
        ServerRoomManager.Instance.Log($"RoomLogic SavePlayer OK - Player:{piirir.Enter.Account}");
    }

    public bool LoadPlayer(PlayerInfoInRoom piir)
    {
        if (piir == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic LoadPlayer Error - Player not found!");
            return false;
        }
        
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Player-{piir.Enter.TokenId}";
        byte[] playerBytes = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        if (playerBytes == null)
        {
            ServerRoomManager.Instance.Log($"RoomLogic LoadPlayer Error - Player Data not found in Redist!It is not an error if it is a new battlefiled! - Player:{piir.Enter.Account} - Key:{keyName}");//  如果是新战场则不是错误!
            return false;
        }

        bool ret = piir.LoadBuffer(playerBytes, playerBytes.Length);
        string msg;
        if (ret)
        {
            msg = $"RoomLogic LoadPlayer OK - Player:{piir.Enter.Account}";
        }
        else
        {
            msg = $"RoomLogic LoadPlayer Error - Player LoadBuffer Failed! - Player:{piir.Enter.Account}";
        }

        ServerRoomManager.Instance.Log(msg);
        return true;
    }

    /// <summary>
    /// 保存基础信息, 仅供查看, 如果不想看的话, 根本不用保存
    /// </summary>
    /// <param name="piir"></param>
    public void SavePlayerDebugInfo(PlayerInfoInRoom piir)
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"Infos";
        string info = $"Total City Count:{UrbanManager.AllCities.Count} | Total Actor Count:{ActorManager.AllActors.Count} | Total Res Count:{ResManager.AllRes.Count}";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, info);
        
        // 本玩家身上的物体的数量
        if (piir == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic SaveCommonInfo Error - player not found!");
            return;
        }

        long ownerId = piir.Enter.TokenId;
        keyName = $"Infos-{ownerId}";
        info = $"City Count:{UrbanManager.CountOfThePlayer(ownerId)}/{UrbanManager.AllCities.Count} | Actor Count:{ActorManager.CountOfThePlayer(ownerId)}/{ActorManager.AllActors.Count} | Res Count:{ResManager.AllRes.Count}";
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, info);
    }

    public void AddPlayerIdToRedis(PlayerInfoInRoom piir)
    {
        if (piir == null)
        {
            ServerRoomManager.Instance.Log("RoomLogic AddPlayerIdToRedis Error - Player not found!");
            return;
        }
        
        // 1-把玩家列表从redis里读出来
        List<long> playerIdList = LoadAllPlayerIdsInRoom();
        if (playerIdList.Count == 0)
        { // 调试用
            long[] Ids = { -1858895091378629347, -5280871521389498391, -8236625811434607887, 2448695053450986095, 512909072226182911, 9095304521408418377};
            playerIdList = Ids.ToList();
        }
        // 2-添加自己
        if (!playerIdList.Contains(piir.Enter.TokenId))
        {
            playerIdList.Add(piir.Enter.TokenId);
        }

        // 3-保存回redis
        SaveAllPlayerIdsInRoom(playerIdList);
    }
    
    private List<long> LoadAllPlayerIdsInRoom()
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"AllPlayerIdsInRoom";
        byte[] readPlayersInRoom = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, keyName);
        List<long> playerIdList = new List<long>();
        if (readPlayersInRoom != null)
        {
            MemoryStream ms = new MemoryStream(readPlayersInRoom);
            BinaryReader br = new BinaryReader(ms);
            int count = br.ReadInt32();
            for (int i = 0; i < count; ++i)
            {
                long playerId = br.ReadInt64();
                playerIdList.Add(playerId);
            }
        }

        return playerIdList;
    }

    private void SaveAllPlayerIdsInRoom(List<long> playerIdList)
    {
        string tableName = $"MAP:{RoomId}";
        string keyName = $"AllPlayerIdsInRoom";
        MemoryStream ms = new MemoryStream();
        BinaryWriter bw = new BinaryWriter(ms);
        bw.Write(playerIdList.Count);
        for (int i = 0; i < playerIdList.Count; ++i)
        {
            bw.Write(playerIdList[i]);
        }
        
        ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, keyName, ms.GetBuffer());
    }

    #endregion
    
    #region 玩家
    
    public void Online(SocketAsyncEventArgs args, PlayerEnter enter, long roomId)
    {
        var piir = GetPlayerInRoom(enter.TokenId);
        if (piir == null)
        { // 如果房间里实际没有这个玩家(的存盘), 则表明这是一个新用户, 要从表格中读取初始数据
            piir = new PlayerInfoInRoom();
            var csv = CsvDataManager.Instance.GetTable("battle_init");
            int wood = csv.GetValueInt(1, "PlayerInit_Wood");
            int food = csv.GetValueInt(1, "PlayerInit_Food");
            int iron = csv.GetValueInt(1, "PlayerInit_Iron");
            int actionPointMax = csv.GetValueInt(1, "MaxActionPoint");
            piir.AddWood(wood);
            piir.AddFood(food);
            piir.AddIron(iron);
            piir.SetActionPointMax(actionPointMax);
            piir.AddActionPoint(actionPointMax);
            piir.TimeSinceLastSave = DateTime.Now.ToFileTime();
            piir.TimeSinceLastRestoreActionPoint = 0;
            PlayersInRoom[enter.TokenId] = piir;
        }
        // 把本玩家记录到Room的Redis里, 便于以后查找, 这里保存的就是房间内所有的玩家的id
        AddPlayerIdToRedis(piir);
        
        // 玩家上线
        piir.Online(args, enter, roomId);

        // 玩家进入以后,根据该玩家[离开游戏]的时间,到[现在]的时间差(秒),计算出应该恢复多少的行动点数, 一次性恢复之
        piir.RestoreActionPointAfterLoading();
        
        // 这段逻辑很重要: 这是要把所有房间内的玩家的代理权限交给第一个进入房间的玩家, 以后, 如果这些玩家也上线了, 则把代理权交还给真正的玩家
        // 这是因为, 服务器并没有对这些玩家的逻辑进行AI处理, 而是由客户端自己来控制自己的.
        // 未来, 如果服务器要把AI代理权限收回来,这些地方都要做修改.
        if(piir.AiRights == 0 || piir.AiRights != piir.Enter.TokenId)
        { // 我之前曾经被别人代理过, 把代理权收回来
            var piirAi = GetPlayerInRoom(piir.AiRights);
            if (piirAi != null && piirAi.IsOnline)
            {
                piir.AiRights = piir.Enter.TokenId;
                ChangeAiRightsReply output = new ChangeAiRightsReply()
                {
                    RoomId = piirAi.RoomId,
                    OwnerId = piirAi.Enter.TokenId,
                    AiActorId = piir.Enter.TokenId,
                    AiAccount = piir.Enter.Account,
                    ControlByMe = false,
                    Ret = true,
                };
                ServerRoomManager.Instance.SendMsg(piirAi.Args, ROOM_REPLY.ChangeAiRightsReply, output.ToByteArray());
                ServerRoomManager.Instance.Log($"RoomLogic Online OK - AI Rights of {piir.Enter.TokenId} is no longer controlled by {piir.Enter.Account}");
            }
            else
            {
                ServerRoomManager.Instance.Log($"RoomLogic Online Error - When trying to handle the AI controlled rights, the original player is offline! {piir.Enter.Account} - {piir.Enter.Account}");
            }
        }
        
        // 遍历所有玩家, 把所有: AI没有被代理, 且不在线的玩家, 权限都给它. 这种情况仅发生在第一个进入的玩家
        foreach (var keyValue in PlayersInRoom)
        {
            var piirAi = keyValue.Value;
            if (piirAi.AiRights == 0)
            { // 我自己不需要发送此消息, 我自己的AiRights此时肯定不是0了
                piirAi.AiRights = piir.Enter.TokenId;
                ChangeAiRightsReply output = new ChangeAiRightsReply()
                {
                    RoomId = piir.RoomId,
                    OwnerId = piir.Enter.TokenId,
                    AiActorId = piirAi.Enter.TokenId,
                    AiAccount = piirAi.Enter.Account,
                    ControlByMe = true,
                    Ret = true,
                };
                ServerRoomManager.Instance.SendMsg(piir.Args, ROOM_REPLY.ChangeAiRightsReply, output.ToByteArray());
                ServerRoomManager.Instance.Log($"RoomLogic Online OK - AI Rights of {piirAi.Enter.Account} belongs to {piir.Enter.Account} now!" );
            }
        }

        _curPlayerCount = PlayersInRoom.Count;
        ServerRoomManager.Instance.Log($"RoomLogic Online OK - Player entered the battlefield! Player:{piir.Enter.Account}"); // 玩家进入战场
        
    }

    public void Offline(long playerId)
    {
        var piir = GetPlayerInRoom(playerId);
        if (piir == null)
        {
            ServerRoomManager.Instance.Log($"RoomLogic Offline - Player not found!");
            return;
        }
        // 玩家下线
        piir.Offline();
        
        // 把本玩家的代理权交给别的在线玩家, 除非我已经是最后一个了, 就不做任何处理了
        // 找到一个仍然在线的玩家
        PlayerInfoInRoom piirOnline = null;
        foreach (var keyValue in PlayersInRoom)
        {
            var piirOther = keyValue.Value;
            if (piirOther.IsOnline)
            {
                piirOnline = piirOther;
            }
        }
        if(piirOnline != null)
        { // 不仅是自己, 而且是把所有自己管理的AiRights, 都交给对方 
            foreach (var keyValue in PlayersInRoom)
            {
                var piirAi = keyValue.Value;
                if(piirAi.AiRights == piir.Enter.TokenId)
                {
                    piirAi.AiRights = piirOnline.Enter.TokenId;
                    ChangeAiRightsReply output = new ChangeAiRightsReply()
                    {
                        RoomId = piirOnline.RoomId,
                        OwnerId = piirOnline.Enter.TokenId,
                        AiActorId = piirAi.Enter.TokenId,
                        AiAccount = piirAi.Enter.Account,
                        ControlByMe = true,
                        Ret = true,
                    };
                    ServerRoomManager.Instance.SendMsg(piirOnline.Args, ROOM_REPLY.ChangeAiRightsReply,
                        output.ToByteArray());
                    ServerRoomManager.Instance.Log($"RoomLogic Offline OK - AI Rights of {piirAi.Enter.Account} belongs to {piirOnline.Enter.Account}");
                }
            }
        }
        else
        { // 如果我是最后一个玩家, 则把所有人的 AI Rights 改为0
            foreach (var keyValue in PlayersInRoom)
            {
                var piirAi = keyValue.Value;
                if (piirAi.AiRights != piir.Enter.TokenId)
                {
                    ServerRoomManager.Instance.Log($"RoomLogic Offline Error - I am the last one in the room. But the AI Rights of {piirAi.Enter.Account} belongs to someone else {piirAi.AiRights} - MyName:{piir.Enter.Account}");
                }
                piirAi.AiRights = 0;
                ServerRoomManager.Instance.Log($"RoomLogic Offline OK - AI Rights of {piirAi.Enter.Account} belongs to nobody! There is no player in the room.");
            }
        }

        _curPlayerCount = PlayersInRoom.Count;
        ServerRoomManager.Instance.Log($"RoomLogic Offline OK - Player left the battlefield! Player:{piir.Enter.Account}"); // 玩家离开战场
    }

    public bool IsOnline(long playerId)
    {
        var piir = GetPlayerInRoom(playerId);
        if (piir != null)
        {
            return piir.IsOnline;
        }

        return false;
    }

    /// <summary>
    /// 注意: 这里有两个版本, 这个版本快一点
    /// </summary>
    /// <param name="playerId"></param>
    /// <returns></returns>
    public PlayerInfoInRoom GetPlayerInRoom(long playerId)
    {
        if (PlayersInRoom.ContainsKey(playerId))
        {
            return PlayersInRoom[playerId];
        }

        return null;
    }

    /// <summary>
    /// 注意: 这里有两个版本, 这个版本慢一点
    /// </summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public PlayerInfoInRoom GetPlayerInRoom(SocketAsyncEventArgs args)
    {
        PlayerInfo pi = ServerRoomManager.Instance.GetPlayer(args);
        if (pi != null)
        {
            return GetPlayerInRoom(pi.Enter.TokenId);
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
            if (keyPair.Value.IsOnline)
            {
                ServerRoomManager.Instance.SendMsg(keyPair.Value.Args, msgId, output);
            }
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
        
        bool isCapiirtal = UrbanManager.CountOfThePlayer(input.OwnerId) == 0; // 第一座城市是都城
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
            IsCapital = isCapiirtal,
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
                IsCapital = isCapiirtal,
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

        var piir = GetPlayerInRoom(args);

        if (piir == null)
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
        PlayerInfoInRoom piir = GetPlayerInRoom(args);
        if (piir == null || piir.Enter.TokenId != input.OwnerId)
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

            if (piir.ActionPoint < input.ActionPointCost)
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
            piir.AddActionPoint(-input.ActionPointCost);
        }

        {
            // 行动点发生变化,要通知客户端
            UpdateActionPointReply output = new UpdateActionPointReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActionPoint = piir.ActionPoint,
                ActionPointMax = piir.ActionPointMax,
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
            ServerRoomManager.Instance.Log($"RoomLogic OnTryCommand OK - Permission granted! - CommandId:{input.CommandId} - ActionPointCost:{input.ActionPointCost}");
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
        PlayerInfoInRoom piir = GetPlayerInRoom(args);
        if (piir == null || piir.Enter.TokenId != input.OwnerId)
        {
            ServerRoomManager.Instance.Log($"RoomLogic OnHarvestStop Error - player not found!{input.OwnerId}");
            return;
        }
        
        switch ((ResInfo.RESOURCE_TYPE)input.ResType)
        {
            case ResInfo.RESOURCE_TYPE.WOOD:
                piir.AddWood(input.ResHarvest);
                break;
            case ResInfo.RESOURCE_TYPE.FOOD:
                piir.AddFood(input.ResHarvest);
                break;
            case ResInfo.RESOURCE_TYPE.IRON:
                piir.AddIron(input.ResHarvest);
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
            Wood = piir.Wood,
            Food = piir.Food,
            Iron = piir.Iron,
        };
        ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.UpdateResReply, output2.ToByteArray());
    }

    private void OnUpdateRes(SocketAsyncEventArgs args, byte[] bytes)
    {
        UpdateRes input = UpdateRes.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        PlayerInfoInRoom piir = GetPlayerInRoom(args);
        if (piir == null || piir.Enter.TokenId != input.OwnerId)
        {
            ServerRoomManager.Instance.Log($"RoomLogic OnUpdateRes Error - player not found!{input.OwnerId}");
            return;
        }
        UpdateResReply output = new UpdateResReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            Ret = true,
            Wood = piir.Wood,
            Food = piir.Food,
            Iron = piir.Iron,
        };
        ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.UpdateResReply, output.ToByteArray());
    }

    private void OnUpdateActionPoint(SocketAsyncEventArgs args, byte[] bytes)
    {
        UpdateActionPoint input = UpdateActionPoint.Parser.ParseFrom(bytes);
        if (input.RoomId != RoomId)
            return; // 不是自己房间的消息，略过
        PlayerInfoInRoom piir = GetPlayerInRoom(args);
        if (piir == null || piir.Enter.TokenId != input.OwnerId)
        {
            ServerRoomManager.Instance.Log($"RoomLogic OnUpdateActionPoint Error - player not found!{input.OwnerId}");
            return;
        }
        UpdateActionPointReply output = new UpdateActionPointReply()
        {
            RoomId = input.RoomId,
            OwnerId = input.OwnerId,
            Ret = true,
            ActionPoint = piir.ActionPoint,
            ActionPointMax = piir.ActionPointMax,
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
            // 弹药充足, 并且这不是反击的情况下, 允许攻击下一轮
            if (attacker.AmmoBase > 0 && !input.IsCounterAttack)
                isFightAgain = true;
        }
        
        // 4-战斗计算 - 减法公式
        int damage = (int)Mathf.FloorToInt(attacker.AttackPower - defender.DefencePower);
        if (input.IsCounterAttack) // 如果是反击,则仅计算60%的伤害
            damage = Mathf.FloorToInt(damage * 0.6f);
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
