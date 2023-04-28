using UnityEngine;

namespace RainMeadow
{
    public class AbstractCreatureState : PhysicalObjectEntityState
    {
        private OnlineState creatureStateState;
     
        public AbstractCreatureState() : base()
        {
        }

        public AbstractCreatureState(OnlineEntity onlineEntity, ulong ts, bool realizedState) : base(onlineEntity, ts, realizedState)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.entity;
            if (realizedState) creatureStateState = GetCreatureStateState(abstractCreature);
        }

        private CreatureStateState GetCreatureStateState(AbstractCreature abstractCreature)
        {
            if (abstractCreature.state is HealthState) return new CreatureHealthStateState(onlineEntity);
            return new CreatureStateState(onlineEntity);
        }

        protected override PhysicalObjectState GetRealizedState()
        {
            if (onlineEntity.entity.realizedObject is Player) return new RealizedPlayerState(onlineEntity);
            if (onlineEntity.entity.realizedObject is Creature) return new RealizedCreatureState(onlineEntity);
            return base.GetRealizedState();
        }

        public override StateType stateType => StateType.AbstractCreatureState;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeNullable(ref creatureStateState);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var abstractCreature = (AbstractCreature)onlineEntity.entity;
            
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