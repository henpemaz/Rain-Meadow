namespace RainMeadow
{
    /// <summary>
    /// Root-element state. Meant to be used with IPrimaryDelta
    /// Turns out to be not root-only as EntityStates are this but are sent inside ResourceState as well, chaos
    /// </summary>
    public abstract class RootDeltaState : OnlineState, Generics.IPrimaryDelta<OnlineState>
    {
        [FieldIgnore] 
        public OnlinePlayer from; // not serialized, message source
        [FieldIgnore] 
        public uint tick; // not serialized, latest from player when read
        [FieldIgnore]
        public bool isDelta;
        [FieldIgnore]
        public uint baseline;
        protected RootDeltaState() : base() { }

        protected RootDeltaState(uint tick)
        {
            this.from = OnlineManager.mePlayer;
            this.tick = tick;
        }
    }
}