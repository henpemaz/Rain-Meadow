﻿using RainMeadow;
using static RainMeadow.OnlineState;

namespace RainMeadow
{
    public class PlayerStateState : CreatureStateState
    {
        [OnlineField]
        public int foodInStomach;
        [OnlineField]
        public int quarterFoodPoints;
        [OnlineField(nullable:true)]
        public OnlineEntity.EntityId? objectInStomach;

        public PlayerStateState() { }

        public PlayerStateState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            var playerState = (PlayerState)abstractCreature.state;

            foodInStomach = playerState.foodInStomach;
            quarterFoodPoints = playerState.quarterFoodPoints;

            if ((abstractCreature.realizedCreature as Player)?.objectInStomach is AbstractPhysicalObject apo)
            {
                apo.Move(abstractCreature.pos);
                RainMeadow.Debug($"objectInStomach is {apo}");
                if (!OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                {
                    apo.world.GetResource().ApoEnteringWorld(apo);
                    if (!OnlinePhysicalObject.map.TryGetValue(apo, out oe)) throw new System.InvalidOperationException("Stomach item doesn't exist in online space!");
                }
                RainMeadow.Debug($"onlineEntity is {oe}");
                objectInStomach = oe.id;
            }
            else
            {
                RainMeadow.Debug("objectInStomach is null");
                objectInStomach = null;
            }
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var playerState = (PlayerState)abstractCreature.state;

            playerState.foodInStomach = this.foodInStomach;
            playerState.quarterFoodPoints = this.quarterFoodPoints;

            if (abstractCreature.realizedCreature is Player player)
            {
                player.objectInStomach = (this.objectInStomach?.FindEntity() as OnlinePhysicalObject)?.apo;
            }
        }
    }
}
