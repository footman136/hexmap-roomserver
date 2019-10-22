using System;
using System.Collections.Generic;
using LitJson;
using UnityEngine;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Google.Protobuf;
using Main;
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
    
    // 只能放到主线程来执行的消息链表
    private static object _lockObj;

    public struct ObjMsgDefine
    {
        public SocketAsyncEventArgs _args;
        public byte[] _bytes;
        public int _size;
    };
    public static List<ObjMsgDefine> _objectMsgList;

    #region 消息分发

    public static void Init()
    {
        _lockObj = new object();
        lock (_lockObj)
        {
            _objectMsgList = new List<ObjMsgDefine>();
        }
    }
    
    /// <summary>
    /// 处理服务器接收到的消息
    /// </summary>
    /// <param name="args"></param>
    /// <param name="content"></param>
    public static void ProcessMsg(SocketAsyncEventArgs args, byte[] bytes, int size)
    {
        try
        {
            if (size < 4)
            {
                Debug.Log($"ProcessMsg Error - invalid data size:{size}");
                return;
            }

            _args = args;
            byte[] recvData = new byte[size - 4];
            Array.Copy(bytes, 4, recvData, 0, size - 4);

            int msgId = ParseMsgId(bytes);
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
                    AddObjMsg(args, bytes, size);
                    break;
                case ROOM.LeaveRoom:
                    AddObjMsg(args, bytes, size);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception - LobbyMsgReply - {e}");
        }
    }

    private static int ParseMsgId(byte[] bytes)
    {
        byte[] recvHeader = new byte[4];
        Array.Copy(bytes, 0, recvHeader, 0, 4);
        int msgId = BitConverter.ToInt32(recvHeader, 0);
        return msgId;
    }

    /// <summary>
    /// 凡是需要主线程处理的消息走这里。因为不是所有的消息都需要走主线程，所以做出区分（有的消息甚至不能放到主线程）
    /// </summary>
    /// <param name="args">注意：因为跨线程了，所以这个参数有可能失效</param>
    /// <param name="bytes"></param>
    public static void AddObjMsg(SocketAsyncEventArgs args, byte[] bytes, int size)
    {
        lock (_lockObj)
        {
            ObjMsgDefine objMsg = new ObjMsgDefine()
            {
                _args = args,
                _bytes = bytes,
                _size = size,
            };
            _objectMsgList.Add(objMsg);
        }
    }

    public static void ProcessObjMsg()
    {
        lock (_lockObj)
        {
            while (_objectMsgList.Count > 0)
            {
                ObjMsgDefine objMsg = _objectMsgList[0];
                
                byte[] recvData = new byte[objMsg._size - 4];
                Array.Copy(objMsg._bytes, 4, recvData, 0, objMsg._size - 4);
                
                int MsgId = ParseMsgId(objMsg._bytes);
                switch ((ROOM) MsgId)
                {
                    case ROOM.EnterRoom:
                        ENTER_ROOM(objMsg._args, recvData);
                        break;
                    case ROOM.LeaveRoom:
                        LEAVE_ROOM(objMsg._args, recvData);
                        break;
                }
                
                _objectMsgList.RemoveAt(0);
            }
        }
    }
    #endregion
    
    #region 立即消息处理
    private static void PLAYER_ENTER(byte[] bytes)
    {
        PlayerEnter input = PlayerEnter.Parser.ParseFrom(bytes);
        PlayerInfo pi = new PlayerInfo()
        {
            Enter = input,
        };
        RoomManager.Instance.AddPlayer(_args, pi);
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

            PlayerInfo pi = RoomManager.Instance.GetPlayer(_args);
            if (pi == null)
            {
                RoomManager.Instance.Log($"MSG：UPLOAD_MAP - 保存地图数据失败！创建者没有找到！地图名{input.RoomName}");
            }
            else
            {
                ret = true;
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
        PlayerInfo pi = RoomManager.Instance.GetPlayer(_args);
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
    #endregion
    
    #region 主线程消息处理
    private static void ENTER_ROOM(SocketAsyncEventArgs args, byte[] bytes)
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
                        roomLogic.name = $"RoomLogic - {roomInfo.RoomName}";
                        RoomManager.Instance.Rooms.Add(roomInfo.RoomId, roomLogic);
                        RoomManager.Instance.UpdateName();
                    }
                }
            }
        }
        else
        { // 房间已经开启，只需要进入    
            roomLogic = RoomManager.Instance.Rooms[input.RoomId];
        }
        if (roomLogic != null )
        {
            PlayerInfo pi = RoomManager.Instance.GetPlayer(args);
            if (pi != null)
            {
                roomLogic.AddPlayer(args, pi.Enter.TokenId, pi.Enter.Account);
                pi.RoomId = input.RoomId;
                RoomManager.Instance.SetPlayerInfo(args, pi);
            }
            else
            {
                RoomManager.Instance.Log("MSG: ENTER_ROOM - 玩家没有找到！");
            }
            RoomManager.Instance.Log($"MSG: ENTER_ROOM - 玩家进入房间！Account:{pi.Enter.Account} - Room:{roomLogic.RoomName}");
            
            // 通知大厅
            UpdateRoomInfo output2 = new UpdateRoomInfo()
            {
                RoomId = roomLogic.RoomId,
                RoomName = roomLogic.RoomName,
                Creator = roomLogic.Creator,
                CurPlayerCount    = roomLogic.CurPlayerCount,
                MaxPlayerCount = roomLogic.MaxPlayerCount,
                IsRunning = true,
                IsRemove = false,
            };
            ClientManager.Instance.LobbyManager.SendMsg(LOBBY.UpdateRoomInfo, output2.ToByteArray());
        }
        EnterRoomReply output = new EnterRoomReply()
        {
            Ret = true,
            RoomName = roomLogic.RoomName,
        };
        RoomManager.Instance.SendMsg(args, ROOM_REPLY.EnterRoomReply, output.ToByteArray());
    }

    private static void LEAVE_ROOM(SocketAsyncEventArgs args, byte[] bytes)
    {
        LeaveRoom input = LeaveRoom.Parser.ParseFrom(bytes);

        bool ret = false;
        if (!RoomManager.Instance.Rooms.ContainsKey(input.RoomId))
        {
            RoomManager.Instance.Log($"MSG: LEAVE_ROOM - room not found! RoomId:{input.RoomId}");
        }
        else
        {
            
            RoomLogic roomLogic = RoomManager.Instance.Rooms[input.RoomId];
            if (roomLogic != null)
            {
                string account = RoomManager.Instance.GetPlayer(args)?.Enter.Account;
                RoomManager.Instance.Log($"MSG: LEAVE_ROOM - 玩家离开房间！Account:{account} - Room:{roomLogic.RoomName}");
                if (roomLogic.CurPlayerCount == 0 && input.ReleaseIfNoUser)
                {
                    RoomManager.Instance.Rooms.Remove(input.RoomId);
                    // 通知大厅：删除房间
                    UpdateRoomInfo output2 = new UpdateRoomInfo()
                    {
                        RoomId = roomLogic.RoomId,
                        IsRemove = true,
                    };
                    ClientManager.Instance.LobbyManager.SendMsg(LOBBY.UpdateRoomInfo, output2.ToByteArray());
                }
            }
            RoomManager.Instance.RemovePlayer(args);
        }

        ret = true;
        LeaveRoomReply output = new LeaveRoomReply()
        {
            Ret = ret,
        };
        RoomManager.Instance.SendMsg(args, ROOM_REPLY.LeaveRoomReply, output.ToByteArray());
    }
    #endregion
}
