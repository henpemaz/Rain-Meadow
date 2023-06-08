using UnityEngine;

namespace RainMeadow
{
    public class AbstractCreatureState : PhysicalObjectEntityState
    {
        public OnlineCreature onlineCreature => onlineEntity as OnlineCreature;
        private CreatureStateState creatureStateState;
     
        public AbstractCreatureState() : base()
        {
        }

        public AbstractCreatureState(OnlineCreature onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            if (realizedState) creatureStateState = GetCreatureStateState(abstractCreature);
        }

        private CreatureStateState GetCreatureStateState(AbstractCreature abstractCreature)
        {
            if (abstractCreature.state is HealthState) return new CreatureHealthStateState(onlineCreature);
            return new CreatureStateState(onlineCreature);
        }

        protected override RealizedPhysicalObjectState GetRealizedState()
        {
            if (onlineCreature.apo.realizedObject is Player) return new RealizedPlayerState(onlineCreature);
            if (onlineCreature.apo.realizedObject is Creature) return new RealizedCreatureState(onlineCreature);
            return base.GetRealizedState();
        }

        public override StateType stateType => StateType.AbstractCreatureState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeNullablePolyState(ref creatureStateState);
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
    }
}