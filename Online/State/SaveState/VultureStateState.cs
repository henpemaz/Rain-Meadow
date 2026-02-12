namespace RainMeadow
{
    public class VultureStateState : CreatureStateState
    {
        [OnlineField]
        bool mask;

        public VultureStateState() {}
        public VultureStateState(OnlineCreature onlineCreature) : base(onlineCreature)
        {
            var abstractCreature = (AbstractCreature)onlineCreature.apo;
            var vultureState = (Vulture.VultureState)abstractCreature.state;

            mask = vultureState.mask;
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var vultureState = (Vulture.VultureState)abstractCreature.state;

            vultureState.mask = mask;
        }
    }
}
