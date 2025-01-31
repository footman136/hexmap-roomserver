﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using GameUtils;
using Google.Protobuf;
using JetBrains.Annotations;
using Protobuf.Lobby;
using Protobuf.Room;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

namespace AI
{
    /// <summary>
    /// 本类需要ActorManager来进行管理，与ActorManager配套使用，与ActorVisualizer无关，这是为了看以后是否方便移植到服务器去
    /// </summary>
    public class ActorBehaviour
    {
        
        #region 成员
        
        public long RoomId;
        public long OwnerId;
        public long ActorId;
        public int PosX;
        public int PosZ;
        public int CellIndex;
        public float Orientation;
        public string Species = "N/A";
        public int ActorInfoId;
        
        public string Name;
        public int Hp;
        public int HpMax;
        public float AttackPower;
        public float DefencePower;
        public float Speed;
        public float FieldOfVision; // 视野
        public float ShootingRange; // 射程

        public float AttackDuration; // 攻击持续时间
        public float AttackInterval; // 攻击间隔
        public int AmmoBase; // 弹药基数
        public int AmmoBaseMax; // 最大弹药基数
        
        // AI params
        public int AiState;
        public long AiTargetId;
        public int AiCellIndexTo;
        public float AiTotalTime; // 原始的总的持续时间
        public float AiDurationTime; // 剩余的时间
        public DateTime AiStartTime; // 本AI状态开始的时间
        
        // High AI params
        // 高级AI
        public int HighAiState;
        public long HighAiTargetId;
        public int HighAiCellIndexTo;
        public float HighAiDurationTime;
        public float HighAiTotalTime;
        public DateTime HighAiStartTime; // 本AI状态开始的时间

        //This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)
        private ActorStats ScriptableActorStats;

        //public StateMachineActor StateMachine; // 目前都是在客户端进行AI,这里暂时没用
        public Vector2 TargetPosition;
        public Vector2 CurrentPosition;
        private float _distance;
        private float TIME_DELAY;

        private RoomLogic _roomLogic;
        
        //If true, AI changes to this animal will be logged in the console.
        private bool _logChanges = false;
        
        #endregion
        
        #region 初始化

        public ActorBehaviour()
        {
            TIME_DELAY = 1f;
            //StateMachine = new StateMachineActor(this);
        }

        public void Init(RoomLogic roomLogic)
        {
            _roomLogic = roomLogic;
            MsgDispatcher.RegisterMsg((int)ROOM.AmmoSupply, OnAmmoSupply);
        }
        public void Fini()
        {
            MsgDispatcher.UnRegisterMsg((int)ROOM.AmmoSupply, OnAmmoSupply);
        }


        // Update is called once per frame
        private float timeNow = 0;
        public void Tick()
        {
            _distance = Vector3.Distance(CurrentPosition, TargetPosition);
            //StateMachine.Tick();
            
            timeNow += Time.deltaTime;
            if (timeNow < TIME_DELAY)
            {
                return;
            }

            timeNow = 0;
        
            // AI的执行频率要低一些
            //AI_Running();
        }

        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
        }
        
        #endregion;
        
        #region 存盘

        public void LoadFromTable()
        {
            var csv = CsvDataManager.Instance.GetTable("actor_info");
            if (csv == null)
            {
                Debug.LogError($"ActorBehaviour LoadFromTable Error - table not found:actor_info");
                return;
            }

            Name = csv.GetValue(ActorInfoId, "Name");
            Hp = csv.GetValueInt(ActorInfoId, "Hp");
            HpMax = csv.GetValueInt(ActorInfoId, "Hp");
            AttackPower = csv.GetValueFloat(ActorInfoId, "AttackPower_1"); // 攻击力未来扩展为多种,实现兵种相克
            DefencePower = csv.GetValueFloat(ActorInfoId, "DefencePower");
            Speed = csv.GetValueFloat(ActorInfoId, "Speed");
            FieldOfVision = csv.GetValueFloat(ActorInfoId, "FieldOfVision");
            ShootingRange = csv.GetValueFloat(ActorInfoId, "ShootingRange");
            
            AttackDuration = csv.GetValueFloat(ActorInfoId, "AttackDuration");
            AttackInterval = csv.GetValueFloat(ActorInfoId, "AttackInterval");
            AmmoBase = csv.GetValueInt(ActorInfoId, "AmmoBase");
            AmmoBaseMax = csv.GetValueInt(ActorInfoId, "AmmoBase");
        }
    
        public void SaveBuffer(BinaryWriter bw)
        {
            bw.Write(RoomId);    
            bw.Write(OwnerId);
            bw.Write(ActorId);
            bw.Write(PosX);
            bw.Write(PosZ);
            bw.Write(CellIndex);
            bw.Write(Orientation);
            bw.Write(Species);
            bw.Write(ActorInfoId);

            bw.Write(Name);
            bw.Write(Hp);
            bw.Write(HpMax);
            bw.Write(AttackPower);
            bw.Write(DefencePower);
            bw.Write(Speed);
            bw.Write(FieldOfVision);
            bw.Write(ShootingRange);
            
            bw.Write(AttackDuration);
            bw.Write(AttackInterval);
            bw.Write(AmmoBase);
            bw.Write(AmmoBaseMax);
            
//            // AI state params
//            bw.Write(AiState);
//            bw.Write(AiTargetId);
//            bw.Write(AiCellIndexTo);
//            if (AiDurationTime > 0)
//            {
//                AiDurationTime -= (float) (DateTime.Now - AiStartTime).TotalSeconds;
//            }
//
//            if (AiDurationTime < 0)
//            {
//                ServerRoomManager.Instance.Log($"ActorBehaviour Save Buffer Error - AiDurationTime is less than 0 - Name:{Name} - Time:{AiDurationTime}");
//                AiDurationTime = 0;
//            }
//            bw.Write(AiDurationTime);
//            bw.Write(AiTotalTime);
            
            // High AI state params
            bw.Write(HighAiState);
            bw.Write(HighAiCellIndexTo);
            bw.Write(HighAiTargetId);
            if (HighAiDurationTime > 0)
            {
                HighAiDurationTime -= (float) (DateTime.Now - HighAiStartTime).TotalSeconds;
            }
            if (HighAiDurationTime < 0)
            {
                ServerRoomManager.Instance.Log($"ActorBehaviour Save Buffer Error - HighAiDurationTime is less than 0 - Name:{Name} - Time:{HighAiDurationTime}");
                HighAiDurationTime = 0;
            }
            bw.Write(HighAiDurationTime);
            bw.Write(HighAiTotalTime);
        }

