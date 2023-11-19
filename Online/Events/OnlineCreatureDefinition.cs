namespace RainMeadow
{
    public class OnlineCreatureDefinition : OnlinePhysicalObjectDefinition
    {
        public OnlineCreatureDefinition() { }
        public OnlineCreatureDefinition(int seed, bool realized, string serializedObject, OnlineResource onlineResource, OnlinePhysicalObject onlinePhysicalObject, TickReference memberSinceTick) : base(seed, realized, serializedObject, onlinePhysicalObject) { }
    }
}