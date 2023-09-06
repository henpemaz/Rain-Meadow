using RainMeadow.Generics;
using System.Collections.Generic;

namespace RainMeadow
{
    public class CreatureStateState : OnlineState
    {
        // main part of AbstractCreatureState
        public bool alive;
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