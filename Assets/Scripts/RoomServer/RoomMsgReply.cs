using System;
using System.Collections.Generic;
using UnityEngine;
using System.Net.Sockets;
using Google.Protobuf;
using Main;
// https://blog.csdn.net/u014308482/article/details/52958148
using Protobuf.Room;
using GameUtils;
using Actor;
using AI;

// https://github.com/LitJSON/litjson
public class RoomMsgReply
{
    private static SocketAsyncEventArgs _args;

    #region 消息分发

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

            // 记录心跳时间,每接收到一条消息,都更新时间,而不仅仅是心跳消息
            HEART_BEAT(null);

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
                    ENTER_ROOM(recvData);
                    break;
                case ROOM.LeaveRoom:
                    LEAVE_ROOM(recvData);
                    break;
                case ROOM.DownloadCities:
                    DOWNLOAD_CITIES(recvData);
                    break;
                case ROOM.DownloadActors:
                    DOWNLOAD_ACTORS(recvData);
                    break;
                case ROOM.DownloadResCell:
                    DOWNLOAD_RESCELL(recvData);
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
        PlayerInfo pi = new PlayerInfo(_args, input);
        
        //检测是否重复登录,如果发现曾经有人登录,则将前面的人踢掉
        var alreadyLoggedIn = ServerRoomManager.Instance.FindPlayerArgs(input.TokenId);
        if (alreadyLoggedIn != null)
        {
            var oldPlayer = ServerRoomManager.Instance.GetPlayer(alreadyLoggedIn);
            if (oldPlayer != null)
            {
                string roomName = "";
                RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(oldPlayer.RoomId);
                if (roomLogic != null)
                {
                    roomName = roomLogic.RoomName;
                }
                LeaveRoomReply output = new LeaveRoomReply()
                {
                    RoomName = roomName,
                    Ret = true,
                };
                ServerRoomManager.Instance.SendMsg(alreadyLoggedIn, ROOM_REPLY.LeaveRoomReply, output.ToByteArray());
                ServerRoomManager.Instance.RemovePlayer(alreadyLoggedIn, true);
                string msg = "Kick myself that priviously logged in."; // "踢掉之前登录的本用户.";
                ServerRoomManager.Instance.Log($"MSG: PLAYER_ENTER WARNING - " + msg + $" - {oldPlayer.Enter.Account}");
            }
        }
        
        ServerRoomManager.Instance.AddPlayer(_args, pi);

        {
            PlayerEnterReply output = new PlayerEnterReply()
            {
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.PlayerEnterReply, output.ToByteArray());
        }
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
            ServerRoomManager.Instance.Log($"MSG：UPLOAD_MAP - Begin upload map data! MapName:{input.RoomName}"); // 开始上传地图数据！地图名
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
                ServerRoomManager.Instance.Log($"MSG：UPLOAD_MAP Error - Save map data failed! Creator not found! MapName:{input.RoomName}"); // 保存地图数据失败！创建者没有找到！地图名
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

                ServerRoomManager.Instance.Log($"MSG: UPLOAD_MAP OK - Upload map data and save to redis succeeded! MapName:{input.RoomName} - Total Size:{totalSize}"); // 上传地图数据，并保存到Redis成功！地图名
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
            string msg = $"Cannot find the table - {tableName}"; // Redis中没有找到地图表格
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
            string msg = $"Read map data from redis failed! RoomId is not matched! RoomId from client:{input.RoomId} - RoomId from Redis:{roomId}"; // 从Redis中读取地图数据失败！roomId不匹配！传来的RoomId // Redis中保存的RoomId
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
            string msg = $"Read map data from redis failed! Player is not found! MapName:{roomName} - RoomId:{roomId}"; // 从Redis中读取地图数据失败！我自己并没有在战场服务器！地图名
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
            string msg = ($"The Battlefield is not created or has been disposed! MapName:{roomName} - RoomId:{roomId}"); // 该战场尚未创建或者已经被销毁！地图名
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
            string msg = ($"Map data is not valid, can be currupted! MapName:{roomName} - RoomId:{roomId}"); // 地图数据不合法，可能已经被损坏！地图名
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
        
