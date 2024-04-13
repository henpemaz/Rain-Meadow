using System;

namespace RainMeadow
{
    internal class MeadowWorldData : OnlineResource.ResourceData
    {
        internal float[] roomWeights;
        internal AbstractRoom[] validRooms;

        public MeadowWorldData(OnlineResource resource) : base(resource) { }
    }
}