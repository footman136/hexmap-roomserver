using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using GameUtils;
using JetBrains.Annotations;
using Protobuf.Lobby;
using Protobuf.Room;
using UnityEngine;
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
        public float AttackPower;
        public float DefencePower;
        public float Speed;
        public float FieldOfVision;
        public float ShootingRange;

        //This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)
        private ActorStats ScriptableActorStats;

        public StateMachineActor StateMachine;
        public Vector2 TargetPosition;
        public Vector2 CurrentPosition;
        private float _distance;
        private float TIME_DELAY;

        //If true, AI changes to this animal will be logged in the console.
        private bool _logChanges = false;
        
        #endregion
        
        #region 初始化

        public ActorBehaviour()
        {
            TIME_DELAY = 1f;
            StateMachine = new StateMachineActor(this);
        }

        public void Init(long roomId, long ownerId, long actorId, int posX, int posZ, int cellIndex, float orientation, 
            string species, int actorInfoId,
            string name, int hp, float attackPower, float defencePower, float speed, float fieldOfVision, float shootingRange)
        {
            RoomId = roomId;
            OwnerId = ownerId;
            ActorId = actorId;
            PosX = posX;
            PosZ = posZ;
            CellIndex = cellIndex;
            Orientation = orientation;
            Species = species;
            ActorInfoId = actorInfoId;

            Name = name;
            Hp = hp;
            AttackPower = attackPower;
            DefencePower = defencePower;
            Speed = speed;
            FieldOfVision = fieldOfVision;
            ShootingRange = shootingRange;
        }
        public void Fini()
        {
        }


        // Update is called once per frame
        private float timeNow = 0;
        public void Tick()
        {
            _distance = Vector3.Distance(CurrentPosition, TargetPosition);
            StateMachine.Tick();
            
            timeNow += Time.deltaTime;
            if (timeNow < TIME_DELAY)
            {
                return;
            }

            timeNow = 0;
        
            // AI的执行频率要低一些
            AI_Running();
        }

        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
        }
        
        #endregion;
        
        #region 存盘

        public void LoadFromTable(out string name, out int hp, out float attackPower, out float defencePower,
            out float speed, out float fieldOfVision, out float shootingRange)
        {
            var csv = CsvDataManager.Instance.GetTable("actor_info");
            if (csv == null)
            {
                Debug.LogError($"ActorBehaviour LoadFromTable Error - table not found:actor_info");
                name = "";
                hp = 0;
                attackPower = 0;
                defencePower = 0;
                speed = 0;
                fieldOfVision = 0;
                shootingRange = 0;
                return;
            }

            name = csv.GetValue(ActorInfoId, "Name");
            hp = csv.GetValueInt(ActorInfoId, "Hp");
            attackPower = csv.GetValueFloat(ActorInfoId, "AttackPower");
            defencePower = csv.GetValueFloat(ActorInfoId, "DefencePower");
            speed = csv.GetValueFloat(ActorInfoId, "Speed");
            fieldOfVision = csv.GetValueFloat(ActorInfoId, "FieldOfVision");
            shootingRange = csv.GetValueFloat(ActorInfoId, "ShootingRange");
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
            bw.Write(AttackPower);
            bw.Write(DefencePower);
            bw.Write(Speed);
            bw.Write(FieldOfVision);
            bw.Write(ShootingRange);
        }

        public void LoadBuffer(BinaryReader br)
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
            AttackPower = br.ReadSingle();
            DefencePower = br.ReadSingle();
            Speed = br.ReadSingle();
            FieldOfVision = br.ReadSingle();
            ShootingRange = br.ReadSingle();
        }
        
        #endregion
        
        #region AI - 第一层
        IEnumerator Running()
        {
            yield return new WaitForSeconds(ScriptableActorStats.thinkingFrequency);
            while (true)
            {
                AI_Running();
            
                yield return new WaitForSeconds(ScriptableActorStats.thinkingFrequency);
            }
        }
        #endregion
        
        #region AI - 第二层

        private bool bFirst = true;
        private float _lastTime = 0f;
        private float _deltaTime;

        private void AI_Running()
        {
            // 这里的_deltaTime是真实的每次本函数调用的时间间隔（而不是Time.deltaTime）。
            //_deltaTime = Time.deltaTime;
            var nowTime = Time.time;
            _deltaTime = nowTime - _lastTime;
            _lastTime = nowTime;

            if (bFirst)
            {
                //newBorn();
                _deltaTime = 0; // 第一次不记录时间延迟
                bFirst = false;
            }

            if (CurrentPosition != TargetPosition && StateMachine.CurrentAiState != FSMStateActor.StateEnum.WALK)
            {
                StateMachine.TriggerTransition(FSMStateActor.StateEnum.WALK);
            }
        }

        #endregion
    }

}
