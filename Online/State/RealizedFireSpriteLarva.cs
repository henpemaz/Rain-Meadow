using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watcher;

namespace RainMeadow
{
    public class RealizedFireSpriteLarva : RealizedState<BoxWorm.Larva>
    {
        [OnlineField]
        byte bites = 3;
        public RealizedFireSpriteLarva() { }
        public RealizedFireSpriteLarva(OnlinePhysicalObject onlineEntity)
        {
            var larva = (BoxWorm.Larva)onlineEntity.apo.realizedObject;

            this.bites = (byte)larva.bites;
        }

        public override void ReadTo(BoxWorm.Larva larva, OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            larva.bites = bites;
        }
    }
}
