using RainMeadow.Generics;
using System;
using System.Collections.Generic;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.FollowsContainer)]
    public class CreatureStateState : OnlineState
    {
        // main part of AbstractCreatureState
        [OnlineField]
        public bool alive;
        [OnlineField]
        public byte meatLeft;

        public CreatureStateState() { }
        public CreatureStateState(OnlineCreature onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            alive = abstractCreature.state.alive;
            meatLeft = (byte)abstractCreature.state.meatLeft;
        }

        public virtual void ReadTo(AbstractCreature abstractCreature)
        {
            abstractCreature.state.alive = this.alive;
            abstractCreature.state.meatLeft = this.meatLeft;
            if (abstractCreature.realizedCreature is Creature realCreature)
            {
                realCreature.dead = !this.alive;
            }
        }
    }
}