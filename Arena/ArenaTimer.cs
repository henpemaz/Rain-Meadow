
using UnityEngine;

namespace RainMeadow
{
    public class ArenaPrepTimer : HUD.HudPart
    {
        public enum TimerMode
        {
            Countdown,
            Waiting
        }

        public float Readtimer;
        public bool isRunning;
        public TimerMode currentMode = TimerMode.Waiting;  // Track which timer is active
        public TimerMode showMode = TimerMode.Waiting;  // Track which timer is being displayed
        public TimerMode matchMode = TimerMode.Waiting; // mode at start of match
        public FLabel timerLabel;
        public FLabel modeLabel;
        public Vector2 pos, lastPos;
        public float fade, lastFade;
        public ArenaOnlineGameMode arena;
        public ArenaGameSession session;
        public bool cancelTimer;
        public Player? player;
        public bool countdownInitiated;
        public int safetyCatchTimer;
        public ArenaPrepTimer(HUD.HUD hud, FContainer fContainer, ArenaOnlineGameMode arena, ArenaGameSession arenaGameSession) : base(hud)
        {
            arena.arenaPrepTimer = this;
            session = arenaGameSession;
            arena.trackSetupTime = arena.onlineArenaGameMode.SetTimer(arena);
            matchMode = TimerMode.Waiting;

            timerLabel = new FLabel("font", FormatTime(0))
            {
                scale = 2.4f,
                alignment = FLabelAlignment.Left
            };

            modeLabel = new FLabel("font", currentMode.ToString())
            {
                scale = 1.6f,
                alignment = FLabelAlignment.Left
            };

            pos = new Vector2(80f, hud.rainWorld.options.ScreenSize.y - 60f);
            lastPos = pos;
            timerLabel.SetPosition(DrawPos(1f));
            modeLabel.SetPosition(DrawPos(1f) + new Vector2(135f, 0f));

            fContainer.AddChild(timerLabel);
            fContainer.AddChild(modeLabel);
            this.arena = arena;
            countdownInitiated = false;
            safetyCatchTimer = 0;
        }

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (RainMeadow.isArenaMode(out var arena))
            {
                if (showMode == TimerMode.Waiting)
                {
                    safetyCatchTimer++;
                }

                if (arena.playerEnteredGame < arena.arenaSittingOnlineOrder.Count)
                {
                    showMode = TimerMode.Waiting;
                    matchMode = TimerMode.Waiting;
                    modeLabel.text = Utils.Translate(showMode.ToString());
                }
                else
                {
                    showMode = TimerMode.Countdown;
                }

                if ((safetyCatchTimer > 300)) // Something went wrong with the timer. Let's move on
                {
                    showMode = TimerMode.Countdown;
                };

                arena.onlineArenaGameMode.HoldFireWhileTimerIsActive(arena);


                if (arena.setupTime > 0 && showMode == TimerMode.Countdown)
                {
                    matchMode = TimerMode.Countdown;
                    modeLabel.text = arena.onlineArenaGameMode.TimerText();
                }

                if (arena.setupTime <= 0 && !countdownInitiated)
                {
                    countdownInitiated = true;
                    hud.PlaySound(SoundID.MENU_Start_New_Game);
                    ClearSprites();
                }
                timerLabel.text = FormatTime(arena.setupTime);
            }
        }

        // Format time to MM:SS:MMM
        public static string FormatTime(float time)
        {
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);
            int milliseconds = Mathf.FloorToInt((time % 1) * 1000);

            return $"{minutes:D2}:{seconds:D2}:{milliseconds:D3}";
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            timerLabel.RemoveFromContainer();
            modeLabel.RemoveFromContainer();

            arena.arenaPrepTimer = null;
        }
    }
}