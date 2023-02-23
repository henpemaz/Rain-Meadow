namespace RainMeadow
{
    public abstract partial class OnlineResource
    {
        private ResourceState lastState;

        public virtual ResourceState GetState(ulong ts)
        {
            if (lastState == null || lastState.ts != ts)
            {
                lastState = MakeState(ts);
            }

            return lastState;
        }

        protected abstract ResourceState MakeState(ulong ts);
        public abstract void ReadState(ResourceState newState, ulong ts);

        public abstract class ResourceState : OnlineState
        {
            public OnlineResource resource;

            protected ResourceState() : base() { }
            protected ResourceState(OnlineResource resource, ulong ts) : base(ts)
            {
                this.resource = resource;
            }

            public override long EstimatedSize => resource.SizeOfIdentifier();
            public override void CustomSerialize(Serializer serializer)
            {
                base.CustomSerialize(serializer);
                serializer.Serialize(ref resource);
            }
        }
    }
}
