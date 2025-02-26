
namespace RainMeadow {
    class SocialMemoryState : Serializer.ICustomSerializable {
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

            RelationshipState(SocialMemory.Relationship relationship) {
                subjectID = new(relationship.subjectID);
                like = relationship.like;
                fear = relationship.fear;
                know = relationship.know;
                tempLike = relationship.tempLike;
                tempFear = relationship.tempFear;
            }
            public RelationshipState() {}
            SocialMemory.Relationship Create() {
                return new SocialMemory.Relationship(subjectID.Create()) { 
                    like = this.like,
                    fear = this.fear,
                    know = this.know,
                    tempLike = this.tempLike,
                    tempFear = this.tempFear
                };
            }
        };

        public RelationshipState[] relationshipStates;
        public SocialMemoryState() {
        }

        void Serializer.ICustomSerializable.CustomSerialize(Serializer serializer) {
            if (serializer.IsWriting) serializer.writer.Write(relationshipStates.Length);
            else if (serializer.IsReading) relationshipStates = new RelationshipState[serializer.reader.ReadInt32()];

            for (int i = 0; i < relationshipStates.Length; i++) {
                serializer.Serialize(ref relationshipStates[i]);
            }  
        }
    }
}
        