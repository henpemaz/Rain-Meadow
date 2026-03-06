using System.Collections.Generic;
using System.Linq;
using HUD;
using Menu;
using RainMeadow;
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
        public override ArenaSetup.GameTypeID GetGameModeId
        {
            get { return ArenaChallengeMode.ChallengeMode; }
            set { GetGameModeId = value; }
        }

        public override void InitAsCustomGameType(ArenaSetup.GameTypeSetup self)
        {
            self.challengeID = challengeID;
            self.gameType = DLCSharedEnums.GameTypeID.Challenge;
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
            PlayerSpecificOnlineHud owner,
            SlugcatCustomization customization,
            OnlinePlayer player
        )
        {
            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                return "ChieftainA";
            }
            return base.AddIcon(arena, owner, customization, player);
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

        public override void HUD_InitMultiplayerHud(ArenaOnlineGameMode arena, HUD.HUD self, ArenaGameSession session)
        {
            base.HUD_InitMultiplayerHud(arena, self, session);
            var psmh = new PlayerSpecificMultiplayerHud(self, session, session.Players.FirstOrDefault(x => x != null && x.IsLocal()));
            psmh.cornerPos = new Vector2(self.rainWorld.options.ScreenSize.x - self.rainWorld.options.SafeScreenOffset.x, 20f + self.rainWorld.options.SafeScreenOffset.y);
            psmh.flip = -1;

            psmh.parts.Clear();
            var killsList = new HUD.PlayerSpecificMultiplayerHud.KillList(psmh);
            var scoreCounter = new HUD.PlayerSpecificMultiplayerHud.ScoreCounter(psmh);
            scoreCounter.scoreText.color = Color.white; // can't see crap
            scoreCounter.lightGradient.color = Color.white;
            psmh.parts.Add(killsList);
            psmh.parts.Add(scoreCounter);
            self.AddPart(psmh);

        }

        public override Dialog AddGameModeInfo(ArenaOnlineGameMode arena, Menu.Menu menu)
        {
            return new DialogNotify(
                menu.LongTranslate("Pit yourself against a series of challenges!<LINE>Points scored scales down with lobby size.<LINE>All players must remain alive to win.<LINE>Challenges marked in red text are unstable."),
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
