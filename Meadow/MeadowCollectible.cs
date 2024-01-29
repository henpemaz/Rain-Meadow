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
            this.firstChunk.pos = this.abstractPhysicalObject.Room.realizedRoom.RandomPos();
            RainMeadow.Debug("Realized at pos " + this.firstChunk.pos);

            this.gravity = 0;
        }
    }
}
