namespace RainMeadow
{
    internal class NewCreatureEvent : NewObjectEvent
    {
        public NewCreatureEvent() { }
        public NewCreatureEvent(int seed, bool realized, string serializedObject, OnlineResource onlineResource, OnlinePhysicalObject onlinePhysicalObject, TickReference memberSinceTick) : base(seed, realized, serializedObject, onlineResource, onlinePhysicalObject, memberSinceTick) { }

        public override EventTypeId eventType => EventTypeId.NewCreatureEvent;
    }
}