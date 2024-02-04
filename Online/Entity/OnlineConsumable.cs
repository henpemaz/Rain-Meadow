using UnityEngine;

namespace RainMeadow
{
    public class OnlineConsumable : OnlinePhysicalObject
    {
        public OnlineConsumable(OnlineConsumableDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public AbstractConsumable Consumable => apo as AbstractConsumable;

        public static OnlineEntity FromDefinition(OnlineConsumableDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource.World;
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var acm = (AbstractConsumable)SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            acm.ID = id;
            acm.originRoom = newObjectEvent.originRoom;
            acm.placedObjectIndex = newObjectEvent.placedObjectIndex;
            acm.isConsumed = newObjectEvent.originallyConsumed;

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