        public void LoadBuffer(BinaryReader br, int header)
        {
            RoomId = br.ReadInt64();
            OwnerId = br.ReadInt64();
            ActorId = br.ReadInt64();
            PosX = br.ReadInt32();
            PosZ = br.ReadInt32();
            CellIndex = br.ReadInt32();
            Orientation = br.ReadSingle();
            Species = br.ReadString();
            ActorInfoId = br.ReadInt32();

            Name = br.ReadString();
            Hp = br.ReadInt32();
            HpMax = br.ReadInt32();
            AttackPower = br.ReadSingle();
            DefencePower = br.ReadSingle();
            Speed = br.ReadSingle();
            FieldOfVision = br.ReadSingle();
            ShootingRange = br.ReadSingle();

            AttackDuration = br.ReadSingle();
            AttackInterval = br.ReadSingle();
            AmmoBase = br.ReadInt32();
            AmmoBaseMax = AmmoBase;
            AmmoBaseMax = br.ReadInt32();

            if (header >= 9)
            {
                // 去掉了4,5,6的修改, 普通AI状态不再保存, 仅保存高级AI的参数
            }
            else
            {
                if (header >= 4)
                {
                    // AI State params
                    AiState = br.ReadInt32();
                    AiTargetId = br.ReadInt64();
                    AiCellIndexTo = br.ReadInt32();
                }

                if (header >= 5)
                {
                    // 开始时间从读盘时重新计算, 这样玩家下次登录以后, 得到的时间差, 都是在线的时间
                    // 玩家离线的时间不计算在内, 如果要想把离线时间也计算在内, 需要在存盘的时候, 把存盘时间记录下来, 
                    // 然后读盘的时候读取到AiStartTime里
                    AiDurationTime = br.ReadSingle();
                    AiStartTime = DateTime.Now;
                }

                if (header >= 6)
                {
                    AiTotalTime = br.ReadSingle();
                }
                else
                {
                    AiTotalTime = AiDurationTime;
                }
            }

            // High AI state params
            if (header >= 7)
            { // 读取高级AI存盘数据, AI-代理权
                HighAiState = br.ReadInt32();
                HighAiCellIndexTo = br.ReadInt32();
                HighAiTargetId = br.ReadInt64();
            }

            if (header >= 8)
            {
                HighAiDurationTime = br.ReadSingle();
                HighAiTotalTime = br.ReadSingle();
                HighAiStartTime = DateTime.Now; 
            }
        }
        
        #endregion
    
        #region 补充弹药
    
        private void OnAmmoSupply(SocketAsyncEventArgs args, byte[] bytes)
        {
            AmmoSupply input = AmmoSupply.Parser.ParseFrom(bytes);
            if (input.ActorId != ActorId)
                return; // 不是自己，略过
    
            AmmoBase = input.AmmoBase;
            if (AmmoBase > AmmoBaseMax)
            {
                ServerRoomManager.Instance.Log($"ActorBehaviour OnAmmoSupply Error - Ammobase is invalid! AmmoBase:{AmmoBase}/{AmmoBaseMax}"); //  弹药基数出现异常
                AmmoBase = AmmoBaseMax;
            }
            
            AmmoSupplyReply output = new AmmoSupplyReply()
            {
                RoomId = input.RoomId,
                OwnerId = input.OwnerId,
                ActorId = input.ActorId,
                AmmoBase = input.AmmoBase,
                Ret = true,
            };
            _roomLogic.BroadcastMsg(ROOM_REPLY.AmmoSupplyReply, output.ToByteArray());
        }
        
        #endregion
        
        #region AI - 第一层
//        IEnumerator Running()
//        {
//            yield return new WaitForSeconds(ScriptableActorStats.thinkingFrequency);
//            while (true)
//            {
//                AI_Running();
//            
//                yield return new WaitForSeconds(ScriptableActorStats.thinkingFrequency);
//            }
//        }
        #endregion
        
        #region AI - 第二层

//        private bool bFirst = true;
//        private float _lastTime = 0f;
//        private float _deltaTime;
//
//        private void AI_Running()
//        {
//            // 这里的_deltaTime是真实的每次本函数调用的时间间隔（而不是Time.deltaTime）。
//            //_deltaTime = Time.deltaTime;
//            var nowTime = Time.time;
//            _deltaTime = nowTime - _lastTime;
//            _lastTime = nowTime;
//
//            if (bFirst)
//            {
//                //newBorn();
//                _deltaTime = 0; // 第一次不记录时间延迟
//                bFirst = false;
//            }
//
//            if (CurrentPosition != TargetPosition && StateMachine.CurrentAiState != FSMStateActor.StateEnum.WALK)
//            {
//                StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK);
//            }
//        }

        #endregion
    }

}
