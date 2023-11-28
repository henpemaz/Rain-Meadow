namespace RainMeadow
{
    public class MeadowCreatureData : GamemodeData
    {
        //MeadowCustomization.CreatureCustomization customization; // todo maybe actually move it here!
        public EmoteHolder emoteHolder;
        public OnlineCreature owner;

        public MeadowCreatureData(OnlineCreature owner)
        {
            this.owner = owner;
            this.emoteHolder = new EmoteHolder(owner);
        }

        internal override GamemodeDataState MakeState()
        {
            return new MeadowCreatureDataState(this);
        }
    }

    public abstract class GamemodeData
    {
        protected GamemodeData() { }

        internal abstract GamemodeDataState? MakeState();
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public abstract class GamemodeDataState : OnlineState
    {
        internal abstract void ReadTo(GamemodeData gameModeData);
    }


    public class MeadowCreatureDataState : GamemodeDataState
    {
        public MeadowCreatureDataState(MeadowCreatureData meadowCreatureData)
        {

        }

        internal override void ReadTo(GamemodeData gameModeData)
        {
            
        }
    }

}