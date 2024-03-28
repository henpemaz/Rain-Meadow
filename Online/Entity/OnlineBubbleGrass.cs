using static RainMeadow.OnlineEntity;

namespace RainMeadow
{
    public class OnlineBubbleGrass : OnlineConsumable
    {
        public OnlineBubbleGrass(OnlineBubbleGrassDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public BubbleGrass.AbstractBubbleGrass AbstractBubbleGrass => apo as BubbleGrass.AbstractBubbleGrass;

        public static OnlineEntity FromDefinition(OnlineBubbleGrassDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var abg = (BubbleGrass.AbstractBubbleGrass)SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            abg.ID = id;
            abg.oxygenLeft = newObjectEvent.originalOxygenLevel;
            abg.originRoom = newObjectEvent.originRoom;
            abg.placedObjectIndex = newObjectEvent.placedObjectIndex;
            abg.isConsumed = newObjectEvent.originallyConsumed;
            return new OnlineBubbleGrass(newObjectEvent, abg);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineBubbleGrassState(this, inResource, tick);
        }

        public override void OnJoinedResource(OnlineResource inResource)
        {
            base.OnJoinedResource(inResource);
            // ?
        }
    }
}
