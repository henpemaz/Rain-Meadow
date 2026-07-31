using System;
using System.Linq;
using Menu;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
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


        public override void InitAsCustomGameType(ArenaOnlineGameMode arena, ArenaSetup.GameTypeSetup self)
        {
            self.challengeID = challengeID;
            self.gameType = DLCSharedEnums.GameTypeID.Challenge;
            self.spearsHitPlayers = arena.onlineArenaSettingsInterfaceeBool["SPEARSHIT"];
            SandboxSettingsInterface.DefaultKillScores(ref self.killScores);
        }

        /// <exception cref="InvalidOperationException">
        /// Thrown if the online game mode is <see cref="ArenaOnlineGameMode"/>
        /// and <see cref="ArenaChallengeMode"/> is not registered.
        /// </exception>
        public static bool IsChallengeMode(out ArenaChallengeMode challenge)
        {
            challenge = null!;

            if (!RainMeadow.isArenaMode(out ArenaOnlineGameMode arenaOnline))
                return false;
            if (!arenaOnline.registeredGameModes.TryGetValue(ChallengeMode.value, out ExternalArenaGameMode externalArena))
            {
                throw new InvalidOperationException(
                    $"Could not find game mode. Registered: " +
                    $"[ {string.Join(", ", arenaOnline.registeredGameModes.Keys)} ]."
                );
            }

            if (arenaOnline.currentGameMode == ChallengeMode.value)
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
            if (arena?.session?.arenaSitting?.players != null && arena.session.arenaSitting.players.Count > 0 && (arena.session?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.PROTECT || arena.session?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.SURVIVE))
            {
                return arena.session.arenaSitting.players.Max(pl => pl.timeAlive);
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
            if (arena.session?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.PROTECT || arena.session?.chMeta?.secondaryWinMethod == MoreSlugcats.ChallengeInformation.ChallengeMeta.WinCondition.SURVIVE)
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



        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
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
