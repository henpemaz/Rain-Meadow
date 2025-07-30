
using System.Collections.Generic;
using RainMeadow.Generics;

namespace RainMeadow {
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class ArtificialIntelligenceState : OnlineState
    {

        [OnlineField(group: "AImodules", nullable: true)]
        public DynamicOrderedStates<AIModuleState>? moduleStates;

        [OnlineField(nullable: true)]
        public OnlineEntity.EntityId? focusCreature;

        public ArtificialIntelligenceState() {}
        public ArtificialIntelligenceState(ArtificialIntelligence AI)
        {
            List<AIModuleState> moduleStatesList = new();
            foreach (AIModule module in AI.modules)
            {
                var state = GetModuleState(module);
                if (state is not null)
                {
                    moduleStatesList.Add(state);
                }
            }

            moduleStates = new DynamicOrderedStates<AIModuleState>(moduleStatesList);
        }

        public AIModuleState? GetModuleState(AIModule module)
        {
            if (module is FriendTracker) return new FriendTrackerState(module);
            return null;
        }


        public void ReadTo(ArtificialIntelligence AI)
        {
            if (moduleStates is not null)
            {
                foreach (var state in moduleStates.list)
                {
                    foreach (var aimodule in AI.modules)
                    {
                        if (aimodule.GetType() == state.ModuleType)
                        {
                            state.ReadTo(aimodule);
                            break;
                        }
                    }
                }
            }
        }
    }
}