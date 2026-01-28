
    
namespace RainMeadow
{
    /// <summary>
    /// Middleware to handle safe casting for specific realized types
    /// </summary>
    /// <typeparam name="T">The specific PhysicalObject type</typeparam>
    public abstract class RealizedState<T> : RealizedPhysicalObjectState where T : PhysicalObject
    {
        public RealizedState() : base() { }
        public RealizedState(OnlinePhysicalObject onlineEntity) : base(onlineEntity) { }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (onlineEntity is OnlinePhysicalObject opo && opo.apo.realizedObject is T realized)
            {
                ReadTo(realized, onlineEntity);
            }
            else
            {
               // This silently swallows the desync for this frame, preventing the crash.
               // We assume the object will correct itself or the packet is old.
            }
        }

        public abstract void ReadTo(T realized, OnlineEntity onlineEntity);
    }
}
