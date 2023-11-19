namespace RainMeadow
{
    public class OnlinePhysicalObjectDefinition : EntityDefinition
    {
        public int seed;
        public bool realized;
        public string serializedObject;

        public OnlinePhysicalObjectDefinition() { }
        public OnlinePhysicalObjectDefinition(int seed, bool realized, string serializedObject, OnlinePhysicalObject onlinePhysicalObject) : base(onlinePhysicalObject)
        {
            this.seed = seed;
            this.realized = realized;
            this.serializedObject = serializedObject;
        }
    }
}