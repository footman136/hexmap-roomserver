using System.Collections;
using System.Collections.Generic;
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

        public void AddActor(long roomId, long ownerId, long actorId, int posX, int posZ, int cellIndex, float orientation, string species, int actorInfoId)
        {
            ActorBehaviour ab = new ActorBehaviour();
            ab.Init(roomId, ownerId, actorId, posX, posZ, cellIndex, orientation, species, actorInfoId);
            _allActors.Add(actorId, ab);
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

        public ActorBehaviour GetPlayer(long actorId)
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
    }
}