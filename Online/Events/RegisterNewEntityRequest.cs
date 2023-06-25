namespace RainMeadow
{
    public class RegisterNewEntityRequest : OnlineEvent, ResolvableEvent
    {
        public NewEntityEvent newEntityEvent; // contained instead of inherited, because polymorphic

        public RegisterNewEntityRequest() { }
        public RegisterNewEntityRequest(NewEntityEvent newEntityEvent)
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
            (newEntityEvent as NewEntityEvent).onlineResource.OnEntityRegisterRequest(this);
        }

        public void Resolve(GenericResult genericResult)
        {
            (newEntityEvent as NewEntityEvent).onlineResource.OnRegisterResolve(genericResult);
        }

        public override EventTypeId eventType => EventTypeId.RegisterNewEntityRequest;
    }
}