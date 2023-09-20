using RWCustom;
using UnityEngine;

namespace RainMeadow;

public class PlayerEvent
{
    public class AddFood : OnlineEvent
    {
        private short add;

        public AddFood() { }
        public AddFood(int add)
        {
            this.add = (short)add;
        }
        public override EventTypeId eventType => EventTypeId.PlayerAddFood;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref add);
        }

        public override void Process()
        {
            ((RWCustom.Custom.rainWorld.processManager.currentMainLoop as RainWorldGame)?.Players[0].realizedCreature as Player).AddFood(add);
        }
    }

}
