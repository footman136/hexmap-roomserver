using System;
using Protobuf.Room;

namespace Actor
{
    public class PlayerInfo
    {
        public PlayerEnter Enter;
    
        public bool IsOnLine;
    
        public bool IsInRoom;
    
        public bool IsCreatedByMe;
    
        public long RoomId;

        public DateTime HeartBeatTime;
    }
}