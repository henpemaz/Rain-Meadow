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
        public override ArenaSetup.GameTypeID GetGameModeId
        {
            get
            {
                return FFA.FFAMode;
            }
            set { GetGameModeId = value; }

        }
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

            if (playersStillStanding == 1 && arena.arenaSittingOnlineOrder.Count > 1 && arena.setupTime <= 0)
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
            return Utils.Translate("Prepare for combat,") + " " + Utils.Translate(PlayingAsText());
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

        public override string AddIcon(ArenaOnlineGameMode arena, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {

            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                return "ChieftainA";
            }
            return base.AddIcon(arena, owner, customization, player);

        }

        public override Color IconColor(ArenaOnlineGameMode arena, OnlinePlayerDisplay display, PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player)
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }
            if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
            {
                return Color.yellow;
            }

            return base.IconColor(arena, display, owner, customization, player);
        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(menu.LongTranslate("Trust no one. Last scug standing wins"), new Vector2(500f, 400f), menu.manager, () => { menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed); });
        }
    }
}
