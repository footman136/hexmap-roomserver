using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;
using Protobuf.Room;
using Actor;
using Google.Protobuf;

public class ServerRoomManager : MonoBehaviour
{
    public static ServerRoomManager Instance { get; private set; }
    [Header("Server Attributes"), Space(5)]
    public ServerScript _server;
    public RedisManager _redis;
    public RedisManager Redis => _redis;
    
    private string receive_str;
    
    // 玩家的集合，Key是玩家的TokenId，因为真正的账号系统我们不一定能够获得玩家的账号名
    public Dictionary<SocketAsyncEventArgs, PlayerInfo> Players { set; get; }
    
    // 房间的集合，Key是房间的唯一ID
    private Dictionary<long, RoomLogic> Rooms { set; get; }

    [Header("Basic Attributes"), Space(5)]
    public string ServerName;
    public long ServerId;
    public int MaxRoomCount;
    public int CurRoomCount;
    public int MaxPlayerPerRoom;
    public int MaxActionPoint;
    
    [Space(), Header("Debug"), Space(5)]
    public bool IsCheckHeartBeat;
    
    private const float _HEART_BEAT_INTERVAL = 20f; // 心跳时间间隔,服务器检测用的间隔比客户端实际间隔要多一些
    
    //行动点
    private const int _ACTION_POINT_INTERVAL = 60; // 恢复行动点的时间间隔
    private const int _ACTION_POINT_ADD = 1; // 每次恢复几点行动点


    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("RoomManager is Singleton! Cannot be created again!");
        }

        Instance = this;
        Players = new Dictionary<SocketAsyncEventArgs, PlayerInfo>();
        Rooms = new Dictionary<long, RoomLogic>();
        _server = GetComponent<ServerScript>();
        _redis = GetComponent<RedisManager>();
    }

    #region 初始化
    // Start is called before the first frame update
    void Start()
    {
        _server.Received += OnReceive;
        _server.Completed += OnComplete;

        StartCoroutine(WaitForReady());
    }

    private void OnDestroy()
    {
        _server.Received -= OnReceive;
        _server.Completed -= OnComplete;
    }

    IEnumerator WaitForReady()
    {
        while (!_server.IsReady)
        {
            yield return null;
        }
        
        ServerName = $"{_server.Address}:{_server.Port}";
        ServerId = GameUtils.Utils.GuidToLongId(); // 生成唯一ID
        MaxRoomCount = 5;
        CurRoomCount = 0;
        MaxPlayerPerRoom = 30;
       
        receive_str = $"Server started! {_server.Address}:{_server.Port}";
        // RoomServer已经启动成功，开始监听了，进入Connecting阶段，开始连接大厅服务器
        MixedManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTING);
        if (IsCheckHeartBeat)
        {
            StartCheckHeartBeat(); //监听心跳
        }
    }

    void OnGUI()
    {
        if (receive_str != null)
        {
            var style = GUILayout.Width(600) ;
            GUILayout.Label (receive_str, style);
            GUILayoutOption[] style2 = new GUILayoutOption[2] {style, GUILayout.Height(60)};
            string msg = $"----Player Count:{Players.Count}/{_server.MaxClientCount} - Room Count:{Rooms.Count}/{MaxRoomCount} - MaxPlayerPerRoom:{MaxPlayerPerRoom}";
            GUILayout.Label (msg, style2);
        }
    }

    public void Log(string msg)
    {
        receive_str = msg;
        _server.Log(msg);
    }

    void Update()
    {
        UpdateName();
        
        // [定时恢复行动点] (会不会有多线程问题? 这里是主线程运行的)
        foreach (var keyValue in Players)
        {
            var pi = keyValue.Value;
            pi?.Tick();
        }
    }
    
    #endregion
    
    #region 检测心跳

    private void StartCheckHeartBeat()
    {
        InvokeRepeating(nameof(CheckHeartBeat), _HEART_BEAT_INTERVAL, _HEART_BEAT_INTERVAL);
    }

    private void StopCheckHeartBeat()
    {
        CancelInvoke(nameof(CheckHeartBeat));
    }

    private void CheckHeartBeat()
    {
        var now = DateTime.Now;
        List<SocketAsyncEventArgs> delPlayerList = new List<SocketAsyncEventArgs>();
        foreach (var keyValue in Players)
        {
            var pi = keyValue.Value;
            var ts = now - pi.HeartBeatTime;
            if (pi.IsReady && ts.TotalSeconds > _HEART_BEAT_INTERVAL)
            { // 该客户端超时没有心跳了,干掉. 客户端必须是已经进入房间的,因为loading时间较长
                delPlayerList.Add(keyValue.Key);
                _server.Log($"长时间没有检测到心跳,将客户端踢出! - {pi.Enter.Account}");
            }
        }
        foreach (var args in delPlayerList)
        {
            _server.CloseASocket(args);
        }
    }
    
    #endregion
    
    #region 恢复行动点

    private IEnumerator RestoreActionPointOfPlayer(SocketAsyncEventArgs args)
    {
        // 第一次等待的时间,是上次[恢复行动点数]到下次[恢复行动点数]的剩余时间
        var pi = GetPlayer(args);
        if (pi == null) yield break;
        float timeRemain = _ACTION_POINT_INTERVAL - pi.TimeSinceLastRestoreActionPoint;
        if ( timeRemain < 0)
        {
            Debug.LogError($"DEBUG - ServerRoomManager UpdateActionPointOfPlayer Error - [{pi.Enter.Account}] 时间计算错误!! - 上次经过时间:{pi.TimeSinceLastRestoreActionPoint}");
        }
        yield return new WaitForSeconds(timeRemain);
        while (true)
        {
            pi = GetPlayer(args);
            if (pi == null) yield break;
            addActionPointofPlayerOneTime(pi, _ACTION_POINT_ADD);
            // 以后每次的间隔,都是固定时间
            yield return new WaitForSeconds(_ACTION_POINT_INTERVAL);            
        }
    }

    private void addActionPointofPlayerOneTime(PlayerInfo pi, int points)
    {
        pi.AddActionPoint(points);
        UpdateActionPointReply output = new UpdateActionPointReply()
        {
            RoomId = pi.RoomId,
            OwnerId = pi.Enter.TokenId,
            Ret = true,
            ActionPoint = pi.ActionPoint,
            ActionPointMax = pi.ActionPointMax,
        };
        SendMsg(pi.Args, ROOM_REPLY.UpdateActionPointReply, output.ToByteArray());
    }

    /// <summary>
    /// 根据玩家[离开游戏]的时间,到[现在]的时间差(秒), 计算出应该给这个玩家恢复多少行动点数
    /// </summary>
    /// <param name="pi">当前这个玩家</param>
    /// <param name="timeSpan">上次存盘到这次取盘之间经过的时间(秒)</param>
    public void RestoreActionPointAfterLoading(PlayerInfo pi)
    {
        // 计算[上次存盘]到[这次取盘]之间的时间差,还应该加上上次存盘时,距离上次恢复行动点的时间
        long timeNow = DateTime.Now.ToFileTime();
        long timeSpan = (timeNow - pi.TimeSinceLastSave) / 1000000;   // 单位秒
        timeSpan += pi.TimeSinceLastRestoreActionPoint;
        Debug.Log($"ServerRoomManager RestoreActionPoint - [{pi.Enter.Account}] 上次登录到现在的时间差<{timeSpan}>秒");
        int actionPointAddTimes = Mathf.FloorToInt(timeSpan / _ACTION_POINT_INTERVAL);
        if (actionPointAddTimes > 0)
        {
            pi.RestoreActionPoint(_ACTION_POINT_ADD*actionPointAddTimes);
        }
        // 距离上次[恢复行动点数]的时间差(秒), RestoreActionPointOfPlayer()的时候使用
        pi.TimeSinceLastRestoreActionPoint = Mathf.FloorToInt(timeSpan % _ACTION_POINT_INTERVAL);
        int timeRemain = _ACTION_POINT_INTERVAL - pi.TimeSinceLastRestoreActionPoint;
        Debug.Log($"ServerRoomManager RestoreActionPoint - [{pi.Enter.Account}] 第一次恢复行动点在<{timeRemain}>秒以后");
    }
    
    #endregion

    #region 收发消息
    
    void OnReceive(SocketAsyncEventArgs args, byte[] content, int size)
    {
        receive_str = System.Text.Encoding.UTF8.GetString(content, 0, 100);
        RoomMsgReply.ProcessMsg(args, content, size);
    }

    private void OnComplete(SocketAsyncEventArgs args, ServerSocketAction action)
    {
        switch (action)
        {
            case ServerSocketAction.Listen:
                // 因为启动顺序的关系，这段代码不会被执行到
                receive_str = $"RoomServer started! {_server.Address}:{_server.Port}";
                _server.Log(receive_str);
                break;
            case ServerSocketAction.Accept:
                receive_str = $"RoomServer accepted a client! Total Count :{_server.ClientCount}/{_server.MaxClientCount}";
                _server.Log(receive_str);
                break;
            case ServerSocketAction.Send:
            {
                int size = args.BytesTransferred;
                _server.Log($"RoomServer send a message. {size} bytes");
            }
                break;
            case ServerSocketAction.Receive:
            {
                int size = args.BytesTransferred;
                _server.Log($"RoomServer receive a message. {size} bytes");
            }
                break;
            case ServerSocketAction.Drop:
                DropAClient(args);
                receive_str = $"RoomServer drop a client! Total Count :{_server.ClientCount}/{_server.MaxClientCount}";
                _server.Log(receive_str);
                break;
            case ServerSocketAction.Close:
                StopCheckHeartBeat();
                receive_str = "RoomServer Stopped!";
                _server.Log(receive_str);
                break;
            case ServerSocketAction.Error:
                receive_str = System.Text.Encoding.UTF8.GetString(args.Buffer);
                Debug.LogError(receive_str);
                break;
        }
    }

    /// <summary>
    /// 新增的发送消息函数，增加了消息ID，会把前面的消息ID（4字节）和后面的消息内容组成一个包再发送
    /// </summary>
    /// <param name="msgId">消息ID，注意这是服务器返回给客户端的消息</param>
    /// <param name="???"></param>
    public void SendMsg(SocketAsyncEventArgs args, ROOM_REPLY msgId, byte[] data)
    {
        byte[] sendData = new byte[data.Length + 4];
        byte[] sendHeader = System.BitConverter.GetBytes((int)msgId);
        
        Array.Copy(sendHeader, 0, sendData, 0, 4);
        Array.Copy(data, 0, sendData, 4, data.Length);
        _server.SendMsg(args, sendData, sendData.Length);
    }

    public void DropAClient(SocketAsyncEventArgs args)
    {
        if (Players.ContainsKey(args))
        {
            var pi = GetPlayer(args);
            RoomLogic roomLogic = null;
            if (pi != null)
            {
                roomLogic = GetRoomLogic(pi.RoomId);
            }
            if (roomLogic != null)
            {
                // 通知大厅
                UpdateRoomInfoToLobby(roomLogic);
            }
            
            Log($"MSG: DropAClient - 玩家离开房间服务器 - {Players[args].Enter.Account} - PlayerCount:{Players.Count-1}/{_server.MaxClientCount}");
            RemovePlayer(args, true);
        }
        else
        {
            Log("MSG: DropAClient - Remove Player failed - Player not found!");
        }
    }

    public void UpdateRoomInfoToLobby(RoomLogic roomLogic)
    {
        // 通知大厅(往大厅发送消息)
        Protobuf.Lobby.UpdateRoomInfo output2 = new Protobuf.Lobby.UpdateRoomInfo()
        {
            RoomId = roomLogic.RoomId,
            RoomName = roomLogic.RoomName,
            Creator = roomLogic.Creator,
            CurPlayerCount    = roomLogic.CurPlayerCount,
            MaxPlayerCount = roomLogic.MaxPlayerCount,
            IsRunning = true,
            IsRemove = false,
        };
        MixedManager.Instance.LobbyManager.SendMsg(Protobuf.Lobby.LOBBY.UpdateRoomInfo, output2.ToByteArray());
    }

    public void UpdateName()
    {
        transform.name = $"RoomManager - ({transform.childCount})";
    }
    
    #endregion
    
    #region 玩家
    
    public bool AddPlayer(SocketAsyncEventArgs args, PlayerInfo pi)
    {
        foreach (var keyValue in Players)
        {
            var playerInfo = keyValue.Value;
            if (playerInfo.Enter.TokenId == pi.Enter.TokenId)
            {
                Log($"RoomManager AddPlayer Error - Duplicated player! Account:<{pi.Enter.Account}> - TokenId:<{pi.Enter.TokenId}>");
                return false;
            }
        }
        Players[args] = pi;
        return true;
    }
    public void RemovePlayer(SocketAsyncEventArgs args, bool bCloseRoomIfNoUser)
    {
        bool ret = false;
        PlayerInfo pi = GetPlayer(args);
        if (pi == null)
        {
            Log($"RoomManager RemovePlayer Error - Player not found!");
            return;
        }
        RoomLogic roomLogic = Rooms[pi.RoomId];
        // 存盘
        roomLogic.SaveCommonInfo(args);
        roomLogic.SaveActor();
        roomLogic.SaveCity();
        if (roomLogic.CurPlayerCount == 0 && bCloseRoomIfNoUser)
        { // 关闭房间
            roomLogic.Fini(); // 结束化
            Rooms.Remove(pi.RoomId);
            // 通知大厅：删除房间
            Protobuf.Lobby.UpdateRoomInfo output2 = new Protobuf.Lobby.UpdateRoomInfo()
            {
                RoomId = roomLogic.RoomId,
                IsRemove = true,
            };
            MixedManager.Instance.LobbyManager.SendMsg(Protobuf.Lobby.LOBBY.UpdateRoomInfo, output2.ToByteArray());
        }
        RemovePlayerFromRoom(args);
        Players.Remove(args);
    }

    public PlayerInfo GetPlayer(SocketAsyncEventArgs args)
    {
        if (Players.ContainsKey(args))
        {
            PlayerInfo pi = Players[args];
            return pi;
        }
        else
        {
            return null;
        }
    }

    public void SetPlayerInfo(SocketAsyncEventArgs args, PlayerInfo playerInfo)
    {
        if (Players.ContainsKey(args))
        {
            Players[args] = playerInfo;
        }
    }

    public void RemovePlayerFromRoom(SocketAsyncEventArgs args)
    {
        long roomId = -1;
        if (Players.ContainsKey(args))
        {
            roomId = Players[args].RoomId;
        }

        if (roomId != -1)
        {
            Rooms[roomId].RemovePlayerFromRoom(args);
        }
    }
    
    public SocketAsyncEventArgs FindPlayerArgs(long tokenId)
    {
        foreach (var keyValue in Players)
        {
            var pe = keyValue.Value;
            if (pe.Enter.TokenId == tokenId)
            {
                return keyValue.Key;
            }
        }

        return null;
    }

    
    #endregion
    
    #region 房间

    public void AddRoomLogic(long roomId, RoomLogic roomLogic)
    {
        Rooms.Add(roomId, roomLogic);
    }
    public RoomLogic GetRoomLogic(long roomId)
    {
        if (!Rooms.ContainsKey(roomId))
            return null;
        return Rooms[roomId];
    }
    #endregion
}
