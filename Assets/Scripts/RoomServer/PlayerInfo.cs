﻿using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using Protobuf.Room;
using UnityEngine;

public class PlayerInfo
{
    public PlayerEnter Enter;

    public bool IsOnLine;

    public bool IsInRoom;

    public bool IsCreatedByMe;

    public long RoomId;

}
