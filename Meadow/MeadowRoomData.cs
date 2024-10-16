using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // no sync only data
    internal class MeadowRoomData : OnlineResource.ResourceData
    {
        private List<Place> places = new();

        public int NumberOfPlaces => places.Count;

        public override ResourceDataState MakeState(OnlineResource resource) => null; // no state

        internal void AddItemPlacement(int x, int y, bool rare)
        {
            places.Add(new Place(x, y, rare));
            RainMeadow.Debug("place added! " + places[places.Count - 1].pos);
        }

        internal IntVector2 GetUnusedPlace(Room placeRoom)
        {
            if (places.Count == 0)
            {
                throw new Exception("no places!");
            }
            var usedPlaces = placeRoom.abstractRoom.entities.Select(e => e.pos.Tile).ToHashSet();
            var unusedPlaces = places.Where(p => !usedPlaces.Contains(p.pos)).ToList();
            if (unusedPlaces.Count > 0)
            {
                return unusedPlaces[UnityEngine.Random.Range(0, unusedPlaces.Count)].pos;
            }
            return places[UnityEngine.Random.Range(0, places.Count)].pos;
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