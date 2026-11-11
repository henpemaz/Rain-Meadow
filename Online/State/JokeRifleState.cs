using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class JokeRifleState : RealizedPhysicalObjectState
    {
        [OnlineField(group = "counters")]
        int counter;

        [OnlineField]
        JokeRifle.AbstractRifle.AmmoType ammoStyle;
        [OnlineField]
        ushort[] ammo;

        public JokeRifleState() {}

        public JokeRifleState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var rifle = (JokeRifle)onlineEntity.apo.realizedObject;

            counter = rifle.counter;

            ammoStyle = rifle.abstractRifle.ammoStyle;
            ammo = rifle.abstractRifle.ammo.Values.Select(x => (ushort)x).ToArray();
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var rifle = (JokeRifle)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            rifle.counter = counter;

            rifle.abstractRifle.ammoStyle = ammoStyle;
            int index = 0;
            foreach (var entry in ExtEnum<JokeRifle.AbstractRifle.AmmoType>.values.entries)
            {
                rifle.abstractRifle.ammo[new JokeRifle.AbstractRifle.AmmoType(entry, false)] = ammo[index];
                index++;
            }
        }
    }
}
