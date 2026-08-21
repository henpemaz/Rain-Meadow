using System;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class FFA : ExternalArenaGameMode
    {
        public static ArenaSetup.GameTypeID FFAMode = new("Free For All");

        public override ArenaSetup.GameTypeID GetGameModeId => FFAMode;
        private int _timerDuration;
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }

        public static bool IsFfaMode(out FFA ffa)
        {
            ffa = null!;

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                return false;

            if (arenaOnline.registeredGameModes.TryGetValue(FFAMode.value, out ExternalArenaGameMode externalArena)
                && arenaOnline.currentGameMode == FFAMode.value)
            {
                ffa = (FFA)externalArena;
                return true;
            }

            return false;
        }

        public override bool On_ArenaBehaviors_ExitManager_ExitsOpen(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self)
        {
            if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Always)
            {
                // idk why orig ignores this when 2 player exists
                return true;
            }

            if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Score)
            {
                return orig(self) || (self.gameSession?.arenaSitting?.players?.Any(p => p?.score >= self.gameSession.GameTypeSetup.ScoreToEnterDen) ?? false);
            }

            int playersStillStanding =
                self.gameSession.Players?.Count(player =>
                    player.realizedCreature != null && (player.realizedCreature.State.alive)
                ) ?? 0;

            if (playersStillStanding == 1 && arenaOnline.arenaSittingOnlineOrder.Count >= 1 && !arenaOnline.countdownInitiatedHoldFire)
            {
                return true;
            }

            if (self.world.rainCycle.TimeUntilRain <= 100)
            {
                return true;
            }

            return orig(self);
        }

        public override string TimerText()
        {
            return Utils.Translate("Prepare for combat,") + " " + Utils.Translate(PlayingAsText());
        }

        public override int SetTimer(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.setupTime = RainMeadow.rainMeadowOptions.ArenaCountDownTimer.Value;
        }

        public override int TimerDirection(ArenaOnlineGameMode arenaOnline, int timer)
        {
            return --arenaOnline.setupTime;
        }

        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arenaOnline)
        {
            if (arenaOnline.setupTime > 0)
            {
                return arenaOnline.countdownInitiatedHoldFire = true;
            }
            else
            {
                return arenaOnline.countdownInitiatedHoldFire = false;
            }
        }

        public override string AddIcon(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            string arenaIcon = base.AddIcon(arenaOnline, display, owner, customization, player);
            if (arenaIcon != "")
                return arenaIcon;
            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
                return "ChieftainA";
            return "";
        }

        public override Color IconColor(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }
            if (
                arenaOnline.reigningChamps != null
                && arenaOnline.reigningChamps.list != null
                && arenaOnline.reigningChamps.list.Contains(player.id)
            )
            {
                return Color.yellow;
            }

            return base.IconColor(arenaOnline, display, owner, customization, player);
        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arenaOnline, Menu.Menu menu)
        {
            return new DialogNotify(
                menu.LongTranslate("Trust no one. Last scug standing wins"),
                new Vector2(500f, 400f),
                menu.manager,
                () =>
                {
                    menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                }
            );
        }
    }
}
