
using System;
using System.Collections.Generic;

namespace RainMeadow {
    abstract class AIModuleState : OnlineState {
        abstract public Type ModuleType { get; }
        abstract public void ReadTo(AIModule module);
    }

    

}
