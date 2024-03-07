using System.Runtime.CompilerServices;

namespace RainMeadow
{
    public class OnlineSeedCob : OnlinePhysicalObject
    {
        public OnlineSeedCob(OnlineSeedCobDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public SeedCob.AbstractSeedCob AbstractSeedCob => apo as SeedCob.AbstractSeedCob;

        public static OnlineEntity FromDefinition(OnlineSeedCobDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RoomSettings roomsetting = new RoomSettings(newObjectEvent.roomName, world.region, false, false, OnlineManager.lobby.gameMode.LoadWorldAs(world.game));
            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);
            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            var asc = new SeedCob.AbstractSeedCob(world, apo.realizedObject, apo.pos, apo.ID, newObjectEvent.originRoom, newObjectEvent.placedObjectIndex, newObjectEvent.originallyDead,
                roomsetting.placedObjects[newObjectEvent.placedObjectIndex].data as PlacedObject.ConsumableObjectData);
            asc.ID = id;
            return new OnlineSeedCob(newObjectEvent, asc);
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
