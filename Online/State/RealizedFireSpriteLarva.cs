using System.Runtime.CompilerServices;
using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedFireSpriteLarva : RealizedPhysicalObjectState
    {
        public static ConditionalWeakTable<BoxWorm.Larva, BoxWorm.LarvaHolder> themoddershavebeenlefttostarve = new();

        [OnlineField]
        byte bites = 3;
        [OnlineField]
        bool edible;

        [OnlineField(nullable = true)]
        LarvaHolderState? holderState;

        public RealizedFireSpriteLarva() { }
        public RealizedFireSpriteLarva(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var larva = (BoxWorm.Larva)onlineEntity.apo.realizedObject;

            bites = (byte)larva.bites;
            edible = larva.edible;

            BoxWorm.LarvaHolder? holder = HolderFromBoxWorm(larva);
            if (holder != null)
            {
                holderState = new(holder);
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var larva = (BoxWorm.Larva)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            larva.bites = bites;
            larva.edible = edible;

            BoxWorm.LarvaHolder? holder = HolderFromBoxWorm(larva);
            if (holder != null)
            {
                holderState?.ReadTo(holder);
            }
        }

        public BoxWorm.LarvaHolder? HolderFromBoxWorm(BoxWorm.Larva larva)
        {
            if (themoddershavebeenlefttostarve.TryGetValue(larva, out var holder))
            {
                return holder;
            }
            return null;
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class LarvaHolderState : OnlineState
    {
        [OnlineField(nullable = true)]
        OnlinePhysicalObject? onlineLarva;
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

            onlineLarva = holder.abstractLarva?.GetOnlineObject();
        }

        public void ReadTo(BoxWorm.LarvaHolder holder)
        {
            holder.forceRelease = forceRelease;
            holder.retracted = retracted;
            holder.timeToDislodge.SetClamped(timeToDislodge);

            var larva = onlineLarva?.apo?.realizedObject;
            if (larva?.abstractPhysicalObject != holder.abstractLarva)
            {
                if (larva?.abstractPhysicalObject is BoxWorm.Larva.AbstractLarva abstractLarva)
                {
                    holder.abstractLarva = abstractLarva;
                }
                else
                {
                    larva = null;
                }
            }
        }
    }
}
