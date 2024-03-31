using UnityEngine;

namespace RainMeadow
{
    public class MeadowCollectibleToken : MeadowCollectible
    {
        private NotAToken token;

        public MeadowCollectibleToken(AbstractPhysicalObject abstractPhysicalObject) : base(abstractPhysicalObject)
        {
            RainMeadow.Debug("MeadowCollectibleToken");
        }

        public override void PlaceInRoom(Room placeRoom)
        {
            base.PlaceInRoom(placeRoom);
            if(this.token == null)
            {
                var tokenData = new MeadowCollectToken.CollectTokenData(null, false);
                tokenData.handlePos = new Vector2(0, -40f);
                var pobj = new PlacedObject(PlacedObject.Type.GoldToken, tokenData);
                pobj.pos = this.firstChunk.pos;
                pobj.pos.y += 30f;
                this.token = new NotAToken(placeRoom, pobj);
                room.AddObject(token);
            }
        }

        public class NotAToken : CollectToken, IDrawable
        {
            public NotAToken(Room room, PlacedObject placedObj) : base(room, placedObj)
            {

            }

            new public void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
            {
                base.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                sLeaser.sprites[this.MainSprite].color = Color.green;
                sLeaser.sprites[this.GoldSprite].color = Color.green;
                sLeaser.sprites[this.LightSprite].color = Color.green;
                sLeaser.sprites[this.TrailSprite].color = Color.green;
            }
        }
    }
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
