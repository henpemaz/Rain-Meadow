namespace RainMeadow
{
    public abstract class AirCreatureController : CreatureController
    {
        protected AirCreatureController(Creature creature, OnlineCreature oc, int playerNumber, MeadowAvatarCustomization customization) : base(creature, oc, playerNumber, customization) { }


    }
}
