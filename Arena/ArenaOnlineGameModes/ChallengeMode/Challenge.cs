using System;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS
{
    public partial class ArenaChallengeMode : ExternalArenaGameMode
    {
        public static ArenaSetup.GameTypeID ChallengeMode = new("Challenge");

        public override ArenaSetup.GameTypeID GetGameModeId => ChallengeMode;
        private int _timerDuration;
        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }
        public override bool ShowAddedScoreBetweenRoundsInOnlinePlayerUI { get => false; set { } }
        public int challengeID = RainMeadow.rainMeadowOptions.ChallengeID.Value;

        public static bool IsChallengeMode(out ArenaChallengeMode challenge)
        {
            challenge = null!;

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                return false;

            if (arenaOnline.registeredGameModes.TryGetValue(
                    ChallengeMode.value,
                    out ExternalArenaGameMode externalArena
                )
                && arenaOnline.currentGameMode == ChallengeMode.value)
            {
                challenge = (ArenaChallengeMode)externalArena;
                return true;
            }

            return false;
        }

        public override bool ShouldWinByScore(ArenaSetup.GameTypeSetup gameTypeSetup) => false;

        public override void InitAsCustomGameType(
            ArenaOnlineGameMode arenaOnline,
            ArenaSetup.GameTypeSetup self)
        {
            base.InitAsCustomGameType(arenaOnline, self);

            self.survivalScore   = 0;
            self.KillScore       = 0;
            self.EmptyDeathScore = 0;
            self.spearHitScore   = 0;
            self.foodScore       = 1;

            self.challengeID = challengeID;
            self.gameType = DLCSharedEnums.GameTypeID.Challenge;
        }

        public override int On_ArenaSetup_GameTypeSetup_get_ScoreToEnterDen(
            Func<ArenaSetup.GameTypeSetup, int> orig,
            ArenaSetup.GameTypeSetup self)
        {
            return orig(self);
        }

        public override bool On_ArenaBehaviors_ExitManager_ExitsOpen(
            ArenaOnlineGameMode arenaOnline,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self)
        {
            if (self.challengeCompleted)
            {
                return self.gameSession.Players.Any(x => x.state.alive);
            }
            return false;
        }

        public override string TimerText()
        {
            return Utils.Translate("Survive,") + " " + Utils.Translate(PlayingAsText());
        }

        public override int SetTimer(ArenaOnlineGameMode arenaOnline)
        {
            if (arenaOnline.ArenaSession?.arenaSitting?.players?.Count > 0 && (arenaOnline.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.PROTECT || arenaOnline.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.SURVIVE))
            {
                return arenaOnline.ArenaSession.arenaSitting.players.Max(pl => pl.timeAlive);
            }
            return 0;
        }

        public override int TimerDirection(ArenaOnlineGameMode arenaOnline, int timer)
        {
            if (arenaOnline.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.PROTECT || arenaOnline.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.SURVIVE)
            {
                return ++arenaOnline.setupTime;
            }
            return --arenaOnline.setupTime;
        }

        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arenaOnline)
        {
            return arenaOnline.countdownInitiatedHoldFire = false;
        }

        public override string AddIcon(
            ArenaOnlineGameMode arenaOnline,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player)
        {
            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                return "ChieftainA";
            }
            return base.AddIcon(arenaOnline, display, owner, customization, player);
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
                menu.LongTranslate("Pit yourself against a series of challenges"),
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
