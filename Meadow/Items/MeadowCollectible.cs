using UnityEngine;

namespace RainMeadow
{
    public abstract class MeadowCollectible : PhysicalObject
    {
        public Vector2 placePos;

        public AbstractMeadowCollectible abstractCollectible => this.abstractPhysicalObject as AbstractMeadowCollectible;
        public MeadowCollectible(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowCollectible " + abstractCollectible.online);
            this.bodyChunks = new BodyChunk[1] { new BodyChunk(this, 0, default, 1, 1) };
            this.bodyChunkConnections = new BodyChunkConnection[0];
            this.gravity = 0;
            this.collisionLayer = 0;
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            RainMeadow.Debug("MeadowCollectible " + abstractCollectible.online);
            if (abstractCollectible.placed)
            {
                RainMeadow.Debug("PlaceInRoom already placed: " + abstractPhysicalObject.pos);
            }
            else
            {
                RainMeadow.Debug("PlaceInRoom picking place");
                RoomSession.map.TryGetValue(placeRoom.abstractRoom, out var rs);
                var mrd = rs.GetData<MeadowRoomData>();
                var place = mrd.GetUnusedPlace(placeRoom); // todo extra reqs for places ie not-narrow etc
                this.abstractPhysicalObject.pos.Tile = place;
                abstractCollectible.placed = true;
                RainMeadow.Debug("placed at " + abstractPhysicalObject.pos);
            }

            this.placePos = placeRoom.MiddleOfTile(this.abstractPhysicalObject.pos);
            firstChunk.HardSetPosition(placePos);

            base.PlaceInRoom(placeRoom);
        }
    }
}
