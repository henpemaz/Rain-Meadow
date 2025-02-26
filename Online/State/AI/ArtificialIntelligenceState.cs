
using RainMeadow.Generics;

namespace RainMeadow {
    class ArtificialIntelligenceState : OnlineState {

        [OnlineField(group: "AImodules", nullable: true, polymorphic = true)]
        public DynamicOrderedStates<AIModuleState>? moduleStates;

        ArtificialIntelligenceState() {

        }

        void ReadTo(ArtificialIntelligence AI) {
            if (moduleStates is not null)
            foreach (var state in moduleStates.list) {
                var type = state.GetType();
                if (AI.modules.ha)
            }
        }
    }
}