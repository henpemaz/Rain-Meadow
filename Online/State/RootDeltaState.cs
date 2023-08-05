namespace RainMeadow
{
    /// <summary>
    /// Root-element state. Meant to be used with IPrimaryDelta
    /// Turns out to be not root-only as EntityStates are this but are sent inside ResourceState as well, chaos
    /// </summary>
    public abstract class RootDeltaState : OnlineState
    {
        public OnlinePlayer from; // not serialized, message source
        public uint tick; // not serialized, latest from player when read

        protected RootDeltaState() { }

        protected RootDeltaState(uint tick)
        {
            this.from = OnlineManager.mePlayer;
            this.tick = tick;
        }

        public bool IsDelta { get => _isDelta; set => _isDelta = value; }
        protected bool _isDelta;
        public uint DeltaFromTick;
        public override void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref _isDelta);
            if (!serializer.IsDelta && _isDelta) { serializer.Serialize(ref DeltaFromTick); }
            serializer.IsDelta = _isDelta; // Serializer wraps this call and restores the previous value later (override-proof)
        }

        public override long EstimatedSize(bool inDeltaContext)
        {
            return !inDeltaContext && _isDelta ? 6 : 2;
        }
    }
}