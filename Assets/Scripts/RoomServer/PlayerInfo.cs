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
    /// 房间内的玩家信息, 在房间生命周期内有效
    /// </summary>
    public class PlayerInfo
    {
        public PlayerEnter Enter;
        public SocketAsyncEventArgs Args;
        public bool IsReady;
        public long RoomId;

        public DateTime HeartBeatTime;
        
        #region 初始化

        public PlayerInfo(SocketAsyncEventArgs args, PlayerEnter enter)
        {
            IsReady = false;
            Args = args;
            Enter = new PlayerEnter()
            {
                Account = enter.Account,
                TokenId = enter.TokenId,
            };
        }

        #endregion
        
        
    }
}