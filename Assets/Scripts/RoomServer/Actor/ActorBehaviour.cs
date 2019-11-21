using System;
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
            
            // AI state params
            bw.Write(AiState);
            bw.Write(AiTargetId);
            bw.Write(AiCellIndexTo);
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

            if (header >= 4)
            { // AI State params
                AiState = br.ReadInt32();
                AiTargetId = br.ReadInt64();
                AiCellIndexTo = br.ReadInt32();
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
