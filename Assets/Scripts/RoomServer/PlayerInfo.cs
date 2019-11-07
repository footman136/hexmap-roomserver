using System;
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
        
        
    }
}