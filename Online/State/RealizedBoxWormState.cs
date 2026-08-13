using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedBoxWormState : RealizedCreatureState
    {        
        [OnlineField(nullable = true)]
        Generics.DynamicOrderedStates<LarvaHolderState> larvaHolders;

        [OnlineField(group = "counters")]
        int attackTimer;
        [OnlineField(group = "counters")]
        int releaseSteamTimer;
        [OnlineField]
        int steamAvailable;
        

        public RealizedBoxWormState() { }

        public RealizedBoxWormState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var boxWorm = (BoxWorm)onlineCreature.realizedCreature;

            attackTimer = boxWorm.attackTimer;
            releaseSteamTimer = boxWorm.releaseSteamTimer;
            steamAvailable = boxWorm.steamAvailable;

            System.Collections.Generic.List<LarvaHolderState> LarvaHolders = new();
            for (int i = 0; i < boxWorm.larvaHolders.Length; i++)
            {
                if (boxWorm.larvaHolders[i].hasLarva)
                {
                    LarvaHolders.Add(new LarvaHolderState(boxWorm.larvaHolders[i], i));
                }
            }
            larvaHolders = new Generics.DynamicOrderedStates<LarvaHolderState>(LarvaHolders);
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (((OnlinePhysicalObject)onlineEntity).apo.realizedObject is not BoxWorm boxWorm) return;

            boxWorm.attackTimer.SetClamped(attackTimer);
            boxWorm.releaseSteamTimer.SetClamped(releaseSteamTimer);
            boxWorm.steamAvailable.SetClamped(steamAvailable);

            for (int i = 0; i < larvaHolders?.list?.Count; i++)
            {
                int index = larvaHolders.list[i].index;
                larvaHolders.list[i].ReadTo(boxWorm.larvaHolders[index]);
            }
        }      
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class LarvaHolderState : OnlineState
    {
        [OnlineField(nullable = true)]
        public OnlineEntity.EntityId onlineLarvaID;
        [OnlineField]
        public byte index;
       
        [OnlineField]
        byte timeToDislodge;

        public LarvaHolderState() { }
        public LarvaHolderState(BoxWorm.LarvaHolder holder, int index)
        {
            this.index = (byte)index;
            timeToDislodge = (byte)holder.timeToDislodge;
            if (holder.abstractLarva?.GetOnlineObject() is OnlinePhysicalObject opo)
                onlineLarvaID = opo.id;
        }
        public void ReadTo(BoxWorm.LarvaHolder holder)
        {
            holder.timeToDislodge.SetClamped(timeToDislodge);
            if (onlineLarvaID.FindEntity() is not OnlinePhysicalObject onlineLarva) return;
            if (onlineLarva.apo?.realizedObject is not Watcher.BoxWorm.Larva larva) return;
            if (holder.abstractLarva != larva.abstractPhysicalObject)
            {
                holder.abstractLarva = (BoxWorm.Larva.AbstractLarva)onlineLarva.apo;
                holder.hasLarva = true;
            }
        }
    }
}