using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlineSeedPod : OnlinePhysicalObject
    {
        public OnlineSeedPod(OnlineSeedPodDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public SeedCob.AbstractSeedCob AbstractSeedCob => apo as SeedCob.AbstractSeedCob;

        public static OnlineEntity FromDefinition(OnlineSeedPodDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");

            //TODO get from campaigne
            RoomSettings roomsetting = new RoomSettings(newObjectEvent.roomName, world.region, false, false, SlugcatStats.Name.White);
            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            var asc = new SeedCob.AbstractSeedCob(world, apo.realizedObject, apo.pos, apo.ID, newObjectEvent.originRoom, newObjectEvent.placedObjectIndex, newObjectEvent.originallyDead,
                roomsetting.placedObjects[newObjectEvent.placedObjectIndex].data as PlacedObject.ConsumableObjectData);

            return new OnlineSeedPod(newObjectEvent, asc);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineSeedCobState(this, inResource, tick);
        }

        public override void OnJoinedResource(OnlineResource inResource)
        {
            base.OnJoinedResource(inResource);
            // ?
        }
    }
}
