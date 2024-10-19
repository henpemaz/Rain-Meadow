namespace RainMeadow
{
    public abstract class AirCreatureController : CreatureController
    {
        protected AirCreatureController(Creature creature, OnlineCreature oc, int playerNumber, MeadowAvatarData customization) : base(creature, oc, playerNumber, customization) { }


    }
}
