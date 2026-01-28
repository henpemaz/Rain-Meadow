
namespace RainMeadow
{
    public class RealizedSingularityBombState : RealizedState<MoreSlugcats.SingularityBomb>
    {
        [OnlineField]
        bool ignited;

        [OnlineField]
        bool activateSingularity;

        [OnlineField]
        bool activateSucktion;

        [OnlineField]
        byte counter; // float is always used as an integer with range [0, 120)

        public RealizedSingularityBombState() { }
        public RealizedSingularityBombState(MoreSlugcats.SingularityBomb bomb, OnlinePhysicalObject onlineEntity) : base(onlineEntity)
        {
            ignited = bomb.ignited;
            activateSingularity = bomb.activateSingularity;
            activateSucktion = bomb.activateSucktion;
            counter = (byte)bomb.counter;
        }

        public override void ReadTo(MoreSlugcats.SingularityBomb bomb, OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            bomb.ignited = ignited;
            bomb.activateSingularity = activateSingularity;
            bomb.activateSucktion = activateSucktion;
            bomb.counter = counter;
        }
    }
}
