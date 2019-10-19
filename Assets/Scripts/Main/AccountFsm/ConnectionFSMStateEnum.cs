using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConnectionFSMStateEnum
{
    [global::System.Serializable]
    public enum StateEnum : uint
    {
        NONE = 0,
        START = 1,
        CONNECTING = 2,
        CONNECTED = 3,
        DISCONNECTED = 4,
        ROOM = 5,
        COUNT = 6,
    }
}
