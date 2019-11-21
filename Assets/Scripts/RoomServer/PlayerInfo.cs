using System;
using System.Diagnostics.Contracts;
using System.IO;
using System.Net.Sockets;
using Protobuf.Room;
using UnityEngine;
using Google.Protobuf;

namespace Actor
{
    /// <summary>
    /// 这个类是房间服务器范围内使用的玩家类, 不属于某个具体的房间, 是属于服务器的, 在PlayerEnter和PlayerLeave之间存在
    /// 未来可以支持, 一个玩家同时进入多个房间, 只要把RoomId换成链表即可
    /// </summary>
    public class PlayerInfo
    {
        public PlayerEnter Enter;
        public long RoomId;
        public bool IsReady;
        public bool IsCreatedByMe;
        public SocketAsyncEventArgs Args;
        
        public DateTime HeartBeatTime; // 心跳: 记录接收到最近这条消息的时间
        
        #region 初始化

        public PlayerInfo(SocketAsyncEventArgs args)
        {
            IsReady = false;
            Args = args;
        }

        #endregion
    }
}