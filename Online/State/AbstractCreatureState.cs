namespace RainMeadow
{
    public class AbstractCreatureState : PhysicalObjectEntityState
    {
        [OnlineField(polymorphic = true)]
        private CreatureStateState creatureStateState;
        public AbstractCreatureState() : base() { }
        public AbstractCreatureState(OnlineCreature onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            creatureStateState = GetCreatureStateState(onlineEntity);
        }

        protected virtual CreatureStateState GetCreatureStateState(OnlineCreature onlineCreature)
        {
            if ((onlineCreature.apo as AbstractCreature).state is HealthState) return new CreatureHealthStateState(onlineCreature);
            if ((onlineCreature.apo as AbstractCreature).state is PlayerState) return new PlayerStateState(onlineCreature);
            return new CreatureStateState(onlineCreature);
        }

        protected override RealizedPhysicalObjectState GetRealizedState(OnlinePhysicalObject onlineObject)
        {
            if (onlineObject.apo.realizedObject is Player) return new RealizedPlayerState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Overseer) return new RealizedOverseerState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Fly) return new RealizedFlyState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Creature) return new RealizedCreatureState((OnlineCreature)onlineObject);
            return base.GetRealizedState(onlineObject);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var abstractCreature = (AbstractCreature)((OnlineCreature)onlineEntity).apo;
            creatureStateState.ReadTo(abstractCreature);
        }
    }
}