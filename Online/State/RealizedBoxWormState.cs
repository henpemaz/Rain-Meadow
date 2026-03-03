using System.Linq;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedBoxWormState : RealizedCreatureState
    {
        //[OnlineField]
        //Generics.DynamicOrderedStates<LarvaHolderState> larvaHolders;
        [OnlineField (group = "counters")]
        int attackTimer;
        [OnlineField(group = "counters")]
        int releaseSteamTimer;
        [OnlineField]
        int steamAvailable;

        public RealizedBoxWormState() { }

        public RealizedBoxWormState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var boxWorm = onlineCreature.realizedCreature as BoxWorm;

            attackTimer = boxWorm.attackTimer;
            releaseSteamTimer = boxWorm.releaseSteamTimer;
            steamAvailable = boxWorm.steamAvailable;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not BoxWorm boxWorm) return;

            boxWorm.attackTimer.SetClamped(attackTimer);
            boxWorm.releaseSteamTimer.SetClamped(releaseSteamTimer);
            boxWorm.steamAvailable.SetClamped(steamAvailable);
        }
    }

    // LarvaHolders Now handled by RealizedFireSpriteLarva
}
