namespace RainMeadow
{
    public class ResourceState
    {
        internal long ts;

        public long EstimatedSize { get; internal set; }

        public virtual void CustomSerialize(Serializer serializer) { }
    }
}
