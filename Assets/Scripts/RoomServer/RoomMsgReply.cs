using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Google.Protobuf;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Room;
using UnityEditor;
using UnityEditor.Build.Player;

// https://github.com/LitJSON/litjson
public class RoomMsgReply
{
    private static SocketAsyncEventArgs _args;
    
    /// <summary>
    /// 处理服务器接收到的消息
    /// </summary>
    /// <param name="args"></param>
    /// <param name="content"></param>
    public static void ProcessMsg(SocketAsyncEventArgs args, byte[] data, int size)
    {
        try
        {
            if (size < 4)
            {
                Debug.Log($"ProcessMsg Error - invalid data size:{size}");
                return;
            }

            _args = args;

            byte[] recvHeader = new byte[4];
            Array.Copy(data, 0, recvHeader, 0, 4);
            byte[] recvData = new byte[size - 4];
            Array.Copy(data, 4, recvData, 0, size - 4);

            int msgId = BitConverter.ToInt32(recvHeader, 0);
            switch ((ROOM) msgId)
            {
                case ROOM.PlayerEnter:
                    PLAYER_ENTER(recvData);
                    break;
                case ROOM.UploadMap:
                    UPLOAD_MAP(recvData);
                    break;
                case ROOM.EnterRoom:
                    ENTER_ROOM(recvData);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception - LobbyMsgReply - {e}");
        }
    }

    private static void PLAYER_ENTER(byte[] bytes)
    {
        PlayerEnter input = PlayerEnter.Parser.ParseFrom(bytes);
        PlayerInfo pi = new PlayerInfo()
        {
            Enter = input,
        };
        RoomManager.Instance.Players[_args] = pi;
        RoomManager.Instance.Log($"MSG: 玩家登录房间服务器 - {input.Account}");

        PlayerEnterReply output = new PlayerEnterReply()
        {
            Ret = true,
        };
        RoomManager.Instance.SendMsg(_args, ROOM_REPLY.PlayerEnterReply, output.ToByteArray());
    }

    private static List<byte[]> mapDataBuffers = new List<byte[]>();
    private static void UPLOAD_MAP(byte[] bytes)
    {
        // 这个消息会有好多组
        UploadMap input = UploadMap.Parser.ParseFrom(bytes);
        if (input.PackageIndex == 0)
        {// 第一条此类消息
            mapDataBuffers.Clear();                        
        }
        mapDataBuffers.Add(input.MapData.ToByteArray());
        
        bool ret = false;
        long roomId = 0;
        if (input.IsLastPackage)
        {// 最后一条此类消息了
            // 生成唯一ID
            roomId = RoomManager.GuidToLongId();

            int totalSize = 0;
            foreach (var package in mapDataBuffers)
            {
                totalSize += package.Length;
            }

            byte[] totalMapData = new byte[totalSize];
            int position = 0;
            foreach (var package in mapDataBuffers)
            {
                Array.Copy(package, 0, totalMapData, position, package.Length);
                position += package.Length;
            }

            PlayerInfo pi = null;
            if (RoomManager.Instance.Players.ContainsKey(_args))
            {
                pi = RoomManager.Instance.Players[_args];
            }

            if (pi != null)
            {
                string tableName = "Maps";
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "Creator", pi.Enter.TokenId);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomId", roomId);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomName", input.RoomName);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "MaxPlayerCount", input.MaxPlayerCount);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "MapData", totalMapData);

                ret = true;
                RoomManager.Instance.Log($"MSG: UPLOAD_MAP - 保存地图数据成功！地图名:{input.RoomName} - Total Size:{totalSize}");
            }
            else
            {
                RoomManager.Instance.Log($"MSG：UPLOAD_MAP - 保存地图数据失败！创建者没有找到！地图名{input.RoomName}");
            }
        }
        UploadMapReply output = new UploadMapReply()
        {
            Ret = ret,
            IsLastPackage = input.IsLastPackage,
            RoomId = roomId,
        };
        RoomManager.Instance.SendMsg(_args, ROOM_REPLY.UploadMapReply, output.ToByteArray());
    }

    private static void ENTER_ROOM(byte[] bytes)
    {
        
    }
}
