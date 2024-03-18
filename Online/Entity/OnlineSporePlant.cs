namespace RainMeadow
{
    public class OnlineSporePlant : OnlineConsumable
    {
        public OnlineSporePlant(OnlineSporePlantDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public SporePlant.AbstractSporePlant AbstractSporePlant => apo as SporePlant.AbstractSporePlant;

        public static OnlineEntity FromDefinition(OnlineSporePlantDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            var asp = (SporePlant.AbstractSporePlant)SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            asp.ID = id;
            asp.originRoom = newObjectEvent.originRoom;
            asp.placedObjectIndex = newObjectEvent.placedObjectIndex;
            asp.isConsumed = newObjectEvent.originallyConsumed;
            asp.used = newObjectEvent.originallyUsed;
            asp.pacified = newObjectEvent.originallyPacified;
            return new OnlineSporePlant(newObjectEvent, asp);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineSporePlantState(this, inResource, tick);
        }

        public override void OnJoinedResource(OnlineResource inResource)
        {
            base.OnJoinedResource(inResource);
            // ?
        }
    }
}
