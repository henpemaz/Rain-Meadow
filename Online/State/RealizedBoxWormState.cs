using System.Linq;
using Watcher;

namespace RainMeadow
{
    public class RealizedBoxWormState : RealizedCreatureState
    {
        [OnlineField]
        Generics.DynamicOrderedStates<LarvaHolderState> larvaHolders;
        [OnlineField (group = "counters")]
        int attackTimer;
        [OnlineField(group = "counters")]
        int steamAvailable;

        public RealizedBoxWormState() { }

        public RealizedBoxWormState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var boxWorm = onlineCreature.apo.realizedObject as BoxWorm;

            larvaHolders = new(boxWorm.larvaHolders.Select(x => new LarvaHolderState(x)).ToList());

            attackTimer = boxWorm.attackTimer;
            steamAvailable = boxWorm.steamAvailable;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlinePhysicalObject).apo.realizedObject is not BoxWorm boxWorm) return;

            for(int i = 0; i < larvaHolders.list.Count; i++)
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

        public LarvaHolderState() { }
        public LarvaHolderState(BoxWorm.LarvaHolder holder)
        {
            forceRelease = holder.forceRelease;
            retracted = holder.retracted;
        }

        public void ReadTo(BoxWorm.LarvaHolder holder)
        {
            holder.forceRelease = forceRelease;
            holder.retracted = retracted;
        }
    }
}
