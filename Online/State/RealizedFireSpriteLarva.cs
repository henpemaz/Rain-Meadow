using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Watcher;

namespace RainMeadow
{
    public class RealizedFireSpriteLarva : RealizedPhysicalObjectState
    {
        [OnlineField]
        byte bites = 3;
        public RealizedFireSpriteLarva() { }
        public RealizedFireSpriteLarva(OnlinePhysicalObject onlineEntity)
        {
            var larva = (BoxWorm.Larva)onlineEntity.apo.realizedObject;

            this.bites = (byte)larva.bites;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            var larva = (BoxWorm.Larva)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            larva.bites = bites;
        }
    }
}
