namespace RainMeadow
{
    public class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;
        public bool abstractOnDeactivate;

        public RoomSession(WorldSession ws, AbstractRoom absroom)
        {
            super = ws;
            this.absroom = absroom;
            deactivateOnRelease = true;
        }

        protected override void ActivateImpl()
        {
            //throw new System.NotImplementedException();
        }

        protected override void DeactivateImpl()
        {
            if (abstractOnDeactivate)
            {
                absroom.Abstractize();
            }
        }

        internal override string Identifier()
        {
            return super.Identifier() + absroom.name;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            //throw new System.NotImplementedException();
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new RoomState(this, ts);
        }

        public class RoomState : ResourceState
        {
            public RoomState(OnlineResource resource, ulong ts) : base(resource, ts)
            {
            }

            public override ResourceStateType stateType => ResourceStateType.RoomState;
        }
    }
}
