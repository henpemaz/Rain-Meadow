namespace RainMeadow
{
    internal class ScavengerController : CreatureController
    {
        public static void EnableScavenger()
        {
            On.ScavengerAI.Update += ScavengerAI_Update;
        }

        private static void ScavengerAI_Update(On.ScavengerAI.orig_Update orig, ScavengerAI self)
        {
            if (UnityEngine.Input.GetKey(UnityEngine.KeyCode.Space))
            {
                self.testIdlePos = self.creature.pos;
                self.SetDestination(self.creature.pos);
                self.behavior = ScavengerAI.Behavior.Idle;
                self.idleCounter = 15;
                self.backedByPack = 0f;
                self.scavageItemCheck = 200;
                self.goToSquadLeaderFirstTime = 1000;
                self.arrangeInventoryCounter = 400;
                self.runSpeedGoal = 1f;
                self.scavengeCandidate = null;
                self.agitation = 0f;
                self.scared = 0f;
                self.discomfortWithOtherCreatures = 0f;
                self.idleCounter = 0;
                self.alreadyIdledAt.Clear();
                self.tradeSpot = null;
                self.wantToTradeWith = null;
                self.giftForMe = null;
                self.scavenger.GoThroughFloors = false;
                self.UpdateLookPoint();
                self.runSpeedGoal = 0f;
                self.scavenger.moving = false;
                self.scavenger.movMode = Scavenger.MovementMode.StandStill;
                self.scavenger.notFollowingPathToCurrentGoalCounter = 0;
            }
            else
            {
                self.behavior = ScavengerAI.Behavior.Idle;
                self.idleCounter = 200;
                self.testIdlePos = self.creature.pos;
                //self.SetDestination(self.creature.pos);
                self.discomfortWithOtherCreatures = 0;
                orig(self);
            }
        }

        public ScavengerController(Scavenger scav, OnlineCreature oc, int playerNumber) : base(scav, oc, playerNumber)
        {
        }

        public override bool GrabImpl(PhysicalObject pickUpCandidate)
        {
            //throw new System.NotImplementedException();
            return false;
        }
    }
}