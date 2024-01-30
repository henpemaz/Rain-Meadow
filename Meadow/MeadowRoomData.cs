using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowRoomData : OnlineResource.ResourceData
    {
        private List<Place> places = new();

        public int NumberOfPlaces => places.Count;

        internal void AddItemPlacement(int x, int y, bool rare)
        {
            places.Add(new Place(x, y, rare));
            RainMeadow.Debug("place added! " + places[places.Count - 1].pos);
        }

        internal IntVector2 GetUnusedPlace(Room placeRoom)
        {
            if(places.Count == 0) {
                throw new Exception("no places!");
            }
            var index = UnityEngine.Random.Range(0, places.Count);
            return places[index].pos;
        }

        internal override OnlineResource.ResourceDataState MakeState(OnlineResource inResource)
        {
            return new MeadowRoomState(this);
        }

        internal class MeadowRoomState : OnlineResource.ResourceDataState
        {
            [OnlineField]
            bool dummy;
            public MeadowRoomState() { }
            public MeadowRoomState(MeadowRoomData meadowRoomData)
            {

            }

            internal override void ReadTo(OnlineResource onlineResource)
            {
                
            }
        }

        private class Place
        {
            public IntVector2 pos;
            private bool rare;

            public Place(int x, int y, bool rare)
            {
                this.pos = new(x, y);
                this.rare = rare;
            }
        }
    }
}