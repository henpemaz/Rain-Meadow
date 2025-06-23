using Menu;
using RainMeadow;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace RainMeadow
{
    public class FFA : ExternalArenaGameMode
    {

        public static ArenaSetup.GameTypeID FFAMode = new ArenaSetup.GameTypeID("Free For All", register: false);

        private int _timerDuration;
        //public override ArenaSetup.GameTypeID GetGameModeId
        //{
        //    get
        //    {
        //        return FFAMode;
        //    }
        //}
        public static bool isFFA(ArenaOnlineGameMode arena, out FFA ffa)
        {

            ffa = null;
            if (arena.currentGameMode == FFAMode.value)
            {
                ffa = (arena.registeredGameModes.FirstOrDefault(x => x.Key == FFAMode.value).Value as FFA);
                return true;
            }
            return false;

        }

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

            return orig(self);
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }
        public override string TimerText()
        {
            var client_settings = OnlineManager.lobby.clientSettings[OnlineManager.mePlayer].GetData<ArenaClientSettings>();

            SlugcatStats.Name playingAs;
            if (client_settings.playingAs != RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat)
            {
                playingAs = client_settings.playingAs;
            }
            else
            {
                playingAs = client_settings.randomPlayingAs ?? SlugcatStats.Name.White;
            }

            if (ModManager.MSC && playingAs == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
            {
                return Utils.Translate($"Prepare for combat,") + " " + Utils.Translate((OnlineManager.lobby.gameMode as ArenaOnlineGameMode)?.paincatName ?? "");
            }

            return Utils.Translate("Prepare for combat,") + " " + Utils.Translate(SlugcatStats.getSlugcatName(playingAs));
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

        public override void ArenaSessionEnded(ArenaOnlineGameMode arena, On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session, List<ArenaSitting.ArenaPlayer> list)
        {
            if (list.Count == 1)
            {
                list[0].winner = list[0].alive;
            }
            else if (list.Count > 1)
            {
                if (list[0].alive && !list[1].alive)
                {
                    list[0].winner = true;
                }
                else if (list[0].score > list[1].score)
                {
                    list[0].winner = true;
                }
            }
        }

        public override DialogNotify AddGameModeInfo(Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Trust no one. Last scug standing wins."), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }

    }
}
