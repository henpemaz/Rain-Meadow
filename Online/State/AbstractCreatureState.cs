namespace RainMeadow
{
    public class AbstractCreatureState : PhysicalObjectEntityState
    {
        private CreatureStateState creatureStateState;

        bool hasStateState;

        public override EntityState EmptyDelta() => new AbstractCreatureState();
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

        public override StateType stateType => StateType.AbstractCreatureState;

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

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            if (IsDelta) serializer.Serialize(ref hasStateState);
            if (!IsDelta || hasStateState)
            {
                serializer.SerializePolyState(ref creatureStateState);
            }
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            return base.EstimatedSize(inDeltaContext) + creatureStateState.EstimatedSize(inDeltaContext);
        }

        public override EntityState Delta(EntityState _other)
        {
            var delta = (AbstractCreatureState)base.Delta(_other);
            var other = (AbstractCreatureState)_other;
            delta.creatureStateState = creatureStateState.Delta(other.creatureStateState);
            delta.hasStateState = !delta.creatureStateState.IsEmptyDelta;
            delta.IsEmptyDelta &= !delta.hasStateState;
            return delta;
        }

        public override EntityState ApplyDelta(EntityState _other)
        {
            var result = (AbstractCreatureState)base.ApplyDelta(_other);
            var other = (AbstractCreatureState)_other;
            result.creatureStateState = other.hasStateState ? creatureStateState.ApplyDelta(other.creatureStateState) : creatureStateState;
            return result;
        }
    }
}