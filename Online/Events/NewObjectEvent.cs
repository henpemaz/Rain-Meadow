using Mono.Cecil;

namespace RainMeadow
{
    internal class NewObjectEvent : NewEntityEvent
    {
        public int seed;
        public WorldCoordinate enterPos;
        public bool realized;
        public string serializedObject;

        public NewObjectEvent() { }
        public NewObjectEvent(int seed, WorldCoordinate enterPos, bool realized, string serializedObject, OnlineResource onlineResource, OnlinePhysicalObject onlinePhysicalObject, TickReference memberSinceTick) : base(onlineResource, onlinePhysicalObject, memberSinceTick)
        {
            this.seed = seed;
            this.enterPos = enterPos;
            this.realized = realized;
            this.serializedObject = serializedObject;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref seed);
            serializer.SerializeNoStrings(ref enterPos);
            serializer.Serialize(ref realized);
            serializer.Serialize(ref serializedObject);
        }

        public override EventTypeId eventType => EventTypeId.NewObjectEvent;
    }
}