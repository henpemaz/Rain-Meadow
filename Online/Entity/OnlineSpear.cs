namespace RainMeadow
{
    public class OnlineSpear : OnlinePhysicalObject
    {
        public class OnlineSpearDefinition : OnlinePhysicalObjectDefinition
        {
            [OnlineField]
            public bool explosive; // arti spawns a new AbstractSpear
            //[OnlineField]
            //public float hue;
            [OnlineField]
            public bool electric;
            [OnlineField]
            public bool needle; // arti spawns a new AbstractSpear

            public OnlineSpearDefinition() { }

            public OnlineSpearDefinition(OnlineSpear onlineSpear, OnlineResource inResource) : base(onlineSpear, inResource)
            {
                this.explosive = onlineSpear.AbstractSpear.explosive;
                //this.hue = onlineSpear.AbstractSpear.hue;
                this.electric = onlineSpear.AbstractSpear.electric;
                this.needle = onlineSpear.AbstractSpear.needle;
            }

            public override OnlineEntity MakeEntity(OnlineResource inResource, OnlineEntity.EntityState initialState)
            {
                return new OnlineSpear(this, inResource, (OnlineSpearState)initialState);
            }
        }

        public OnlineSpear(OnlineSpearDefinition entityDefinition, OnlineResource inResource, OnlineSpearState initialState) : base(entityDefinition, inResource, initialState)
        {

        }

        public OnlineSpear(AbstractSpear ac, EntityId id, OnlinePlayer owner, bool isTransferable) : base(ac, id, owner, isTransferable)
        {

        }

        public AbstractSpear AbstractSpear => apo as AbstractSpear;

        protected override AbstractPhysicalObject ApoFromDef(OnlinePhysicalObjectDefinition newObjectEvent, OnlineResource inResource, PhysicalObjectEntityState initialState)
        {
            var entityDefinition = (OnlineSpearDefinition)newObjectEvent;
            var asp = (AbstractSpear)base.ApoFromDef(newObjectEvent, inResource, initialState);
            asp.explosive = entityDefinition.explosive;
            //asp.hue = entityDefinition.hue;
            asp.electric = entityDefinition.electric;
            asp.needle = entityDefinition.needle;
            asp.stuckInWallCycles = (initialState as OnlineSpearState).stuckInWallCycles;
            asp.electricCharge = (initialState as OnlineSpearState).electricCharge;

            if (asp.needle && asp.realizedObject is Spear spear)
            {
                spear.Spear_makeNeedle(UnityEngine.Random.Range(0, 3), active: true);
            }

            return asp;
        }

        internal override EntityDefinition MakeDefinition(OnlineResource onlineResource)
        {
            return new OnlineSpearDefinition(this, onlineResource);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new OnlineSpearState(this, inResource, tick);
        }

        public class OnlineSpearState : PhysicalObjectEntityState
        {
            [OnlineField]
            public sbyte stuckInWallCycles;
            [OnlineField]
            public byte electricCharge;

            public OnlineSpearState() { }

            public OnlineSpearState(OnlineSpear onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
            {
                stuckInWallCycles = (sbyte)onlineEntity.AbstractSpear.stuckInWallCycles;
                electricCharge = (byte)onlineEntity.AbstractSpear.electricCharge;
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                if ((onlineEntity as OnlineSpear).apo is AbstractSpear asp)
                {
                    asp.stuckInWallCycles = stuckInWallCycles;
                    asp.electricCharge = electricCharge;
                }
            }
        }
    }
}
