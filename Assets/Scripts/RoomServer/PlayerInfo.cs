using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Sockets;
using Protobuf.Room;
using UnityEngine;
using Google.Protobuf;
namespace Actor
{
    public class PlayerInfo
    {
        public PlayerEnter Enter;
        public long RoomId;
        public DateTime HeartBeatTime;
        public bool IsCreatedByMe;
        public bool IsReady;
        public SocketAsyncEventArgs Args;
        public long TimeSinceLastSave; // 上次存盘的时间
        public int TimeSinceLastRestoreActionPoint; // [现在]到[上次恢复行动点]的时间差
        
        private int _wood;
        private int _food;
        private int _iron;
        private int _actionPoint;
        private int _actionPointMax;

        public int Wood => _wood;
        public int Food => _food;
        public int Iron => _iron;
        public int ActionPoint => _actionPoint;
        public int ActionPointMax => _actionPointMax;

        private const int _ACTION_POINT_INTERVAL = 60; // 恢复行动点的时间间隔
        private const int _ACTION_POINT_ADD = 1; // 每次恢复几点行动点

        #region 初始化

        public PlayerInfo(SocketAsyncEventArgs args)
        {
            IsReady = false;
            Args = args;
        }
        
        #endregion
        
        #region 资源

        public void AddWood(int amount)
        {
            _wood += amount;
        }

        public void AddFood(int amount)
        {
            _food += amount;
        }

        public void AddIron(int amount)
        {
            _iron += amount;
        }

        #endregion
        
        #region 行动点
        
        public void AddActionPoint(int amount)
        {
            _actionPoint += amount;
            if (_actionPoint > _actionPointMax)
            {
                _actionPoint = _actionPointMax;
            }

            if (_actionPoint < 0)
            {
                _actionPoint = 0;
            }
        }

        public void SetActionPointMax(int amount)
        {
            _actionPointMax = amount;
        }
        
        public void RestoreActionPoint(int points)
        {
            AddActionPoint(points);
            UpdateActionPointReply output = new UpdateActionPointReply()
            {
                RoomId = RoomId,
                OwnerId = Enter.TokenId,
                Ret = true,
                ActionPoint = ActionPoint,
                ActionPointMax = ActionPointMax,
            };
            ServerRoomManager.Instance.SendMsg(Args, ROOM_REPLY.UpdateActionPointReply, output.ToByteArray());
            Debug.Log($"PlayerInfo restoreActionPointOneTime - [{Enter.Account}] 恢复行动点数:{_ACTION_POINT_ADD} - 现在行动点数:{ActionPoint}/{ActionPointMax}");
        }

        #endregion
        
        #region 存盘
        
        public byte[] SaveBuffer()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            int version = 3;
            bw.Write(version);
            bw.Write(Enter.Account);
            bw.Write(Enter.TokenId);
            bw.Write(DateTime.Now.ToFileTime());
            bw.Write(TimeSinceLastRestoreActionPoint);
            
            bw.Write(_wood);
            bw.Write(_food);
            bw.Write(_iron);
            bw.Write(_actionPoint);
            bw.Write(_actionPointMax);

            ServerRoomManager.Instance.Log($"PlayerInfo SaveBuffer OK - Player:{Enter.Account}");
            return ms.GetBuffer();
        }
        
        public bool LoadBuffer(byte[] bytes, int size)
        {
            MemoryStream ms = new MemoryStream(bytes);
            BinaryReader br = new BinaryReader(ms);
            int version = br.ReadInt32();
            Enter.Account = br.ReadString();
            Enter.TokenId = br.ReadInt64();
            TimeSinceLastSave = br.ReadInt64();
            TimeSinceLastRestoreActionPoint = br.ReadInt32();

            _wood = br.ReadInt32();
            _food = br.ReadInt32();
            _iron = br.ReadInt32();
            _actionPoint = br.ReadInt32();
            _actionPointMax = br.ReadInt32();
            
            ServerRoomManager.Instance.Log($"PlayerInfo LoadBuffer OK - Player:{Enter.Account}");

            return true;
        }
        
        #endregion
        
        #region Tick

        private float timeNow; 
        /// <summary>
        /// 定时被Update调用,在主线程中运行 (可能存在多线程问题, 如果代码崩溃在这里, 要注意)
        /// </summary>
        public void Tick()
        {
            timeNow += Time.deltaTime;
            if (timeNow > TimeSinceLastRestoreActionPoint)
            {
                timeNow = 0;
                TimeSinceLastRestoreActionPoint = _ACTION_POINT_INTERVAL;
                RestoreActionPoint(_ACTION_POINT_ADD);
            }
        }
        
        #endregion
    }
}