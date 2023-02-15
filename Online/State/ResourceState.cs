namespace RainMeadow
{
    public abstract class ResourceState
    {
        internal long ts;

        public virtual long EstimatedSize { get; }
        public abstract ResourceStateType stateType { get; }

        public OnlineResource resource;

        protected ResourceState(OnlineResource resource)
        {
            this.resource = resource;
        }

        public enum ResourceStateType : byte
        {
            Unknown = 0,
            LobbyState,
        }

        public virtual void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref resource);
        }
    }
}
