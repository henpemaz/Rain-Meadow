namespace RainMeadow
{
    public class ResourceState
    {
        public long EstimatedSize { get; internal set; }

        public virtual void CustomSerialize() { }
    }
}
