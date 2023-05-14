namespace RainMeadow
{
    internal abstract class RegisterNewEntityResult : ResultEvent
    {
        public RegisterNewEntityResult() { }
        public RegisterNewEntityResult(RegisterNewEntityEvent referencedEvent) : base(referencedEvent) { }

        public override void Process()
        {
            ((referencedEvent as RegisterNewEntityEvent).newEntityEvent as NewEntityEvent).onlineResource.OnRegisterResolve(this);
        }

        public class Ok : RegisterNewEntityResult
        {
            public Ok() { }
            public Ok(RegisterNewEntityEvent registerEntityEvent) : base(registerEntityEvent) { }
            
            public override EventTypeId eventType => throw new System.NotImplementedException();
        }
        public class Error : RegisterNewEntityResult
        {
            public Error() { }
            public Error(RegisterNewEntityEvent registerEntityEvent) : base(registerEntityEvent) { }
            
            public override EventTypeId eventType => throw new System.NotImplementedException();
        }

    }
}