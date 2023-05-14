namespace RainMeadow
{
    public class RegisterNewEntityEvent : OnlineEvent
    {
        public NewEntityEvent newEntityEvent; // contained instead of inherited, because polymorphic

        public RegisterNewEntityEvent() { }
        public RegisterNewEntityEvent(NewEntityEvent newEntityEvent)
        {
            this.newEntityEvent = newEntityEvent;
        }

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeEvent(ref this.newEntityEvent);
        }

        public override void Process()
        {
            (newEntityEvent as NewEntityEvent).onlineResource.OnEntityRegistering(this);
        }

        public override EventTypeId eventType => EventTypeId.RegisterNewEntityEvent;
    }
}