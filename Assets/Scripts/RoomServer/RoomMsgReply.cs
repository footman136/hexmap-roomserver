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
using GameUtils;
using Actor;
using AI;

// https://github.com/LitJSON/litjson
public class RoomMsgReply
{
    private static SocketAsyncEventArgs _args;

    #region 消息分发

    public static void Init()
    {
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
                case ROOM.HeartBeat:
                    HEART_BEAT(recvData);
                    break;
                case ROOM.UploadMap:
                    UPLOAD_MAP(recvData);
                    break;
                case ROOM.DownloadMap:
                    DOWNLOAD_MAP(recvData);
                    break;
                case ROOM.EnterRoom:
                    ENTER_ROOM(_args, recvData);
                    break;
                case ROOM.LeaveRoom:
                    LEAVE_ROOM(_args, recvData);
                    break;
                default:
                    // 通用消息处理器，别的地方要想响应找个消息，应该调用MsgDispatcher.RegisterMsg()来注册消息处理事件
                    MsgDispatcher.ProcessMsg(args, bytes, size);
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
    #endregion
    
    #region 消息处理
    private static void PLAYER_ENTER(byte[] bytes)
    {
        PlayerEnter input = PlayerEnter.Parser.ParseFrom(bytes);
        PlayerInfo pi = new PlayerInfo()
        {
            Enter = input,
        };
        ServerRoomManager.Instance.AddPlayer(_args, pi);
        ServerRoomManager.Instance.Log($"MSG: PLAYER_ENTER - 玩家登录房间服务器 - {input.Account}");

        PlayerEnterReply output = new PlayerEnterReply()
        {
            Ret = true,
        };
        ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.PlayerEnterReply, output.ToByteArray());
    }

    private static void HEART_BEAT(byte[] byts)
    {
        var pi = ServerRoomManager.Instance.GetPlayer(_args);
        if (pi != null)
        {
            pi.HeartBeatTime = DateTime.Now;
        }
    }

