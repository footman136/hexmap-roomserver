using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Protobuf.Lobby;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Room;
using UnityEngine.Experimental.PlayerLoop;
using Object = UnityEngine.Object;
using PlayerEnter = Protobuf.Room.PlayerEnter;
using PlayerEnterReply = Protobuf.Room.PlayerEnterReply;
using RoomInfo = Protobuf.Room.RoomInfo;


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
                case ROOM.DownloadMap:
                    DOWNLOAD_MAP(recvData);
                    break;
                case ROOM.EnterRoom:
                    ENTER_ROOM(recvData);
                    break;
                case ROOM.LeaveRoom:
                    LEAVE_ROOM(recvData);
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
            RoomManager.Instance.Log($"MSG：UPLOAD_MAP - 开始上传地图数据！地图名{input.RoomName}");
        }
        mapDataBuffers.Add(input.MapData.ToByteArray());
        
        bool ret = true;
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

            if (pi == null)
            {
                ret = false;
                RoomManager.Instance.Log($"MSG：UPLOAD_MAP - 保存地图数据失败！创建者没有找到！地图名{input.RoomName}");
            }
            else
            {
                string tableName = $"MAP:{roomId}";
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "Creator", pi.Enter.TokenId);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomId", roomId);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomName", input.RoomName);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "MaxPlayerCount", input.MaxPlayerCount);
                RoomManager.Instance.Redis.CSRedis.HSet(tableName, "MapData", totalMapData);

                RoomManager.Instance.Log($"MSG: UPLOAD_MAP - 上传地图数据，并保存到Redis成功！地图名:{input.RoomName} - Total Size:{totalSize}");
            }
        }
        UploadMapReply output = new UploadMapReply()
        {
            Ret = ret,
            IsLastPackage = input.IsLastPackage,
            RoomId = roomId,
            RoomName = input.RoomName,
        };
        RoomManager.Instance.SendMsg(_args, ROOM_REPLY.UploadMapReply, output.ToByteArray());
    }

    private static void DOWNLOAD_MAP(byte[] bytes)
    {
        DownloadMap input = DownloadMap.Parser.ParseFrom(bytes);
        string tableName = $"MAP:{input.RoomId}";
        if (!RoomManager.Instance.Redis.CSRedis.Exists(tableName))
        {
            RoomManager.Instance.Log($"MSG：DOWNLOAD_MAP - Redis中没有找到地图表格 - {tableName}");
        }
        
        //////////////
        // 校验地图的RoomId是否和Redis中保存的一致        
        long roomId = RoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "RoomId");
        if (roomId != input.RoomId)
        {
            RoomManager.Instance.Log($"MSG：DOWNLOAD_MAP - 从Redis中读取地图数据失败！roomId不匹配！传来的RoomId:{input.RoomId} - Redis中保存的RoomId:{roomId}");
            return;
        }
        string roomName = RoomManager.Instance.Redis.CSRedis.HGet<string>(tableName, "RoomName");
        
        //////////////
        // 计算这张地图是不是我自己创建的
        PlayerInfo pi = null;
        if (RoomManager.Instance.Players.ContainsKey(_args))
        {
            pi = RoomManager.Instance.Players[_args];
        }

        if (pi == null)
        {
            RoomManager.Instance.Log($"MSG：DOWNLOAD_MAP - 从Redis中读取地图数据失败！创建者没有找到！地图名:{roomName}");
            return;
        }
        long TokenId = RoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "Creator");
        bool IsCreateByMe = TokenId == pi.Enter.TokenId;
        
        //////////////
        // 其他数据
        int maxPlayerCount = RoomManager.Instance.Redis.CSRedis.HGet<int>(tableName, "MaxPlayerCount");

        //////////////
        // 读取地图数据
        byte[] totalData = RoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, "MapData");
        int totalSize = totalData.Length;
        const int CHUNK_SIZE = 900;
        int remainSize = totalSize;
        int index = 0;
        int position = 0;
        while (remainSize > CHUNK_SIZE)
        {
            DownloadMapReply output = new DownloadMapReply()
            {
                RoomName = roomName,
                RoomId = input.RoomId,
                MaxPlayerCount = maxPlayerCount,
                IsCreatedByMe = IsCreateByMe,
                IsLastPackage = false,
                PackageIndex = index++,
                Ret = true,
            };
            byte[] sendBytes = new byte[CHUNK_SIZE];
            Array.Copy(totalData, position, sendBytes, 0, CHUNK_SIZE);
            output.MapData = ByteString.CopyFrom(sendBytes);
            position += CHUNK_SIZE;
            remainSize -= CHUNK_SIZE;
            RoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
        }

        {
            DownloadMapReply output = new DownloadMapReply()
            {
                RoomName = roomName,
                RoomId = input.RoomId,
                MaxPlayerCount = maxPlayerCount,
                IsCreatedByMe = IsCreateByMe,
                IsLastPackage = true,
                PackageIndex = index++,
                Ret = true,
            };
            byte[] sendBytes = new byte[remainSize];
            Array.Copy(totalData, position, sendBytes, 0, remainSize);
            output.MapData = ByteString.CopyFrom(sendBytes);
            position += remainSize;
            remainSize -= remainSize;
            RoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
        }
        RoomManager.Instance.Log($"MSG：DOWNLOAD_MAP - 地图数据下载完成！地图名:{roomName} - Total Map Size:{totalSize}");
    }
    
    private static void ENTER_ROOM(byte[] bytes)
    {
        EnterRoom input = EnterRoom.Parser.ParseFrom(bytes);
        RoomLogic roomLogic = null;
        if (!RoomManager.Instance.Rooms.ContainsKey(input.RoomId))
        { // 房间没有开启，需要开启并进入
            var go = Resources.Load("Prefabs/RoomLogic");
            if (go != null)
            {
                var go2 = Object.Instantiate(go, RoomManager.Instance.transform) as GameObject;
                if (go2 != null)
                {
                    roomLogic = go2.GetComponent<RoomLogic>();
                    if (roomLogic != null)
                    {
                        string tableName = $"MAP:{input.RoomId}";
                        long createrId = RoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "Creator");
                        RoomInfo roomInfo = new RoomInfo()
                        {
                            RoomId = RoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "RoomId"),
                            MaxPlayerCount =
                                RoomManager.Instance.Redis.CSRedis.HGet<int>(tableName, "MaxPlayerCount"),
                            RoomName = RoomManager.Instance.Redis.CSRedis.HGet<string>(tableName, "RoomName"),
                            Creator = createrId,
                        };
                        roomLogic.Init(roomInfo);
                        RoomManager.Instance.Rooms.Add(roomInfo.RoomId, roomLogic);
                        go2.name = $"RoomLogic - {roomInfo.RoomName}";
                        RoomManager.Instance.UpdateName();
                    }
                }
            }
        }
        else
        { // 房间已经开启，只需要进入    
            roomLogic = RoomManager.Instance.Rooms[input.RoomId];
        }
        if (roomLogic != null && RoomManager.Instance.Players.ContainsKey(_args))
        {
            PlayerInfo pi = RoomManager.Instance.Players[_args];
            roomLogic.AddPlayer(_args, pi.Enter.TokenId, pi.Enter.Account);
        }
        EnterRoomReply output = new EnterRoomReply()
        {
            Ret = true,
        };
        RoomManager.Instance.SendMsg(_args, ROOM_REPLY.EnterRoomReply, output.ToByteArray());
    }

    private static void LEAVE_ROOM(byte[] bytes)
    {
        LeaveRoom input = LeaveRoom.Parser.ParseFrom(bytes);

        bool ret = false;
        if (!RoomManager.Instance.Rooms.ContainsKey(input.RoomId))
        {
            RoomManager.Instance.Log($"MSG: LEAVE_ROOM - room not found! RoomId:{input.RoomId}");
        }
        else
        {
            if (RoomManager.Instance.Players.ContainsKey(_args))
            {
                PlayerInfo pi = RoomManager.Instance.Players[_args];
                //RoomManager.Instance.Rooms.Remove()
            }
            
        }

        ret = true;
        LeaveRoomReply output = new LeaveRoomReply()
        {
            Ret = ret,
        };
        RoomManager.Instance.SendMsg(_args, ROOM_REPLY.LeaveRoomReply, output.ToByteArray());
    }
}
