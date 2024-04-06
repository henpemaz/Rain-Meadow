using System.Text.RegularExpressions;

namespace RainMeadow
{
    public class OnlineMeadowCollectible : OnlinePhysicalObject
    {
        public OnlineMeadowCollectible(OnlinePhysicalObjectDefinition entityDefinition, AbstractPhysicalObject apo) : base(entityDefinition, apo) { }

        public class Definition : OnlinePhysicalObjectDefinition
        {
            public Definition()
            {
            }

            public Definition(OnlinePhysicalObjectDefinition opod) : base(opod)
            {
            }

            public Definition(int seed, bool realized, string serializedObject, EntityId entityId, OnlinePlayer owner, bool isTransferable) : base(seed, realized, serializedObject, entityId, owner, isTransferable)
            {
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource)
            {
                return OnlineMeadowCollectible.FromDefinition(this, inResource);
            }
        }
        new public static OnlineEntity FromDefinition(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource)
        {
            World world = inResource is RoomSession rs ? rs.World : inResource is WorldSession ws ? ws.world : throw new InvalidProgrammerException("not room nor world");
            EntityID id = world.game.GetNewID();
            id.altSeed = newObjectEvent.seed;

            RainMeadow.Debug("serializedObject: " + newObjectEvent.serializedObject);

            var apo = SaveState.AbstractPhysicalObjectFromString(world, newObjectEvent.serializedObject);
            apo.ID = id;
            if (!world.IsRoomInRegion(apo.pos.room))
            {
                RainMeadow.Debug("Room not in region: " + apo.pos.room);
                // most common cause is gates which are ambiguous room names, solve for current region instead of global
                string[] obarray = Regex.Split(newObjectEvent.serializedObject, "<oA>");
                string[] wcarray = obarray[2].Split('.');
                AbstractRoom room = world.GetAbstractRoom(wcarray[0]);
                if (room != null)
                {
                    RainMeadow.Debug($"fixing room index -> {room.index}");
                    apo.pos.room = room.index;
                }
                else
                {
                    RainMeadow.Error("Couldn't find room in region: " + wcarray[0]);
                }
            }

            RainMeadow.Debug($"room index -> {apo.pos.room} in region? {world.IsRoomInRegion(apo.pos.room)}");


            return new OnlineMeadowCollectible(newObjectEvent, apo);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new MeadowCollectibleState(this, inResource, tick);
        }

        public class MeadowCollectibleState : PhysicalObjectEntityState
        {
            [OnlineField]
            private bool placed;
            [OnlineField]
            private bool collected;
            [OnlineField(nullable = true)]
            private TickReference collectedTR;

            public MeadowCollectibleState() : base() { }
            public MeadowCollectibleState(OnlinePhysicalObject onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                var collectible = onlineEntity.apo as AbstractMeadowCollectible;
                placed = collectible.placed;
                collected = collectible.collected;
                collectedTR = collectible.collectedTR;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var collectible = (onlineEntity as OnlinePhysicalObject).apo as AbstractMeadowCollectible;

                collectible.placed = this.placed;
                if (!collectible.collected && this.collected)
                {
                    collectible.collected = this.collected;
                    collectible.collectedTR = this.collectedTR;
                    collectible.collectedAt = collectible.world.game.clock;
                }
            }
        }
    }
}
