using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using static RainMeadow.Serializer;
using static RainMeadow.WorldSession;

namespace RainMeadow
{
    public partial class WorldSession
    {
        public struct RainCycleData : ICustomSerializable
        {
            public int cycleLength = 0;
            public int timer = 0;
            public int preTimer = 0;
            public bool delta = false;

            public RainCycleData(RainCycle rainCycle) {
                this.cycleLength = rainCycle.cycleLength;
                this.timer = rainCycle.timer;
                this.preTimer = rainCycle.preTimer;
                this.delta = true;
            }
            public void WriteDelta(RainCycleData newData) {
                if (newData.delta) {
                    this.cycleLength = newData.cycleLength;
                    this.timer = newData.timer;
                    this.preTimer = newData.preTimer;
                    this.delta = newData.delta;
                }
            }
            public void UpdateDelta(RainCycleData newData) {
                if (!this.Equals(newData))
                {
                    this.cycleLength = newData.cycleLength;
                    this.timer = newData.timer;
                    this.preTimer = newData.preTimer;
                    this.delta = true;
                }
                else {
                    this.delta = false;
                }
            }
            public override bool Equals(object obj)
            {
                if (obj is RainCycleData) {
                    var rainCycle = (RainCycleData)obj;
                    return (this.cycleLength == rainCycle.cycleLength && this.timer == rainCycle.timer && this.preTimer == rainCycle.preTimer);
                }
                return false;
            }
            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref cycleLength);
                serializer.Serialize(ref timer);
                serializer.Serialize(ref preTimer);
                serializer.Serialize(ref delta);
            }
        }
    }
}