    private static List<byte[]> mapDataBuffers = new List<byte[]>();
    private static void UPLOAD_MAP(byte[] bytes)
    {
        // 这个消息会有好多组
        UploadMap input = UploadMap.Parser.ParseFrom(bytes);
        if (input.PackageIndex == 0)
        {// 第一条此类消息
            mapDataBuffers.Clear();
            ServerRoomManager.Instance.Log($"MSG：UPLOAD_MAP - 开始上传地图数据！地图名{input.RoomName}");
        }
        mapDataBuffers.Add(input.MapData.ToByteArray());
        
        bool ret = true;
        long roomId = 0;
        if (input.IsLastPackage)
        {// 最后一条此类消息了
            // 生成唯一ID
            roomId = Utils.GuidToLongId();

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

            PlayerInfo pi = ServerRoomManager.Instance.GetPlayer(_args);
            if (pi == null)
            {
                ServerRoomManager.Instance.Log($"MSG：UPLOAD_MAP - 保存地图数据失败！创建者没有找到！地图名{input.RoomName}");
                ret = false;
            }
            else
            {
                ret = true;
                string tableName = $"MAP:{roomId}";
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "Creator", pi.Enter.TokenId);
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomId", roomId);
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "RoomName", input.RoomName);
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "MaxPlayerCount", input.MaxPlayerCount);
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "MapData", totalMapData);
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "MapDataSize", totalSize);
                ServerRoomManager.Instance.Redis.CSRedis.HSet(tableName, "CreateTime", DateTime.Now);

                ServerRoomManager.Instance.Log($"MSG: UPLOAD_MAP - 上传地图数据，并保存到Redis成功！地图名:{input.RoomName} - Total Size:{totalSize}");
            }
            
        }
        UploadMapReply output = new UploadMapReply()
        {
            Ret = ret,
            IsLastPackage = input.IsLastPackage,
            RoomId = roomId,
            RoomName = input.RoomName,
        };
        ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.UploadMapReply, output.ToByteArray());
    }

    private static void DOWNLOAD_MAP(byte[] bytes)
    {
        DownloadMap input = DownloadMap.Parser.ParseFrom(bytes);
        string tableName = $"MAP:{input.RoomId}";
        if (!ServerRoomManager.Instance.Redis.CSRedis.Exists(tableName))
        {
            string msg = $"Redis中没有找到地图表格 - {tableName}";
            ServerRoomManager.Instance.Log("MSG：DOWNLOAD_MAP - " + msg);
            DownloadMapReply output = new DownloadMapReply()
            {
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
            return;
        }
        
        //////////////
        // 校验地图的RoomId是否和Redis中保存的一致        
        long roomId = ServerRoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "RoomId");
        if (roomId != input.RoomId)
        {
            string msg = $"从Redis中读取地图数据失败！roomId不匹配！传来的RoomId:{input.RoomId} - Redis中保存的RoomId:{roomId}";
            ServerRoomManager.Instance.Log("MSG：DOWNLOAD_MAP - " + msg);
            DownloadMapReply output = new DownloadMapReply()
            {
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
            return;
        }
        string roomName = ServerRoomManager.Instance.Redis.CSRedis.HGet<string>(tableName, "RoomName");
        
        //////////////
        // 计算这张地图是不是我自己创建的
        PlayerInfo pi = ServerRoomManager.Instance.GetPlayer(_args);
        if (pi == null)
        {
            string msg = $"从Redis中读取地图数据失败！我自己并没有在房间服务器！地图名:{roomName} - RoomId:{roomId}";
            ServerRoomManager.Instance.Log("MSG：DOWNLOAD_MAP - " + msg);
            DownloadMapReply output = new DownloadMapReply()
            {
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
            return;
        }
        long TokenId = ServerRoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "Creator");
        bool IsCreateByMe = TokenId == pi.Enter.TokenId;
        
        //////////////
        // 其他数据
        int maxPlayerCount = ServerRoomManager.Instance.Redis.CSRedis.HGet<int>(tableName, "MaxPlayerCount");

        //////////////
        // 读取地图数据
        byte[] totalData = ServerRoomManager.Instance.Redis.CSRedis.HGet<byte[]>(tableName, "MapData");
        int totalSize = totalData.Length;
        
        //////////////
        // 服务器把这份数据留起来自己用——这部分代码暂时无效
        var roomLogic = ServerRoomManager.Instance.GetRoomLogic(roomId);
        if (roomLogic == null)
        {
            string msg = ($"该房间尚未创建或者已经被销毁！地图名:{roomName} - RoomId:{roomId}");
            ServerRoomManager.Instance.Log("MSG：DOWNLOAD_MAP - " + msg);
            DownloadMapReply output = new DownloadMapReply()
            {
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
            return;
        }
        if (!roomLogic.SetMap(totalData))
        {
            string msg = ($"地图数据不合法，可能已经被损坏！地图名:{roomName} - RoomId:{roomId}");
            ServerRoomManager.Instance.Log("MSG：DOWNLOAD_MAP - " + msg);
            DownloadMapReply output = new DownloadMapReply()
            {
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
            return;
        }

        // 把玩家私人的数据（比如：城市）从Redis里读出来
        roomLogic.LoadPlayer(_args);
        
        //////////////
        // 把地图数据下发到客户端
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
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
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
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadMapReply, output.ToByteArray());
        }
        
        // 最后一件事：把房间内已有的所有actor都发给本人
        foreach (var keyValue in roomLogic.ActorManager.AllActors)
        {
            ActorBehaviour ab = keyValue.Value;
            CreateATroopReply output = new CreateATroopReply()
            {
                RoomId = ab.RoomId,
                OwnerId = ab.OwnerId,
                ActorId = ab.ActorId,
                PosX = ab.PosX,
                PosZ = ab.PosZ,
                Orientation = ab.Orientation,
                Species = ab.Species,
                ActorInfoId = ab.ActorInfoId,
                
                Name = ab.Name,
                Hp = ab.Hp,
                AttackPower = ab.AttackPower,
                DefencePower = ab.DefencePower,
                Speed = ab.Speed,
                FieldOfVision = ab.FieldOfVision,
                ShootingRange = ab.ShootingRange,
                
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.CreateAtroopReply, output.ToByteArray());
        }
        
        ServerRoomManager.Instance.Log($"MSG：DOWNLOAD_MAP - 地图数据下载完成！地图名:{roomName} - Total Map Size:{totalSize}");
    }
    
    private static void ENTER_ROOM(SocketAsyncEventArgs args, byte[] bytes)
    {
        bool ret = false;
        string errMsg = "";
        EnterRoom input = EnterRoom.Parser.ParseFrom(bytes);
        RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(input.RoomId);
        if (roomLogic == null)
        { // 房间没有开启，需要开启并进入
            roomLogic = new RoomLogic();
            if (roomLogic != null)
            {
                string tableName = $"MAP:{input.RoomId}";
                if(ServerRoomManager.Instance.Redis.CSRedis.Exists(tableName))
                {
                    long createrId = ServerRoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "Creator");
                    RoomInfo roomInfo = new RoomInfo()
                    {
                        RoomId = ServerRoomManager.Instance.Redis.CSRedis.HGet<long>(tableName, "RoomId"),
                        MaxPlayerCount =
                            ServerRoomManager.Instance.Redis.CSRedis.HGet<int>(tableName, "MaxPlayerCount"),
                        RoomName = ServerRoomManager.Instance.Redis.CSRedis.HGet<string>(tableName, "RoomName"),
                        Creator = createrId,
                    };
                    // 初始化
                    roomLogic.Init(roomInfo);
                    ServerRoomManager.Instance.AddRoomLogic(roomInfo.RoomId, roomLogic);
                }
                else
                {// 房间地图数据没有找到，等于没有创建房间
                    roomLogic = null;
                }
            }
        }

        if (roomLogic != null)
        {
            PlayerInfo pi = ServerRoomManager.Instance.GetPlayer(args);
            if (pi != null)
            {
                ret = true;
                roomLogic.AddPlayer(args, pi.Enter.TokenId, pi.Enter.Account);
                pi.RoomId = input.RoomId;
                ServerRoomManager.Instance.SetPlayerInfo(args, pi);
            
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
                MixedManager.Instance.LobbyManager.SendMsg(LOBBY.UpdateRoomInfo, output2.ToByteArray());
                // 返回成功
                EnterRoomReply output = new EnterRoomReply()
                {
                    Ret = true,
                    RoomId = roomLogic.RoomId,
                    RoomName = roomLogic.RoomName,
                };
                ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.EnterRoomReply, output.ToByteArray());
                ServerRoomManager.Instance.Log($"MSG: ENTER_ROOM - 玩家进入房间！Account:{pi.Enter.Account} - Room:{roomLogic.RoomName}");
                return;
            }
            else
            {
                errMsg = "玩家没有找到！";
            }
        }
        else
        {
            errMsg = $"房间没有找到！RoomId:{input.RoomId}";
        }
        {   // 返回失败
            EnterRoomReply output = new EnterRoomReply()
            {
                Ret = false,
                ErrMsg = errMsg,
            };
            ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.EnterRoomReply, output.ToByteArray());
            ServerRoomManager.Instance.Log("MSG: ENTER_ROOM - "+errMsg);
        }
    }

    private static void LEAVE_ROOM(SocketAsyncEventArgs args, byte[] bytes)
    {
        LeaveRoom input = LeaveRoom.Parser.ParseFrom(bytes);

        bool ret = false;
        RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(input.RoomId);
        if (roomLogic != null)
        {
            UIManager.Instance.EndLoading();
            string account = roomLogic.GetPlayer(args)?.Enter.Account;
            ServerRoomManager.Instance.Log($"MSG: LEAVE_ROOM - 玩家离开房间！Account:{account} - Room:{roomLogic.RoomName}");
            ServerRoomManager.Instance.RemovePlayer(args, input.ReleaseIfNoUser);
            ret = true;
        }
        else
        {
            ServerRoomManager.Instance.Log($"MSG: LEAVE_ROOM - room not found! RoomId:{input.RoomId}");                
        }

        LeaveRoomReply output = new LeaveRoomReply()
        {
            Ret = ret,
        };
        ServerRoomManager.Instance.SendMsg(args, ROOM_REPLY.LeaveRoomReply, output.ToByteArray());
    }
    #endregion
}
