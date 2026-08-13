using System;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS
{
    public partial class ArenaChallengeMode : ExternalArenaGameMode
    {
        public static ArenaSetup.GameTypeID ChallengeMode = new ArenaSetup.GameTypeID(
            "Challenge",
            register: false
        );

        public int challengeID = RainMeadow.rainMeadowOptions.ChallengeID.Value;

        private int _timerDuration;

        public override ArenaSetup.GameTypeID GetGameModeId => ChallengeMode;
        public override bool ShowAddedScoreBetweenRoundsInOnlinePlayerUI { get => false; set { } }

        public override string GameModeInfo => "Pit yourself against a series of challenges";

        public override void InitAsCustomGameType(ArenaOnlineGameMode arena, ArenaSetup.GameTypeSetup self)
        {
            self.challengeID = challengeID;
            self.gameType = DLCSharedEnums.GameTypeID.Challenge;
            self.spearsHitPlayers = arena.onlineArenaSettingsInterfaceeBool["SPEARSHIT"];
            SandboxSettingsInterface.DefaultKillScores(ref self.killScores);
        }

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

        public override bool IsExitsOpen(
            ArenaOnlineGameMode arena,
            On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig,
            ArenaBehaviors.ExitManager self
        )
        {
            if (self.challengeCompleted)
            {
                return self.gameSession.Players.Any(x => x.state.alive);
            }
            return false;
        }

        public override bool SpawnBatflies(FliesWorldAI self, int spawnRoom)
        {
            return false;
        }

        public override string TimerText()
        {
            return Utils.Translate("Survive,") + " " + Utils.Translate(PlayingAsText());
        }

        public override int SetTimer(ArenaOnlineGameMode arena)
        {
            if (arena.ArenaSession?.arenaSitting?.players?.Count > 0 && (arena.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.PROTECT || arena.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.SURVIVE))
            {
                return arena.ArenaSession.arenaSitting.players.Max(pl => pl.timeAlive);
            }
            return 0;
        }

        public override int TimerDuration
        {
            get { return _timerDuration; }
            set { _timerDuration = value; }
        }

        public override int TimerDirection(ArenaOnlineGameMode arena, int timer)
        {
            if (arena.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.PROTECT || arena.ArenaSession?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.SURVIVE)
            {
                return ++arena.setupTime;
            }
            return --arena.setupTime;
        }

        public override bool HoldFireWhileTimerIsActive(ArenaOnlineGameMode arena)
        {
            return arena.countdownInitiatedHoldFire = false;
        }

        public override void LandSpear(
            ArenaOnlineGameMode arena,
            ArenaGameSession self,
            Player player,
            Creature target,
            ArenaSitting.ArenaPlayer aPlayer
        )
        {
            aPlayer.AddSandboxScore(self.GameTypeSetup.spearHitScore);
        }

        public override string AddIcon(
            ArenaOnlineGameMode arena,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                return "ChieftainA";
            }
            return base.AddIcon(arena, display, owner, customization, player);
        }

        public override Color IconColor(
            ArenaOnlineGameMode arena,
            OnlinePlayerDisplay display,
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            if (owner.PlayerConsideredDead)
            {
                return Color.grey;
            }
            if (
                arena.reigningChamps != null
                && arena.reigningChamps.list != null
                && arena.reigningChamps.list.Contains(player.id)
            )
            {
                return Color.yellow;
            }

            return base.IconColor(arena, display, owner, customization, player);
        }
    }
}
