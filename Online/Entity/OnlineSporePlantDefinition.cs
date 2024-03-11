using System.Collections.Generic;

namespace RainMeadow 
{ 
    public class OnlineSporePlantDefinition : OnlineConsumableDefinition
    {
        [OnlineField]
        public bool originallyPacified;
        [OnlineField]
        public bool originallyUsed;
        public OnlineSporePlantDefinition() { }

        public OnlineSporePlantDefinition(OnlineConsumableDefinition ocd, SporePlant.AbstractSporePlant abstractSporePlant) : base(ocd)
        {
            this.originallyPacified = abstractSporePlant.pacified;
            this.originallyUsed = abstractSporePlant.used;
        }

        public override OnlineEntity MakeEntity(OnlineResource inResource)
        {
            return OnlineSporePlant.FromDefinition(this, inResource);
        }
    }
}
