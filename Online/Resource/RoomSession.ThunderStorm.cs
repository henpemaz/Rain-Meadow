using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Watcher;

namespace RainMeadow;

public partial class RoomSession
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class LethalThunderStormData : OnlineState
    {
        [OnlineField(group = "counters")]
        int lethal;
        [OnlineField]
        int lethalMax;
        [OnlineField(group = "counters")]
        int cosmetic;
        [OnlineField]
        int cosmeticMax;
        public LethalThunderStormData() { }
        public LethalThunderStormData(LethalThunderStorm storm)
        {
            lethalMax = storm.lethal.max;
            lethal = storm.lethal;

            cosmeticMax = storm.cosmetic.max;
            cosmetic = storm.cosmetic;
        }

        public void ReadTo(LethalThunderStorm storm)
        {
            storm.lethal.SetMax(Mathf.Clamp(lethalMax, 120, 1480));
            storm.lethal.SetClamped(lethal);

            storm.cosmetic.SetMax(Mathf.Clamp(cosmeticMax, 20, 4920));
            storm.cosmetic.SetClamped(cosmetic);
        }
    }
}
