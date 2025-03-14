using System;
using System.Linq;

namespace RainMeadow {
    class FriendTrackerState : AIModuleState {
        override public Type ModuleType => typeof(FriendTracker);

        [OnlineField]
        public bool followClosestFriend;

        [OnlineField(nullable: true)]
        public OnlineEntity.EntityId? friend;

        [OnlineFieldHalf]
        public float desiredCloseness;

        [OnlineFieldHalf]
        public float tamingDifficlty;

        public FriendTrackerState() {}
        public FriendTrackerState(AIModule module) {
            if (module is FriendTracker friendTracker) {
                followClosestFriend = friendTracker.followClosestFriend;
                friend = (friendTracker.friend?.abstractCreature is AbstractPhysicalObject apo
                    && OnlinePhysicalObject.map.TryGetValue(apo, out var oe)) ? oe.id : null;
                tamingDifficlty = friendTracker.tamingDifficlty;
                desiredCloseness = friendTracker.desiredCloseness;
            } else throw new ArgumentException();
        }
        public override void ReadTo(AIModule module) {
            if (module is FriendTracker friendTracker) {
                friendTracker.followClosestFriend = followClosestFriend;
                
                if (friend is not null) {
                    friendTracker.friend = (friend?.FindEntity() as OnlinePhysicalObject)?.apo?.realizedObject as Creature;
                    if (friendTracker.friend is not null) {
                           friendTracker.friendRel = module.AI.creature.state.socialMemory.relationShips.Where(
                                x => x.subjectID == friendTracker.friend.abstractCreature.ID).FirstOrDefault();
                    }
                }

                friendTracker.tamingDifficlty = tamingDifficlty;
                friendTracker.desiredCloseness = desiredCloseness;
            }
        }
    }

}
