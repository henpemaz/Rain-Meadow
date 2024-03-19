namespace RainMeadow
{
    public class OnlinePebblesPearl : OnlineConsumable
    {
        public OnlinePebblesPearl(OnlinePebblesPearlDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public PebblesPearl.AbstractPebblesPearl AbstractPebblesPearl => apo as PebblesPearl.AbstractPebblesPearl;

        public static OnlineEntity FromDefinition(OnlinePebblesPearlDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var app = (PebblesPearl.AbstractPebblesPearl)SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            app.ID = id;
            app.originRoom = newObjectEvent.originRoom;
            app.placedObjectIndex = newObjectEvent.placedObjectIndex;
            app.isConsumed = newObjectEvent.originallyConsumed;
            app.color = newObjectEvent.originalColor;
            app.number = newObjectEvent.originalNumber;

            return new OnlinePebblesPearl(newObjectEvent, app);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineConsumableState(this, inResource, tick);
        }

        public override void OnJoinedResource(OnlineResource inResource)
        {
            base.OnJoinedResource(inResource);
            // ?
        }
    }
}
