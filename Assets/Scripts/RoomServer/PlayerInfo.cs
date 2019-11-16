using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Sockets;
using Protobuf.Room;
using UnityEditor.Experimental.Rendering;
using UnityEngine;

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
        public long TimeSpanSinceLastRestoreActionPoint; // [现在]到[上次恢复行动点]的时间差
        
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

        public Coroutine CoroutineActionPoint;

        public PlayerInfo(SocketAsyncEventArgs args)
        {
            IsReady = false;
            Args = args;
        }

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
        
        public byte[] SaveBuffer()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            int version = 3;
            bw.Write(version);
            bw.Write(Enter.Account);
            bw.Write(Enter.TokenId);
            bw.Write(DateTime.Now.ToFileTime());
            bw.Write(TimeSpanSinceLastRestoreActionPoint);
            
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
            if (version >= 3)
            {
                TimeSinceLastSave = br.ReadInt64();
                TimeSpanSinceLastRestoreActionPoint = br.ReadInt32();
            }
            else
            {
                TimeSinceLastSave = DateTime.Now.ToFileTime();
                TimeSpanSinceLastRestoreActionPoint = 0;
            }

            _wood = br.ReadInt32();
            _food = br.ReadInt32();
            _iron = br.ReadInt32();
            if (version >= 2)
            {
                _actionPoint = br.ReadInt32();
                _actionPointMax = br.ReadInt32();
            }
            else
            {
                _actionPoint = 100;
                _actionPointMax = 100;
            }
            
            ServerRoomManager.Instance.Log($"PlayerInfo LoadBuffer OK - Player:{Enter.Account}");

            return true;
        }
        
    }
}