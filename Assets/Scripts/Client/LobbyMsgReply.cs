using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
// https://github.com/LitJSON/litjson
using LitJson;
using Main;

// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Lobby;

public class LobbyMsgReply
{

    public static void ProcessMsg(byte[] data, int size)
    {
        if (size < 4)
        {
            Debug.Log($"ProcessMsg Error - invalid data size:{size}");
            return;
        }

        byte[] recvHeader = new byte[4];
        Array.Copy(data, 0, recvHeader, 0, 4);
        byte[] recvData = new byte[size - 4];
        Array.Copy(data, 4, recvData, 0, size - 4);

        int msgId = System.BitConverter.ToInt32(recvHeader,0);
        switch ((LOBBY_REPLY) msgId)
        {
            case LOBBY_REPLY.RoomServerLoginReply:
                ROOM_SERVER_LOGIN_REPLY(recvData);
                break;
            case LOBBY_REPLY.UpdateRoomInfoReply:
                UPDATE_ROOM_INFO_REPLY(recvData);
                break;
        }
    }

    private static void ROOM_SERVER_LOGIN_REPLY(byte[] bytes)
    {
        RoomServerLoginReply input = RoomServerLoginReply.Parser.ParseFrom(bytes);
        if (input.Ret)
        {
            MixedManager.Instance.StateMachine.TriggerTransition(ConnectionFSMStateEnum.StateEnum.CONNECTED);
            MixedManager.Instance.LobbyManager.Log("MSG: LobbyMsgReply ROOM_SERVER_LOGIN_REPLY OK - Room-server logged in succeeded!"); // 房间服务器登录成功！
        }
        else
        {
            MixedManager.Instance.LobbyManager.Log("MSG: LobbyMsgReply ROOM_SERVER_LOGIN_REPLY Error - Room-server logged in failed"); // 房间服务器登录失败！
        }
    }

    private static void UPDATE_ROOM_INFO_REPLY(byte[] bytes)
    {
        UpdateRoomInfoReply input = UpdateRoomInfoReply.Parser.ParseFrom(bytes);
        if (!input.Ret)
        {
            MixedManager.Instance.LobbyManager.Log("MSG: LobbyMsgReply UPDATE_ROOM_INFO_REPLY Error - Update room information failed!"); // 更新房间信息失败！
        }
    }
}
