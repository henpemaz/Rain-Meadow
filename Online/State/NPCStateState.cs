namespace RainMeadow
{
    public class PlayerNPCStateState : PlayerStateState
    {
        [OnlineField]
        public bool Malnourished;


        // this goes completely unused. won't synchronize for now
        // public bool DieOfStarvation;

        [OnlineField]
        public int KarmaLevel;

        [OnlineField]
        public bool HasMark;

        [OnlineField]
        public bool Glowing;

        [OnlineField]
        public bool Drone;

        [OnlineField]
        public bool HasCloak;

        /*
        [OnlineField]
        public OnlineEntity.EntityId player;
        */

        [OnlineField(nullable: true)]
        public OnlineEntity.EntityId? StomachObject;

        public PlayerNPCStateState() {}
        public PlayerNPCStateState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            var abstractCreature = (AbstractCreature)onlineEntity.apo;
            var playerState = (MoreSlugcats.PlayerNPCState)abstractCreature.state;
            Malnourished = playerState.Malnourished;
            KarmaLevel = playerState.KarmaLevel;
            HasMark = playerState.HasMark;
            Glowing = playerState.Glowing;
            Drone = playerState.Drone;
            HasCloak = playerState.HasCloak;

            if (playerState.StomachObject is AbstractPhysicalObject apo) {
                if (!OnlinePhysicalObject.map.TryGetValue(apo, out var oe))
                {
                    apo.world?.GetResource()?.ApoEnteringWorld(apo);
                    if (!OnlinePhysicalObject.map.TryGetValue(apo, out oe)) throw new System.InvalidOperationException("Stomach item doesn't exist in online space!");
                }

                StomachObject = oe.id;
            }
            // We shouldn't need to synchronize player because we know who the player is.
        }

        public override void ReadTo(AbstractCreature abstractCreature)
        {
            base.ReadTo(abstractCreature);
            var NPCState = (MoreSlugcats.PlayerNPCState)abstractCreature.state;
            NPCState.player = abstractCreature;
            
            NPCState.Malnourished = Malnourished;
            NPCState.KarmaLevel = KarmaLevel;
            NPCState.HasMark = HasMark;
            NPCState.Glowing = Glowing;
            NPCState.Drone = Drone;
            NPCState.HasCloak = HasCloak;
            NPCState.StomachObject = (this.objectInStomach?.FindEntity() as OnlinePhysicalObject)?.apo;
        }
    }
}
