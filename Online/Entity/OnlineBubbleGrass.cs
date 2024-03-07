using static RainMeadow.OnlineEntity;

namespace RainMeadow
{
    public class OnlineBubbleGrass : OnlineConsumable
    {
        public OnlineBubbleGrass(OnlineBubbleGrassDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public BubbleGrass.AbstractBubbleGrass AbstractBubbleGrass => apo as BubbleGrass.AbstractBubbleGrass;

        public static OnlineEntity FromDefinition(OnlineConsumableDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var acm = (AbstractConsumable)SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            acm.ID = id;

            return new OnlineConsumable(newObjectEvent, acm);
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
