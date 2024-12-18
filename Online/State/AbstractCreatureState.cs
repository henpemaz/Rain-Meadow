namespace RainMeadow
{
    public class AbstractCreatureState : AbstractPhysicalObjectState
    {
        [OnlineField(polymorphic = true)]
        public CreatureStateState creatureStateState;
        [OnlineField(group = "realized")]
        public WorldCoordinate destination;

        public AbstractCreatureState() : base() { }
        public AbstractCreatureState(OnlineCreature onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            creatureStateState = GetCreatureStateState(onlineEntity);
            if (onlineEntity.creature.abstractAI is AbstractCreatureAI absAi)
            {
                destination = absAi.destination;
            }
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
            if (onlineObject.apo.realizedObject is TubeWorm) return new RealizedTubeWormState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is GarbageWorm) return new RealizedGarbageWormState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is DaddyLongLegs) return new RealizedDaddyLongLegsState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Deer) return new RealizedDeerState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is DropBug) return new RealizedDropBugState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Scavenger) return new RealizedScavengerState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Lizard) return new RealizedLizardState((OnlineCreature)onlineObject);
            if (onlineObject.apo.realizedObject is Creature) return new RealizedCreatureState((OnlineCreature)onlineObject);
            return base.GetRealizedState(onlineObject);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var abstractCreature = (AbstractCreature)((OnlineCreature)onlineEntity).apo;
            creatureStateState.ReadTo(abstractCreature);
            if (abstractCreature.abstractAI is AbstractCreatureAI absAi)
            {
                if (destination.room != absAi.destination.room || destination.abstractNode != absAi.destination.abstractNode)
                {
                    absAi.SetDestinationNoPathing(destination, migrate: true);
                }
            }
        }
    }
}
