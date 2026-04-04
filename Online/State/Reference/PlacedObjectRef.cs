using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.None)]
    public class PlacedObjectRef : OnlineState
    {
        public static ConditionalWeakTable<PlacedObject, PlacedObjectRef> map = new();

        [OnlineField]
        PlacedObject.Type type;
        [OnlineFieldHalf]
        Vector2 pos;

        // TODO: Handle PlacedObject.Data in some capacity when we need to

        public static List<PlacedObject.Type> TrackedTypes = new()
        {
            PlacedObject.Type.HangingPearls,
            PlacedObject.Type.ScavengerOutpost
        };

        public PlacedObjectRef() { }

        public PlacedObjectRef(RoomSession session, PlacedObject.Type type, Vector2 pos)
        {
            this.type = type;
            this.pos = pos;
        }

        // why do they not store a reference to room inside placedobject? >_>
        public static PlacedObjectRef FromPlacedObject(PlacedObject placedObject, Room room)
        {
            if (!RoomSession.map.TryGetValue(room.abstractRoom, out var session)) { throw new InvalidProgrammerException("RoomSession doesn't exist in online space! " + placedObject); }
            return new PlacedObjectRef(session, placedObject.type, placedObject.pos);
        }

        public PlacedObject? ToPlacedObject(RoomSession session)
        {
            if (session.absroom?.realizedRoom == null) return null;

            foreach(var placedObject in session.absroom.realizedRoom.roomSettings.placedObjects.Where(p => TrackedTypes.Contains(p.type)))
            {
                if (EqualsPlacedObject(placedObject))
                {
                    return placedObject;
                }
            }
            return null;
        }

        public bool EqualsPlacedObject(PlacedObject other)
        {
            return other != null &&
                other.type == type &&
                other.pos.CloseEnough(other.pos, 1/4f);
        }

        public override string ToString()
        {
            return $"{base.ToString()}: {type}:{pos}";
        }
    }
}
