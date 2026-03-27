using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RainMeadow
{
    class FastVec2Comparer : IEqualityComparer<IntVector2>
    {
        public bool Equals(IntVector2 a, IntVector2 b)
            => a.x == b.x && a.y == b.y;

        public int GetHashCode(IntVector2 v)
        {
            return (v.x * 73856093) ^ (v.y * 19349663); //hehe magic numbers go brrr
        }
    }
    // no sync only data
    internal class MeadowRoomData : OnlineResource.ResourceData
    {
        private Place[] places = new Place[512]; // Default 512. Should be enough for many rooms (Don't worry it can't overflow)
        private ushort _placesLength = 0;
        private bool _changedSinceLastCheck = true;
        private ushort unusedPlacesLength = 0;
        private HashSet<IntVector2> usedPlaces = new HashSet<IntVector2>(new FastVec2Comparer());
        private Place[] unusedPlaces = new Place[512];
        private ushort scale_power = 9;
        private uint current_max_length = 512;

        public int NumberOfPlaces => _placesLength;

        public override ResourceDataState MakeState(OnlineResource resource) => null; // no state

        internal void AddItemPlacement(int x, int y, bool rare)
        {
            if (_placesLength > current_max_length - 1)
            {
                ++scale_power;
                current_max_length *= 2;
                Place[] new_places = new Place[current_max_length];
                Place[] new_unusedPlaces = new Place[current_max_length];

                Array.Copy(places, new_places, _placesLength);
                Array.Copy(unusedPlaces, new_unusedPlaces, unusedPlacesLength);

                places = new_places;
                unusedPlaces = new_unusedPlaces;

                RainMeadow.Debug("place count increased to 2^" + scale_power);
            }

            places[_placesLength] = new Place(x, y, rare);
            ++_placesLength;
            RainMeadow.Debug("place added! " + places[_placesLength - 1].pos);

            _changedSinceLastCheck = true;
        }

        internal IntVector2 GetUnusedPlace(Room placeRoom)
        {
            if (_placesLength == 0)
                throw new Exception("no places!");

            if (_changedSinceLastCheck)
            {
                ushort entityLength = (ushort)placeRoom.abstractRoom.entities.Count; // Count once, ushort max is 65535, very enough.

                usedPlaces.Clear();
                var refrence = placeRoom.abstractRoom.entities;
                for (int i = 0; i < entityLength; i++)
                {
                    usedPlaces.Add(refrence[i].pos.Tile);
                }

                Array.Clear(unusedPlaces, 0, unusedPlaces.Length);
                unusedPlacesLength = 0;
                for (int i = 0; i < _placesLength; i++)
                {
                    if (!usedPlaces.Contains(places[i].pos))
                    {
                        unusedPlaces[unusedPlacesLength] = places[i]; ++unusedPlacesLength;
                    }
                }

                _changedSinceLastCheck = false;
                if (unusedPlacesLength > 0)
                {
                    return unusedPlaces[UnityEngine.Random.Range(0, unusedPlacesLength)].pos;
                }
                return places[UnityEngine.Random.Range(0, _placesLength)].pos;
            }
            else
            {
                if (unusedPlacesLength > 0)
                {
                    return unusedPlaces[UnityEngine.Random.Range(0, unusedPlacesLength)].pos;
                }
                return places[UnityEngine.Random.Range(0, _placesLength)].pos;
            }
        }

        internal IntVector2 Invalidate(AbstractRoom placeRoomAbstract)
        {
            if (_placesLength == 0) return new IntVector2(0, 0);


            ushort entityLength = (ushort)placeRoomAbstract.entities.Count; // Count once, ushort max is 65535, very enough.

            usedPlaces.Clear();
            var refrence = placeRoomAbstract.entities;
            for (int i = 0; i < entityLength; i++)
            {
                usedPlaces.Add(refrence[i].pos.Tile);
            }

            Array.Clear(unusedPlaces, 0, unusedPlaces.Length);
            unusedPlacesLength = 0;
            for (int i = 0; i < _placesLength; i++)
            {
                if (!usedPlaces.Contains(places[i].pos))
                {
                    unusedPlaces[unusedPlacesLength] = places[i]; ++unusedPlacesLength;
                }
            }

            _changedSinceLastCheck = false;
            if (unusedPlacesLength > 0)
            {
                return unusedPlaces[UnityEngine.Random.Range(0, unusedPlacesLength)].pos;
            }
            return places[UnityEngine.Random.Range(0, _placesLength)].pos;

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