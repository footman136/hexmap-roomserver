using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Actor
{

    public class ActorManager
    {
        private static Dictionary<long, ActorBehaviour> _allActors = new Dictionary<long, ActorBehaviour>();
        public static Dictionary<long, ActorBehaviour> AllActors => _allActors;

        public void AddActor(long roomId, long ownerId, long actorId, int posX, int posZ, float orientation, string species)
        {
            ActorBehaviour ab = new ActorBehaviour(roomId, ownerId, actorId, posX, posZ, orientation, species);
            ab.Init();
            _allActors.Add(actorId, ab);
        }

        public void RemoveActor(long actorId)
        {
            if (_allActors.ContainsKey(actorId))
            {
                var actor = _allActors[actorId];
                actor.Fini();
                _allActors.Remove(actorId);
            }
        }
    }
}