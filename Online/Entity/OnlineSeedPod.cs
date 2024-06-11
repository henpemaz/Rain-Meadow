namespace RainMeadow
{
    public class OnlineSeedCob : OnlineConsumable
    {
        public class OnlineSeedCobDefinition : OnlineConsumableDefinition
        {
            [OnlineField]
            public bool originallyDead;

            public OnlineSeedCobDefinition() { }

            public OnlineSeedCobDefinition(OnlineSeedCob onlineSeedCob, OnlineResource inResource) : base(onlineSeedCob, inResource)
            {
                this.originallyDead = onlineSeedCob.AbstractSeedCob.dead;
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, EntityState initialState)
            {
                return new OnlineSeedCob(this, inResource, (OnlineSeedCobState) initialState);
            }
        }

        public OnlineSeedCob(OnlineConsumableDefinition entityDefinition, OnlineResource inResource, OnlineConsumableState initialState) : base(entityDefinition, inResource, initialState)
        {

        }

        public OnlineSeedCob(AbstractConsumable ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        public SeedCob.AbstractSeedCob AbstractSeedCob => apo as SeedCob.AbstractSeedCob;


        protected override AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, PhysicalObjectEntityState initialState)
        {
            var consumableDef = (OnlineSeedCobDefinition)newObjectEvent;
            var apo = base.ApoFromDef(newObjectEvent, inResource, initialState);
            RoomSettings roomsetting = new RoomSettings(apo.Room.name, apo.world.region, false, false, OnlineManager.lobby.gameMode.LoadWorldAs(apo.world.game));

            var asc = new SeedCob.AbstractSeedCob(apo.world, apo.realizedObject, apo.pos, apo.ID, consumableDef.originRoom, consumableDef.placedObjectIndex, consumableDef.originallyDead,
                roomsetting.placedObjects[consumableDef.placedObjectIndex].data as PlacedObject.ConsumableObjectData) { isConsumed = (initialState as OnlineConsumableState).isConsumed };
            return asc;
        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlineSeedCobDefinition(this, onlineResource);
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

        public class OnlineSeedCobState : OnlineConsumableState
        {
            [OnlineField]
            bool isOpened;

            public OnlineSeedCobState() { }

            public OnlineSeedCobState(OnlineSeedCob onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                isOpened = onlineEntity.AbstractSeedCob.opened;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var onlineSeedPod = onlineEntity as OnlineSeedCob;
                if (!onlineSeedPod.AbstractSeedCob.opened && isOpened)
                {
                    (onlineSeedPod.AbstractSeedCob.realizedObject as SeedCob).Open();
                }
            }
        }
    }
}
