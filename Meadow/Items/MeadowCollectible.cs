namespace RainMeadow
{
    public abstract class MeadowCollectible : PhysicalObject
    {
        public AbstractMeadowCollectible abstractCollectible => this.abstractPhysicalObject as AbstractMeadowCollectible;
        public MeadowCollectible(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowCollectible");
            this.bodyChunks = new BodyChunk[1] { new BodyChunk(this, 0, default, 1, 1) };
            this.bodyChunkConnections = new BodyChunkConnection[0];
            this.gravity = 0;
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
                var place = mrd.GetUnusedPlace(placeRoom);
                this.abstractPhysicalObject.pos.Tile = place;
                firstChunk.HardSetPosition(placeRoom.MiddleOfTile(place));
            }

            base.PlaceInRoom(placeRoom);
        }

        public override void NewRoom(Room newRoom)
        {
            base.NewRoom(newRoom);
        }
    }
}
