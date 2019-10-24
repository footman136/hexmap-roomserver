using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using GameUtils;
using JetBrains.Annotations;
using Protobuf.Lobby;
using Protobuf.Room;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Actor
{
    public class ActorBehaviour : MonoBehaviour
    {
        
        #region 成员
        
        //This specific animal stats asset, create a new one from the asset menu under (LowPolyAnimals/NewAnimalStats)
        private ActorStats ScriptableActorStats;

        public StateMachineActor StateMachine;
        private Vector3 _targetPosition;
        private Vector3 _currentPosition;
        private float _distance;
        private float TIME_DELAY;

        public long RoomId;
        public long OwnerId;
        public long ActorId;
        public string Species;
        public int PosX;
        public int PosZ;
        public float Orientation;

        //If true, AI changes to this animal will be logged in the console.
        private bool _logChanges = false;
        
        #endregion
        
        #region 标准函数

        // Start is called before the first frame update
        public ActorBehaviour(long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, string species)
        {
            Species = species;
            TIME_DELAY = 1f;
            RoomId = roomId;
            OwnerId = ownerId;
            ActorId = actorId;
            PosX = posX;
            PosZ = posZ;
            Orientation = orientation;
            Species = species;
        }

        // Update is called once per frame
        private float timeNow = 0;
        void Update()
        {
            _distance = Vector3.Distance(_currentPosition, _targetPosition);
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

        public void Init()
        {
        }

        public void Fini()
        {
        }
        
        public void Log(string msg)
        {
            if(_logChanges)
                Debug.Log(msg);
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
        }

        #endregion
    }

}
