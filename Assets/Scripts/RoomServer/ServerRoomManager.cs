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
    
    [Space(), Header("Debug"), Space(5)]
    public bool IsCheckHeartBeat;
    
    private const float _heartBeatTimeInterval = 20f; // 心跳时间间隔,服务器检测用的间隔比客户端实际间隔要多一些

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
        RoomMsgReply.Init();
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
    }
    
    #endregion
    
    #region 检测心跳

    private void StartCheckHeartBeat()
    {
        InvokeRepeating(nameof(CheckHeartBeat), _heartBeatTimeInterval, _heartBeatTimeInterval);
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
            var ts = now - keyValue.Value.HeartBeatTime;
            if (ts.TotalSeconds > _heartBeatTimeInterval)
            { // 改客户端超时没有心跳了,干掉
                delPlayerList.Add(keyValue.Key);
            }
        }
        foreach (var args in delPlayerList)
        {
            _server.CloseASocket(args);
        }
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
            Log($"MSG: DropAClient - 玩家离开房间服务器 - {Players[args].Enter.Account} - PlayerCount:{Players.Count-1}/{_server.MaxClientCount}");
            RemovePlayer(args, true);
        }
        else
        {
            Log("MSG: DropAClient - Remove Player failed - Player not found!");
        }
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
                // 存盘这个事情先放一放，因为：
                // 1，服务器目前还没有全部的地图数据
                // 2，里面的Actor我想单独写地方来保存，而不是用它原有的结构。因为未来游戏主要会在这里进行拓展
                // Oct.24.2019. Liu Gang. 
//                        int size = 256 * 1024;
//                        byte[] totalMapData = new byte[size];
//                        roomLogic._hexmapHelper.Save(ref totalMapData, ref size);
//                        // 存盘
//                        string tableName = $"MAP:{roomLogic.RoomId}";
//                        RoomManager.Instance.Redis.CSRedis.HSet(tableName, "Creator", roomLogic.Creator);
//                        RoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomId", roomLogic.RoomId);
//                        RoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomName", roomLogic.RoomName);
//                        RoomManager.Instance.Redis.CSRedis.HSet(tableName, "MaxPlayerCount", roomLogic.MaxPlayerCount);
//                        RoomManager.Instance.Redis.CSRedis.HSet(tableName, "MapData", totalMapData);
//                        
//                        RoomManager.Instance.Log($"MSG: LEAVE_ROOM - 存盘成功！房间名:{roomLogic.RoomName} - RoomId:{roomLogic.RoomId}");
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

        return null;
    }

    public void SetPlayerInfo(SocketAsyncEventArgs args, PlayerInfo playerInfo)
    {
        Players[args] = playerInfo;
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
            Rooms[roomId].RemovePlayer(args);
        }
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
