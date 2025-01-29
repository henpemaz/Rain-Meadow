using RainMeadow;
using System.Collections.Generic;
using System.Linq;
namespace RainMeadow
{
    public class Competitive : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID CompetitiveMode = new ArenaSetup.GameTypeID("Free For All", register: false);

        private int _timerDuration;  // Backing field for TimerDuration



        public override bool IsExitsOpen(ArenaOnlineGameMode arena, On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            int playersStillStanding = self.gameSession.Players?.Count(player =>
                player.realizedCreature != null &&
                (player.realizedCreature.State.alive)) ?? 0;

            if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1)
            {
                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            orig(self);

            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return true;
        }
        public override string TimerText()
        {
            if (ModManager.MSC && (OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                return $"Prepare for combat, {(OnlineManager.lobby.gameMode as ArenaOnlineGameMode).paincatName}";
            }
            return $"Prepare for combat, {SlugcatStats.getSlugcatName((OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>()).playingAs)}";
        }

        public override int SetTimer(ArenaOnlineGameMode arena)
        {
            return arena.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        public override int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            return --arena.setupTime;
        }
        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            if (arena.setupTime > 0)
            {
                return arena.countdownInitiatedHoldFire = true;
            }
            else
            {
                return arena.countdownInitiatedHoldFire = false;
            }
        }

        public override void LandSpear(ArenaOnlineGameMode arena, ArenaGameSession self, Player player, Creature target, ArenaSitting.ArenaPlayer aPlayer)
        {
            aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);

        }

        public override void ArenaSessionCtor(ArenaOnlineGameMode arena, On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            base.ArenaSessionCtor(arena, orig, self, game);
            arena.ResetInvDetails();
        }

        public override string AddCustomIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud onlineHud)
        {
            Player player = null;
            player = onlineHud.abstractPlayer.realizedCreature as Player;


            if (player != null)
            {
                if ((player.SlugCatClass == SlugcatStats.Name.Yellow))
                {

                    return "FriendA";
                }
                if (player.SlugCatClass == SlugcatStats.Name.Red)
                {

                    return "Symbol_Neuron";
                }

                if (player.SlugCatClass == SlugcatStats.Name.Night)
                {

                    return "GuidanceMoon";
                }
                if (ModManager.MSC)
                {

                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                    {

                        return "stickLeftB";
                    }



                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Rivulet)
                    {

                        return "Menu_Symbol_Dont_Shuffle";
                    }

                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                    {

                        return "Symbol_FireSpear";
                    }
                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint)
                    {

                        return "FlowerMarker";
                    }

                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
                    {

                        return "MonkA";
                    }

                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear)
                    {

                        return "spearSymbol";
                    }
                    if (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                    {

                        return "Kill_Bat";
                    }
                }
            }
            return "Multiplayer_Bones";
        }
    }
}