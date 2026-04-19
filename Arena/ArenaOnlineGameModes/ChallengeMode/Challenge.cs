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


        public override void InitAsCustomGameType(ArenaOnlineGameMode arena, ArenaSetup.GameTypeSetup self)
        {
            self.challengeID = challengeID;
            self.gameType = DLCSharedEnums.GameTypeID.Challenge;
            self.spearsHitPlayers = arena.onlineArenaSettingsInterfaceeBool["SPEARSHIT"];
            SandboxSettingsInterface.DefaultKillScores(ref self.killScores);
        }

        public static bool isChallengeMode(
            ArenaOnlineGameMode arena,
            out ArenaChallengeMode challenge
        )
        {
            challenge = null;
            if (arena.currentGameMode == ChallengeMode.value)
            {
                challenge = (
                    arena
                        .registeredGameModes.FirstOrDefault(x => x.Key == ChallengeMode.value)
                        .Value as ArenaChallengeMode
                );
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
            return Utils.Translate("A challenge approaches,") + " " + Utils.Translate(PlayingAsText());
        }

        public override int SetTimer(ArenaOnlineGameMode arena)
        {
            if (arena?.session?.arenaSitting?.players != null && arena.session.arenaSitting.players.Count > 0)
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
            return ++arena.setupTime;
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
