using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class JokeRifleState : RealizedPhysicalObjectState
    {
        [OnlineField(group = "counters")]
        int counter;

        [OnlineField]
        byte ammoStyle;
        [OnlineField]
        Generics.ByteToUshortDict ammo;

        public JokeRifleState() {}

        public JokeRifleState(OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            var rifle = (JokeRifle)onlineEntity.apo.realizedObject;

            counter = rifle.counter;

            ammoStyle = AmmoStyle(rifle.abstractRifle.ammoStyle);
            //ammo = rifle.abstractRifle.ammo.ToDictionary(x => AmmoStyle(x.Key), x => x.Value);
            ammo = new(rifle.abstractRifle.ammo.ToDictionary(k => AmmoStyle(k.Key), v => (ushort)v.Value).ToList());

        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);

            var rifle = (JokeRifle)((OnlinePhysicalObject)onlineEntity).apo.realizedObject;

            rifle.counter = counter;

            rifle.abstractRifle.ammoStyle = AmmoStyle(ammoStyle);
            //rifle.abstractRifle.ammo = ammo.ToDictionary(x => AmmoStyle(x.Key), x => x.Value);
            rifle.abstractRifle.ammo = ammo.list.ToDictionary(k => AmmoStyle(k.Key), v => (int)v.Value);
        }

        // please lmk if theres a better way to do this I'm beggin you
        private byte AmmoStyle(JokeRifle.AbstractRifle.AmmoType ammoType)
        {
            switch(ammoType.value)
            {
                default: return 0;
                case "Grenade": return 1;
                case "Firecracker": return 2;
                case "Pearl": return 3;
                case "Light": return 4;
                case "Ash": return 5;
                case "Bees": return 6;
                case "Void": return 7;
                case "Fruit": return 8;
                case "Noodle": return 9;
                case "FireEgg": return 10;
                case "Singularity": return 11;
            }
        }

        private JokeRifle.AbstractRifle.AmmoType AmmoStyle(byte ammoType)
        {
            switch ((int)ammoType)
            {
                default: return JokeRifle.AbstractRifle.AmmoType.Rock;
                case 1: return JokeRifle.AbstractRifle.AmmoType.Grenade;
                case 2: return JokeRifle.AbstractRifle.AmmoType.Firecracker;
                case 3: return JokeRifle.AbstractRifle.AmmoType.Pearl;
                case 4: return JokeRifle.AbstractRifle.AmmoType.Light;
                case 5: return JokeRifle.AbstractRifle.AmmoType.Ash;
                case 6: return JokeRifle.AbstractRifle.AmmoType.Bees;
                case 7: return JokeRifle.AbstractRifle.AmmoType.Void;
                case 8: return JokeRifle.AbstractRifle.AmmoType.Fruit;
                case 9: return JokeRifle.AbstractRifle.AmmoType.Noodle;
                case 10: return JokeRifle.AbstractRifle.AmmoType.FireEgg;
                case 11: return JokeRifle.AbstractRifle.AmmoType.Singularity;
            }
        }
    }
}
