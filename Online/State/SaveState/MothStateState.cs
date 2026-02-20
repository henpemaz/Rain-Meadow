using RWCustom;
using Watcher;

namespace RainMeadow
{
    public class MothStateState : CreatureStateState
    {
        [OnlineField]
        byte tagOwner;
        [OnlineFieldHalf]
        float babyness;
        [OnlineField]
        bool tagged;
        [OnlineField]
        int hatchedCycle;
        [OnlineField]
        int foodStored;

        public MothStateState() {}

        public MothStateState(OnlineCreature onlineCreature)
        {
            var abstractCreature = (AbstractCreature)onlineCreature.apo;
            var state = (BigMoth.BigMothState)abstractCreature.state;

            tagOwner = (byte)state.tagOwner;
            babyness = state.babyness;
            tagged = state.tagged;
            hatchedCycle = state.hatchedCycle;
            foodStored = state.foodStored;
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var state = (BigMoth.BigMothState)abstractCreature.state;

            if (ExtEnumBase.TryParse(typeof(BigMoth.BigMothState.TagOwner), BigMoth.BigMothState.TagOwner.values.GetEntry(tagOwner), true, out var result));
                state.tagOwner = (BigMoth.BigMothState.TagOwner)result;

            state.babyness = babyness;
            state.tagged = tagged;
            state.hatchedCycle = hatchedCycle;
            state.foodStored = foodStored;
        }
    }
}
