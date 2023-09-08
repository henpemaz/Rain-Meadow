namespace RainMeadow
{
    public class AbstractCreatureState : PhysicalObjectEntityState
    {
        [OnlineField(polymorphic = true)]
        private CreatureStateState creatureStateState;
        public AbstractCreatureState() : base() { }
        public AbstractCreatureState(OnlineCreature onlineEntity, uint ts, bool realizedState) : base(onlineEntity, ts, realizedState)
        {
            creatureStateState = GetCreatureStateState(onlineEntity);
        }

        protected virtual CreatureStateState GetCreatureStateState(OnlineCreature onlineCreature)
        {
            if ((onlineCreature.apo as AbstractCreature).state is HealthState) return new CreatureHealthStateState(onlineCreature);
            return new CreatureStateState(onlineCreature);
        }

        protected override RealizedPhysicalObjectState GetRealizedState(OnlinePhysicalObject onlineObject)
        {
            if (onlineObject.apo.realizedObject is Player) return new RealizedPlayerState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Overseer) return new RealizedOverseerState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Creature) return new RealizedCreatureState((OnlineCreature)onlineObject);
            return base.GetRealizedState(onlineObject);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var abstractCreature = (AbstractCreature)((OnlineCreature)onlineEntity).apo;

            if (creatureStateState is CreatureStateState newState)
            {
                abstractCreature.state.alive = newState.alive;
                abstractCreature.state.meatLeft = newState.meatLeft;
                if (abstractCreature.realizedCreature is Creature realCreature)
                {
                    realCreature.dead = !newState.alive;
                }
            }
            if (creatureStateState is CreatureHealthStateState healthStateState)
            {
                var healthState = (HealthState)abstractCreature.state;
                healthState.health = healthStateState.health;
            }
        }

        public override OnlineState Delta(OnlineState baseline)
        {
            try
            {
                return base.Delta(baseline);
            }
            catch (System.Exception e)
            {
                RainMeadow.Debug(this.baseline);
                RainMeadow.Debug(this.creatureStateState);
                RainMeadow.Debug(this.entityId);
                RainMeadow.Debug(this.from);
                RainMeadow.Debug(this.handler);
                RainMeadow.Debug(this.isDelta);
                RainMeadow.Debug(this.pos);
                RainMeadow.Debug(this.realized);
                RainMeadow.Debug(this.realizedObjectState);
                RainMeadow.Debug(this.tick);
                RainMeadow.Debug(this.valueFlags);
                throw;
            }
        }
    }
}