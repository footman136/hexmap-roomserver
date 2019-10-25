using Protobuf.Room;
using AI;
namespace Actor
{
    public class PlayerInfo
    {
        public PlayerEnter Enter;
    
        public bool IsOnLine;
    
        public bool IsInRoom;
    
        public bool IsCreatedByMe;
    
        public long RoomId;
    }
}