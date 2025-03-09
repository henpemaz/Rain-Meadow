
using System.Linq;
using RainMeadow.Generics;

namespace RainMeadow {

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    class SocialMemoryState : OnlineState {
        public class OnlineGameEntityID : Serializer.ICustomSerializable {
            public int spawner;
            public int number;
            public int altSeed;
            void Serializer.ICustomSerializable.CustomSerialize(Serializer serializer) {
                serializer.Serialize(ref spawner);
                serializer.Serialize(ref number);
                serializer.Serialize(ref altSeed);
            }
            public OnlineGameEntityID(EntityID entityID) {
                this.spawner = entityID.spawner;
                this.number = entityID.number;
                this.altSeed = entityID.altSeed;
            } 

            public OnlineGameEntityID() {}

            public EntityID Create() {
                return new EntityID { altSeed = this.altSeed, spawner = this.spawner, number = this.number };
            }
        }

        public class RelationshipState : Serializer.ICustomSerializable {
            public OnlineGameEntityID subjectID;
            public float like;
            public float fear;
            public float know;
            public float tempLike;
            public float tempFear;
            void Serializer.ICustomSerializable.CustomSerialize(Serializer serializer) {
                serializer.Serialize(ref subjectID);
                serializer.Serialize(ref like);
                serializer.Serialize(ref fear);
                serializer.Serialize(ref know);
                serializer.Serialize(ref tempLike);
                serializer.Serialize(ref tempFear);
            }

            public RelationshipState(SocialMemory.Relationship relationship) {
                subjectID = new(relationship.subjectID);
                like = relationship.like;
                fear = relationship.fear;
                know = relationship.know;
                tempLike = relationship.tempLike;
                tempFear = relationship.tempFear;
            }
            public RelationshipState() {}
            public SocialMemory.Relationship Create() {
                return new SocialMemory.Relationship(subjectID.Create()) { 
                    like = this.like,
                    fear = this.fear,
                    know = this.know,
                    tempLike = this.tempLike,
                    tempFear = this.tempFear
                };
            }
        };
        
        [OnlineField]
        public DynamicOrderedCustomSerializables<RelationshipState> relationshipStates;

        public SocialMemoryState(SocialMemory socialMemory) {
            relationshipStates = new(socialMemory.relationShips.Select(x => new RelationshipState(x)).ToList());
        }

        public SocialMemoryState() {}

        public void ReadTo(SocialMemory mem) {
            mem.relationShips = relationshipStates.list.Select(x => x.Create()).ToList();
        }
    }
}
        