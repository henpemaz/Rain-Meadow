
using System.Collections.Generic;
using RainMeadow.Generics;

namespace RainMeadow {
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class ArtificialIntelligenceState : OnlineState {

        [OnlineField(group: "AImodules", nullable: true)]
        public DynamicOrderedStates<AIModuleState>? moduleStates;
        public ArtificialIntelligenceState(OnlineCreature onlineCreature) {
            if (onlineCreature.apo is AbstractCreature creature) {
                if (creature.creatureTemplate.AI && creature.abstractAI.RealAI is not null) {
                    InitializefromAI(creature.abstractAI.RealAI);
                }
            }
        }
        public void InitializefromAI(ArtificialIntelligence artificialIntelligence) {
            List<AIModuleState> moduleStatesList = new();
            foreach (AIModule module in artificialIntelligence.modules) {
                var state = GetModuleState(module);
                if (state is not null) {
                    moduleStatesList.Add(state);
                }
            }

            moduleStates = new DynamicOrderedStates<AIModuleState>(moduleStatesList);
        }

        public AIModuleState? GetModuleState(AIModule module) {
            if (module is FriendTracker) return new FriendTrackerState(module);
            return null;
        }

        public ArtificialIntelligenceState() {}

        public void ReadTo(ArtificialIntelligence AI) {
            if (moduleStates is not null)
            foreach (var state in moduleStates.list) {
                foreach (var aimodule in AI.modules) {
                    if (aimodule.GetType() == state.ModuleType) {
                        state.ReadTo(aimodule);
                    }
                }
            }
        }
    }
}