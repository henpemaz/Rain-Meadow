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
                return new OnlineSeedCob(this, inResource, (OnlineSeedCobState)initialState);
            }
        }

        public OnlineSeedCob(OnlineConsumableDefinition entityDefinition, OnlineResource inResource, OnlineConsumableState initialState) : base(entityDefinition, inResource, initialState)
        {

        }

        public OnlineSeedCob(AbstractConsumable ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        public SeedCob.AbstractSeedCob AbstractSeedCob => apo as SeedCob.AbstractSeedCob;


        protected override AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, AbstractPhysicalObjectState initialState)
        {
            var consumableDef = (OnlineSeedCobDefinition)newObjectEvent;
            var apo = base.ApoFromDef(newObjectEvent, inResource, initialState);
            RoomSettings roomsetting = new RoomSettings(apo.Room.name, apo.world.region, false, false, OnlineManager.lobby.gameMode.LoadWorldAs(apo.world.game));

            var asc = new SeedCob.AbstractSeedCob(apo.world, apo.realizedObject, apo.pos, apo.ID, consumableDef.originRoom, consumableDef.placedObjectIndex, consumableDef.originallyDead,
                roomsetting.placedObjects[consumableDef.placedObjectIndex].data as PlacedObject.ConsumableObjectData)
            { isConsumed = (initialState as OnlineConsumableState).isConsumed };
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

        public class OnlineSeedCobState : OnlineConsumableState
        {
            [OnlineField]
            bool opened;
            [OnlineField]
            bool spawnedUtility;

            public OnlineSeedCobState() { }

            public OnlineSeedCobState(OnlineSeedCob onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                opened = onlineEntity.AbstractSeedCob.opened;
                spawnedUtility = onlineEntity.AbstractSeedCob.spawnedUtility;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var onlineSeedCob = onlineEntity as OnlineSeedCob;
                var seedCob = onlineSeedCob.AbstractSeedCob;
                if (onlineSeedCob.apo.realizedObject is SeedCob realizedSeedCob)
                {
                    if (spawnedUtility && !seedCob.spawnedUtility)
                    {
                        RainMeadow.Debug("seedcob spawnfood");
                        realizedSeedCob.spawnUtilityFoods();
                    }

                    if (opened && !seedCob.opened)
                    {
                        RainMeadow.Debug("seedcob open");
                        realizedSeedCob.Open();
                    }
                }
                else
                {
                    seedCob.opened = opened;
                    seedCob.spawnedUtility = spawnedUtility;
                    seedCob.dead = seedCob.dead || opened || spawnedUtility;
                }
            }
        }
    }
}
