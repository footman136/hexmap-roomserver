using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;
using Protobuf.Room;

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance { get; private set; }
    public ServerScript _server;
    public RedisManager _redis;
    public RedisManager Redis => _redis;
    
    private string receive_str;

    // 玩家的集合，Key是玩家的TokenId，因为真正的账号系统我们不一定能够获得玩家的账号名
    public Dictionary<SocketAsyncEventArgs, PlayerInfo> Players { set; get; }
    
    // 房间的集合，Key是房间的唯一ID
    public Dictionary<long, RoomLogic> Rooms { set; get; }

    public string ServerName;
    public long ServerId;
    public int MaxRoomCount;
    public int CurRoomCount;
    public int MaxPlayerPerRoom;

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
        _server.Log("RoomManager Started!");

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
        ServerId = GuidToLongId(); // 生成唯一ID
        MaxRoomCount = 5;
        CurRoomCount = 0;
        MaxPlayerPerRoom = 30;
       
        receive_str = $"Server started! {_server.Address}:{_server.Port}";
        // RoomServer已经启动成功，开始监听了，进入Connecting阶段，开始连接大厅服务器
        ClientManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTING);
    }

    /// <summary>  
    /// 根据GUID获取19位的唯一数字序列  
    /// </summary>  
    /// <returns></returns>  
    public static long GuidToLongId()
    {
        byte[] buffer = Guid.NewGuid().ToByteArray();
        return BitConverter.ToInt64(buffer, 0);
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
    
    #endregion
    
    void OnReceive(SocketAsyncEventArgs args, byte[] content, int size)
    {
        receive_str = System.Text.Encoding.UTF8.GetString(content);
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
            Log($"MSG: 玩家离开房间服务器 - {Players[args].Enter.Account} - PlayerCount:{Players.Count-1}/{_server.MaxClientCount}");
            RemovePlayerFromRoom(args);
            Players.Remove(args);
        }
        else
        {
            Log("MSG: RoomServer - Reomve Player failed - Player not found!");
        }
    }

    public void UpdateName()
    {
        transform.name = $"RoomManager - ({transform.childCount})";
    }

    public void AddPlayer(SocketAsyncEventArgs args, PlayerInfo pi)
    {
        RoomManager.Instance.Players[args] = pi;
    }
    public void RemovePlayer(SocketAsyncEventArgs args)
    {
        PlayerInfo pi = GetPlayer(args);
        if (pi != null)
        {
            RemovePlayerFromRoom(args);
            Players.Remove(args);
        }
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
}
