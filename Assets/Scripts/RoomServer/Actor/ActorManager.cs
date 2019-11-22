using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AI
{
    /// <summary>
    /// 本类与ActorBehaviour配合使用，专门管理ActorBehaviour
    /// </summary>
    public class ActorManager
    {
        public Dictionary<long, ActorBehaviour> AllActors = new Dictionary<long, ActorBehaviour>();

        public void AddActor(ActorBehaviour ab, RoomLogic roomLogic)
        {
            AllActors[ab.ActorId] = ab;
            ab.Init(roomLogic);
        }

        public bool RemoveActor(long actorId)
        {
            if (AllActors.ContainsKey(actorId))
            {
                var actor = AllActors[actorId];
                actor.Fini();
                AllActors.Remove(actorId);
                return true;
            }

            return false;
        }
        
        public ActorBehaviour GetActor(long actorId)
        {
            return AllActors.ContainsKey(actorId) ? AllActors[actorId] : null;
        }

        public void Tick()
        {
            foreach (var keyValue in AllActors)
            {
                keyValue.Value.Tick();
            }
        }
        
        public byte[] SaveBuffer()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            int version = 5;
            bw.Write(version);
            Dictionary<long, ActorBehaviour> newAllActors = new Dictionary<long, ActorBehaviour>();
            foreach (var keyValue in AllActors)
            {
                if (keyValue.Value.CellIndex == 0
                    || (keyValue.Value.PosX == 0 && keyValue.Value.PosZ == 0))
                {
                    Debug.LogError("ActorManager SaveBuffer Error - Actor Position lost!!!");
                    continue;
                }
                newAllActors[keyValue.Key] = keyValue.Value;
            }
            int index = 0;
            bw.Write(newAllActors.Count);
            foreach (var keyValue in newAllActors)
            {
                bw.Write(index++);
                keyValue.Value.SaveBuffer(bw);
            }
    
            return ms.GetBuffer();
        }

        public bool LoadBuffer(byte[] bytes, int size, RoomLogic roomLogic)
        {
            MemoryStream ms = new MemoryStream(bytes);
            BinaryReader br = new BinaryReader(ms);
            int version = br.ReadInt32();
            int actorCount = br.ReadInt32();
            if (actorCount < 0 || actorCount > 9999)
            {
                ServerRoomManager.Instance.Log($"ActorManager - LoadBuffer Error - count of actors is invalid: {actorCount} - should between:{0}~{9999}");
                return false;
            }
    
            AllActors.Clear();
            for (int i = 0; i < actorCount; ++i)
            {
                int index = br.ReadInt32();
                if (index != i)
                {
                    ServerRoomManager.Instance.Log($"ActorManager - LoadBuffer Error - actor index is not valid:{index}- should between:{0}~{actorCount}");
                    return false;
                }
    
                ActorBehaviour ab = new ActorBehaviour();
                ab.LoadBuffer(br, version);
                if (ab.CellIndex == 0)
                {
                    Debug.LogError("ActorManager LoadBuffer Error - CellIndex is lost!!!");
                    continue;
                }
                AddActor(ab, roomLogic);
            }
            ServerRoomManager.Instance.Log($"ActorManager LoadBuffer OK - Count of Actors ：{AllActors.Count}"); //单元个数
    
            return true;
        }

        public int CountOfThePlayer(long ownerId)
        {
            int count = 0;
            foreach (var keyValue in AllActors)
            {
                if (keyValue.Value.OwnerId == ownerId)
                {
                    count++;
                }
            }

            return count;
        }
    }
}