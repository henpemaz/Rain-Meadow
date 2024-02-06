using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowRoomData : OnlineResource.ResourceData
    {
        private List<Place> places = new();

        public MeadowRoomData(OnlineResource resource) : base(resource) { }

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

        internal override ResourceDataState MakeState()
        {
            return new State(this);
        }

        internal class State : ResourceDataState
        {
            [OnlineField]
            bool dummy;
            public State() { }
            public State(MeadowRoomData meadowRoomData) { }

            internal override Type GetDataType() => typeof(MeadowRoomData);

            internal override void ReadTo(OnlineResource.ResourceData data) { }
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