using System.Linq;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedBoxWormState : RealizedCreatureState
    {
        [OnlineField]
        Generics.DynamicOrderedStates<LarvaHolderState> larvaHolders;
        [OnlineField (group = "counters")]
        int attackTimer;
        [OnlineField]
        int steamAvailable;

        public RealizedBoxWormState() { }

        public RealizedBoxWormState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var boxWorm = onlineCreature.realizedCreature as BoxWorm;

            larvaHolders = new(boxWorm.larvaHolders.Select(x => new LarvaHolderState(x)).ToList());

            attackTimer = boxWorm.attackTimer;
            steamAvailable = boxWorm.steamAvailable;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not BoxWorm boxWorm) return;

            for (int i = 0; i < larvaHolders.list.Count; i++)
            {
                larvaHolders.list[i].ReadTo(boxWorm.larvaHolders[i]);
            }

            boxWorm.attackTimer.SetClamped(attackTimer);
            boxWorm.steamAvailable.SetClamped(steamAvailable);
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class LarvaHolderState : OnlineState
    {
        [OnlineField]
        bool forceRelease;
        [OnlineField]
        bool retracted;
        [OnlineField]
        int timeToDislodge;

        public LarvaHolderState() { }
        public LarvaHolderState(BoxWorm.LarvaHolder holder)
        {
            forceRelease = holder.forceRelease;
            retracted = holder.retracted;
            timeToDislodge = holder.timeToDislodge;
        }

        public void ReadTo(BoxWorm.LarvaHolder holder)
        {
            holder.forceRelease = forceRelease;
            holder.retracted = retracted;
            holder.timeToDislodge.SetClamped(timeToDislodge);
        }
    }
}
