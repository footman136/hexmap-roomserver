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
        private Dictionary<long, ActorBehaviour> _allActors = new Dictionary<long, ActorBehaviour>();
        public Dictionary<long, ActorBehaviour> AllActors => _allActors;

        public void AddActor(ActorBehaviour ab)
        {
            _allActors.Add(ab.ActorId, ab);
        }

        public bool RemoveActor(long actorId)
        {
            if (_allActors.ContainsKey(actorId))
            {
                var actor = _allActors[actorId];
                actor.Fini();
                _allActors.Remove(actorId);
                return true;
            }

            return false;
        }

        public ActorBehaviour GetActor(long actorId)
        {
            if (_allActors.ContainsKey(actorId))
            {
                return _allActors[actorId];
            }

            return null;
        }

        public void Tick()
        {
            foreach (var keyValue in _allActors)
            {
                keyValue.Value.Tick();
            }
        }
        
        public byte[] SaveBuffer()
        {
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            int version = 1;
            bw.Write(version);
            bw.Write(AllActors.Count);
            int index = 0;
            foreach (var keyValue in AllActors)
            {
                bw.Write(index++);
                keyValue.Value.SaveBuffer(bw);
            }
    
            return ms.GetBuffer();
        }

        public bool LoadBuffer(byte[] bytes, int size)
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
                ab.LoadBuffer(br);
                AddActor(ab);
            }
            ServerRoomManager.Instance.Log($"ActorManager LoadBuffer - 单元个数：{AllActors.Count}");
    
            return true;
        }
    }
}