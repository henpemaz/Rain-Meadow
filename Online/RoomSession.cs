namespace RainMeadow
{
    public class RoomSession : OnlineResource
    {
        public AbstractRoom absroom;

        public RoomSession(WorldSession ws, AbstractRoom absroom)
        {
            super = ws;
            this.absroom = absroom;
        }

        public override void Activate()
        {
            base.Activate();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            absroom.Abstractize();
        }

        internal override string Identifier()
        {
            return absroom.name;
        }

        public override void ReadState(ResourceState newState, ulong ts)
        {
            //throw new System.NotImplementedException();
        }

        protected override ResourceState MakeState(ulong ts)
        {
            return new RoomState(this, ts);
        }

        private class RoomState : ResourceState
        {
            public RoomState(OnlineResource resource, ulong ts) : base(resource, ts)
            {
            }

            public override ResourceStateType stateType => ResourceStateType.RoomState;
        }
    }
}
