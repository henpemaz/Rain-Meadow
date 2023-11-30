namespace RainMeadow
{
    public class MeadowCreatureData : GamemodeData
    {
        public OnlineCreature owner;

        public MeadowCreatureData(OnlineCreature owner)
        {
            this.owner = owner;
        }

        internal override GamemodeDataState MakeState(OnlineResource inResource)
        {
            return new MeadowCreatureDataState(this);
        }
    }

    public abstract class GamemodeData
    {
        protected GamemodeData() { }

        internal abstract GamemodeDataState MakeState(OnlineResource inResource);
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public abstract class GamemodeDataState : OnlineState
    {
        protected GamemodeDataState()
        {
        }

        internal abstract void ReadTo(GamemodeData gameModeData);
    }


    public class MeadowCreatureDataState : GamemodeDataState
    {
        [OnlineField]
        bool unused;

        public MeadowCreatureDataState() { }
        public MeadowCreatureDataState(MeadowCreatureData meadowCreatureData)
        {

        }

        internal override void ReadTo(GamemodeData gameModeData)
        {
            
        }
    }

}