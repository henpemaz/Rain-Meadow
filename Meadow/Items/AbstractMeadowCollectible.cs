using System.Collections.Generic;

namespace RainMeadow
{
    public class AbstractMeadowCollectible : AbstractPhysicalObject
    {
        public bool placed;
        public bool collectedLocally;
        public bool collected;
        public TickReference collectedTR;
        public int collectedAt;
        protected int duration = 40 * 10;
        public OnlinePhysicalObject online;

        internal bool Expired => collected && world.game.clock > collectedAt + duration;

        public AbstractMeadowCollectible(World world, AbstractObjectType type, WorldCoordinate pos, EntityID ID) : base(world, type, null, pos, ID)
        {

        }

        public override void Update(int time)
        {
            base.Update(time);

            if(online == null)
            {
                online = OnlinePhysicalObject.map.GetValue(this, (apo) => throw new KeyNotFoundException(apo.ToString()));
            }
            
            if (Expired && online.isMine)
            {
                RainMeadow.Debug("Expired:" + online);
                this.Destroy();
                this.Room.entities.Remove(this);
            }
        }

        public void Collect()
        {
            if (collectedLocally) { return; }
            RainMeadow.Debug("Collected locally:" + online);
            collectedLocally = true;
            MeadowProgression.ItemCollected(this);

            if (collected) { return; }
            if (online.isMine)
            {
                NowCollected();
            }
            else
            {
                online.owner.InvokeRPC(CollectRemote, online);
            }
        }

        protected void NowCollected()
        {
            if (!online.isMine) { throw new InvalidProgrammerException("not owner: " + online); }
            if (collected) { return; }
            RainMeadow.Debug("Collected:" + online);
            var ws = world.GetResource();
            collected = true;
            collectedAt = world.game.clock;
            collectedTR = ws.owner.MakeTickReference();

            OnlineManager.lobby.owner.InvokeRPC(MeadowGameMode.ItemConsumed, (byte)ws.ShortId(), type);
        }

        [RPCMethod]
        public static void CollectRemote(OnlinePhysicalObject online)
        {
            RainMeadow.Debug("Collect remote!:" + online);
            if (online != null && online.isMine && online.apo is AbstractMeadowCollectible amc)
            {
                amc.NowCollected();
            }
            else
            {
                RainMeadow.Error($"{online != null} && {online?.isMine} && {online?.apo is AbstractMeadowCollectible}");
            }
        }

        public override void Realize()
        {
            base.Realize(); // important for hooks!
            if (this.realizedObject != null)
            {
                return;
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowPlant)
            {
                this.realizedObject = new MeadowPlant(this);
            }
            else if (type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed
                || type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue
                || type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold
                )
            {
                this.realizedObject = new MeadowCollectToken(this);
            }
        }
    }
}
