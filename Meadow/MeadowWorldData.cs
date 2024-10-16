using System;

namespace RainMeadow
{
    // no sync only data
    internal class MeadowWorldData : OnlineResource.ResourceData
    {
        internal float[] roomWeights;
        internal AbstractRoom[] validRooms;

        public override ResourceDataState MakeState(OnlineResource resource) => null;
    }
}