using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// https://github.com/LitJSON/litjson
using LitJson;
using System;
using Google.Protobuf;

// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Lobby;

public class GameLobbyManager : ClientScript
{
    private const float _heartBeatInterval = 15f; // 心跳间隔(秒)
    
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("GameLobbyManager.Start()");
        base.Start();

        Completed += OnComplete;
        Received += OnReceiveMsg;
    }

    void OnDestroy()
    {
        Completed -= OnComplete;
        Received -= OnReceiveMsg;
    }

    // Update is called once per frame
    protected void Update()
    {
        base.Update();
    }

    #region 心跳
    
    private void StartHeartBeat()
    {
        InvokeRepeating(nameof(HeartBeat), 0, _heartBeatInterval);
    }

    private void StopHeartBeat()
    {
        CancelInvoke(nameof(HeartBeat));
    }
    private void HeartBeat()
    {
        HeartBeat output = new HeartBeat();
        SendMsg(LOBBY.HeartBeat, output.ToByteArray());
    }
    
    #endregion

    #region 收发消息
    
    /// <summary>
    /// 新增的发送消息函数，增加了消息ID，会把前面的消息ID（4字节）和后面的消息内容组成一个包再发送
    /// </summary>
    /// <param name="msgId">消息ID</param>
    /// <param name="???"></param>
    public void SendMsg(LOBBY msgId, byte[] data)
    {
        byte[] sendData = new byte[data.Length + 4];
        byte[] sendHeader = System.BitConverter.GetBytes((int)msgId);
        
        Array.Copy(sendHeader, 0, sendData, 0, 4);
        Array.Copy(data, 0, sendData, 4, data.Length);
        SendMsg(sendData);
    }

    private void OnComplete(SocketAction action, string msg)
    {
        switch (action)
        {
            case SocketAction.Connect:
            {
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Success);
                // RoomServer向LobbyServer发送第一条消息，登录该RoomServer
                RoomServerLogin data = new RoomServerLogin()
                {
                    ServerName = RoomManager.Instance.ServerName,
                    ServerId = RoomManager.Instance.ServerId,
                    MaxRoomCount = RoomManager.Instance.MaxRoomCount,
                    MaxPlayerPerRoom = RoomManager.Instance.MaxPlayerPerRoom,
                    Address = RoomManager.Instance._server.Address,
                    Port = RoomManager.Instance._server.Port,
                };
                SendMsg(LOBBY.RoomServerLogin, data.ToByteArray());
                StartHeartBeat();
            }
                break;
            case SocketAction.Send:
                break;
            case SocketAction.Receive:
                break;
            case SocketAction.Close:
                StopHeartBeat();
                UIManager.Instance.SystemTips(msg, PanelSystemTips.MessageType.Error);
                break;
            case SocketAction.Error:
                break;
        }
        Debug.Log(msg);
    }
    
    private void OnReceiveMsg(byte[] data)
    {
        LobbyMsgReply.ProcessMsg(data, data.Length);
    }
    
    #endregion
}
