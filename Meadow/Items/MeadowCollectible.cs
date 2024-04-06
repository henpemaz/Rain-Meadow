using UnityEngine;

namespace RainMeadow
{
    public abstract class MeadowCollectible : PhysicalObject
    {
        public Vector2 placePos;

        public AbstractMeadowCollectible abstractCollectible => this.abstractPhysicalObject as AbstractMeadowCollectible;
        public MeadowCollectible(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowCollectible");
            this.bodyChunks = new BodyChunk[1] { new BodyChunk(this, 0, default, 1, 1) };
            this.bodyChunkConnections = new BodyChunkConnection[0];
            this.gravity = 0;
            this.collisionLayer = 0;
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            if (abstractCollectible.placed)
            {
                firstChunk.HardSetPosition(placeRoom.MiddleOfTile(this.abstractPhysicalObject.pos));
                RainMeadow.Debug("PlaceInRoom already placed at pos " + this.firstChunk.pos);
            }
            else
            {
                RoomSession.map.TryGetValue(placeRoom.abstractRoom, out var rs);
                var mrd = rs.GetData<MeadowRoomData>();
                var place = mrd.GetUnusedPlace(placeRoom); // todo extra reqs for places ie not-narrow etc
                this.abstractPhysicalObject.pos.Tile = place;
                this.placePos = placeRoom.MiddleOfTile(place);
                firstChunk.HardSetPosition(placePos);
                abstractCollectible.placed = true;
            }

            if (abstractCollectible.collectedLocally) return;
            base.PlaceInRoom(placeRoom);
        }
    }
}
