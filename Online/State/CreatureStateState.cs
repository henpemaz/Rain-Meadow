using RainMeadow.Generics;
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
    }
}