        ServerRoomManager.Instance.Log($"MSG: DOWNLOAD_MAP - Download map data succeeded! MapName:{roomName} - Total Map Size:{totalSize}"); // 地图数据下载完成！地图名
    }
    
    private static void ENTER_ROOM(byte[] bytes)
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
                    NetRoomInfo roomInfo = new NetRoomInfo()
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

        PlayerInfo pi = null;
        PlayerInfoInRoom piir = null;
        if (roomLogic == null)
        {
            errMsg = $"Battlefield is not found! RoomId:{input.RoomId}"; // 战场没有找到！
        }
        else
        {
            pi = ServerRoomManager.Instance.GetPlayer(_args);
            if (pi == null)
            {
                errMsg = "PlayerInfo is not found!"; // 玩家没有找到！
            }
            else
            {
                pi.RoomId = input.RoomId;
                piir = roomLogic.GetPlayerInRoom(pi.Enter.TokenId);
                if (piir == null)
                {
                    errMsg = "PlayerInfoInRoom is not found!"; // 玩家没有找到！
                }
            }
        }

        if(piir != null)
        {
            // 把当前玩家设置为在线(所有玩家信息在房间创建的时候(RoomLogic.Init)就存在了, 只是不在线)
            roomLogic.Online(_args, pi.Enter, input.RoomId);

            // 通知大厅
            ServerRoomManager.Instance.UpdateRoomInfoToLobby(roomLogic);
            
            // 返回成功
            EnterRoomReply output = new EnterRoomReply()
            {
                Ret = true,
                RoomId = roomLogic.RoomId,
                RoomName = roomLogic.RoomName,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.EnterRoomReply, output.ToByteArray());
            ServerRoomManager.Instance.Log($"MSG: ENTER_ROOM OK - Player enters the battlefield! Account:{pi.Enter.Account} - Room:{roomLogic.RoomName}"); // 玩家进入战场！
        }
        else
        {   // 返回失败
            EnterRoomReply output = new EnterRoomReply()
            {
                Ret = false,
                ErrMsg = errMsg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.EnterRoomReply, output.ToByteArray());
            ServerRoomManager.Instance.Log("MSG: ENTER_ROOM Error - "+errMsg);
        }
    }

    private static void LEAVE_ROOM(byte[] bytes)
    {
        LeaveRoom input = LeaveRoom.Parser.ParseFrom(bytes);

        bool ret = false;
        RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(input.RoomId);
        if (roomLogic != null)
        {
            var pi = ServerRoomManager.Instance.GetPlayer(_args);
            if (pi != null)
            { // 把当前玩家在房间的状态设置为离线
                roomLogic.Offline(pi.Enter.TokenId);
                
                string account = pi.Enter.Account;
                ServerRoomManager.Instance.Log($"MSG: LEAVE_ROOM OK - Player leaves the battlefield! Account:{account} - Room:{roomLogic.RoomName}"); // 玩家离开战场！
            }

            // 通知大厅
            ServerRoomManager.Instance.UpdateRoomInfoToLobby(roomLogic);
            ret = true;
        }
        else
        {
            ServerRoomManager.Instance.Log($"MSG: LEAVE_ROOM Error - Battlefield is not found! RoomId:{input.RoomId}"); // 战场没有找到     
        }

        LeaveRoomReply output = new LeaveRoomReply()
        {
            Ret = ret,
        };
        ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.LeaveRoomReply, output.ToByteArray());
    }

    private static void DOWNLOAD_CITIES(byte [] bytes)
    {
        DownloadCities input = DownloadCities.Parser.ParseFrom(bytes);
        RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(input.RoomId);
        if (roomLogic == null)
        {
            string msg = $"Battlefield is not found!"; // 战场没有找到
            ServerRoomManager.Instance.Log("MSG: DOWNLOAD_CITIES Error - " + msg + $" - {input.RoomId}");
            DownloadCitiesReply output = new DownloadCitiesReply()
            {
                RoomId = input.RoomId,
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadCitiesReply, output.ToByteArray());
            return;
        }
        PlayerInfo pi = ServerRoomManager.Instance.GetPlayer(_args);
        if (pi == null)
        {
            string msg = $"Current Player is not found!"; // 当前玩家没有找到
            ServerRoomManager.Instance.Log("MSG: DOWNLOAD_CITIES Error - " + msg);
            DownloadCitiesReply output = new DownloadCitiesReply()
            {
                RoomId = input.RoomId,
                Ret = false,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadCitiesReply, output.ToByteArray());
            return;
        }

        long OwnerId = pi.Enter.TokenId;

        long capitalCityId = 0;
        foreach (var keyValue in roomLogic.UrbanManager.AllCities)
        {
            UrbanCity city = keyValue.Value;
            if (city.IsCapital && city.OwnerId == OwnerId)
                capitalCityId = city.CityId;
            CityAddReply output = new CityAddReply()
            {
                RoomId = city.RoomId,
                OwnerId = city.OwnerId,
                CityId = city.CityId,
                PosX = city.PosX,
                PosZ = city.PosZ,
                CellIndex = city.CellIndex,
                CityName = city.CityName,
                CitySize = city.CitySize,
                IsCapital = city.IsCapital,
                Ret = true,
            }; 
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.CityAddReply, output.ToByteArray());
        }

        {
            DownloadCitiesReply output = new DownloadCitiesReply()
            {
                RoomId = input.RoomId,
                TotalCount = roomLogic.UrbanManager.AllCities.Count,
                MyCount = roomLogic.UrbanManager.CountOfThePlayer(OwnerId),
                CapitalCityId = capitalCityId,
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadCitiesReply, output.ToByteArray());
            ServerRoomManager.Instance.Log($"MSG: DOWNLOAD_CITIES OK - Player:{pi.Enter.Account} - City Count:{output.MyCount}/{output.TotalCount}");
        }
    }

    private static void DOWNLOAD_ACTORS(byte[] bytes)
    {
        DownloadActors input = DownloadActors.Parser.ParseFrom(bytes);
        RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(input.RoomId);
        if (roomLogic == null)
        {
            string msg = $"Battlefield is not found!"; // 战场没有找到
            ServerRoomManager.Instance.Log("MSG: DOWNLOAD_ACTORS Error - " + msg + $" - {input.RoomId}");
            DownloadCitiesReply output = new DownloadCitiesReply()
            {
                RoomId = input.RoomId,
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadActorsReply, output.ToByteArray());
            return;
        }
        
        // 把房间内已有的所有actor都发给本人
        foreach (var keyValue in roomLogic.ActorManager.AllActors)
        {
            ActorBehaviour ab = keyValue.Value;
            
            if (ab.CellIndex == 0)
            {
                Debug.LogError("DOWNLOAD_ACTORS Erro - Actor position is lost!!!");
                continue;
            }
            
            ActorAddReply output = new ActorAddReply()
            {
                RoomId = ab.RoomId,
                OwnerId = ab.OwnerId,
                ActorId = ab.ActorId,
                PosX = ab.PosX,
                PosZ = ab.PosZ,
                CellIndex = ab.CellIndex,
                Orientation = ab.Orientation,
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
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.ActorAddReply, output.ToByteArray());
            
            // 更新AI状态, 注: 尽管参数与ActorAiStateReply一样, 但是这里是高级AI状态
            if (ab.AiDurationTime > 0)
            {
                ab.AiDurationTime -= (float) (DateTime.Now - ab.AiStartTime).TotalSeconds;
                if (ab.AiDurationTime < 0f)
                {
                    ServerRoomManager.Instance.Log($"RoomMsgReply DOWNLOAD_ACTORS Error - AiDurationTime is less than 0 - Name:{ab.Name} - Time:{ab.AiDurationTime}");
                    ab.AiDurationTime = 0f;
                }
            }

            HighAiStateReply output2 = new HighAiStateReply()
            {
                RoomId = ab.RoomId,
                OwnerId = ab.OwnerId,
                ActorId = ab.ActorId,
                State = ab.AiState,
                TargetId = ab.AiTargetId,
                CellIndexFrom = ab.CellIndex,
                CellIndexTo = ab.AiCellIndexTo,
                Orientation = ab.Orientation,
                DurationTime = ab.AiDurationTime,
                TotalTime = ab.AiTotalTime,
                Ret = true,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.HighAiStateReply, output2.ToByteArray());
        }

        {
            PlayerInfo pi = ServerRoomManager.Instance.GetPlayer(_args);
            if (pi == null)
            {
                string msg = $"当前玩家没有找到!";
                ServerRoomManager.Instance.Log("MSG: DOWNLOAD_ACTORS Error - " + msg);
                DownloadCitiesReply output = new DownloadCitiesReply()
                {
                    RoomId = input.RoomId,
                    Ret = false,
                    ErrMsg = msg,
                };
                ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadActorsReply, output.ToByteArray());
                return;
            }

            pi.IsReady = true; // 客户端准备好了,可以检测心跳了
            long OwnerId = pi.Enter.TokenId;

            {
                DownloadActorsReply output = new DownloadActorsReply()
                {
                    RoomId = input.RoomId,
                    TotalCount = roomLogic.UrbanManager.AllCities.Count,
                    MyCount = roomLogic.UrbanManager.CountOfThePlayer(OwnerId),
                    Ret = true,
                };
                ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadActorsReply, output.ToByteArray());
                ServerRoomManager.Instance.Log($"MSG: DOWNLOAD_ACTORS OK - Player:{pi.Enter.Account} - City Count:{output.MyCount}/{output.TotalCount}");
            }
        }
    }
    
    private static void DOWNLOAD_RESCELL(byte[] bytes)
    {
        DownloadResCell input = DownloadResCell.Parser.ParseFrom(bytes);
        RoomLogic roomLogic = ServerRoomManager.Instance.GetRoomLogic(input.RoomId);
        string msg = null;
        if (roomLogic != null)
        {
            const int PACKAGE_SIZE = 24;
            int packageCount = Mathf.CeilToInt(roomLogic.ResManager.AllRes.Count * ResInfo.GetSaveSize() / (float)PACKAGE_SIZE);
            int infoCountForEachPakcage = PACKAGE_SIZE / ResInfo.GetSaveSize();
            int packageIndex = 0;
            NetResCellInfo [] netResInfos = new NetResCellInfo[infoCountForEachPakcage];
            int index = 0;
            foreach(var keyValue in roomLogic.ResManager.AllRes)
            {
                var info = new NetResCellInfo()
                {
                    CellIndex = keyValue.Key,
                    ResType = (int)keyValue.Value.ResType,
                    ResAmount = keyValue.Value.GetAmount(keyValue.Value.ResType),
                };
                netResInfos[index++] = info;
                if (index == infoCountForEachPakcage)
                { // 凑够一批就发送
                    DownloadResCellReply output = new DownloadResCellReply()
                    {
                        RoomId = input.RoomId,
                        Ret = true,
                        PackageCount = packageCount,
                        PackageIndex = packageIndex,
                        InfoCount = index,
                        ResInfo = {netResInfos},
                    };
                    ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadResCellReply, output.ToByteArray());
                    ServerRoomManager.Instance.Log($"MSG: DOWNLOAD_RES - Package:{packageIndex}/{packageCount} - InfoCount:{index}");
                    packageIndex++;
                    index = 0;
                }
            }

            if(index > 0)
            { // 最后一段
                NetResCellInfo [] netResInfosLast = new NetResCellInfo[index];
                Array.Copy(netResInfos, 0, netResInfosLast, 0, index); 
                DownloadResCellReply output = new DownloadResCellReply()
                {
                    RoomId = input.RoomId,
                    Ret = true,
                    PackageCount = packageCount,
                    PackageIndex = packageIndex,
                    InfoCount = index,
                    ResInfo = {netResInfosLast},
                };
                ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadResCellReply, output.ToByteArray());
                ServerRoomManager.Instance.Log($"MSG: DOWNLOAD_RES OK - Package:{packageIndex}/{packageCount} - InfoCount:{index} - TotalCount:{roomLogic.ResManager.AllRes.Count}");
            }
            return;
        }
        else
        {
            msg = $"Battlfield is not found! RoomId:{input.RoomId}"; // 战场没有找到! 
            ServerRoomManager.Instance.Log("MSG: DOWNLOAD_RES Error - " + msg);
        }

        {
            DownloadResCellReply output = new DownloadResCellReply()
            {
                RoomId = input.RoomId,
                Ret = false,
                ErrMsg = msg,
            };
            ServerRoomManager.Instance.SendMsg(_args, ROOM_REPLY.DownloadResCellReply, output.ToByteArray());
        }
    }
    
    #endregion
}
