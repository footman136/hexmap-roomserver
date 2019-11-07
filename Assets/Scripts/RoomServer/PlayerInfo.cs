using System;
using System.IO;
using Protobuf.Room;

namespace Actor
{
    public class PlayerInfo
    {
        public PlayerEnter Enter;
        public long RoomId;
        public DateTime HeartBeatTime;
        public bool IsCreatedByMe;
        
        private int _wood;
        private int _food;
        private int _iron;

        public int Wood => _wood;
        public int Food => _food;
        public int Iron => _iron;

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
        
        public byte[] SaveBuffer()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            int version = 1;
            bw.Write(version);
            bw.Write(Enter.Account);
            bw.Write(Enter.TokenId);
            bw.Write(_wood);
            bw.Write(_food);
            bw.Write(_iron);

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
            _wood = br.ReadInt32();
            _food = br.ReadInt32();
            _iron = br.ReadInt32();
            
            ServerRoomManager.Instance.Log($"PlayerInfo LoadBuffer OK - Player:{Enter.Account}");

            return true;
        }
    }
}