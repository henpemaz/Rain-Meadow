using System.Collections.Generic;
using System.Linq;
using HUD;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class ChatHud : HudPart
    {
        private TextPrompt textPrompt;
        private RoomCamera camera;
        private ChatOverlay chatOverlay;
        private RainWorldGame game;
        public static bool chatActive = false;
        public static bool gamePaused;
        private List<string> chatLog = new List<string>();
        private int chatCoolDown = 0;
        public static bool messageRecieved = false;

        public ChatHud(HUD.HUD hud, RoomCamera camera) : base(hud)
        {
            textPrompt = hud.textPrompt;
            this.camera = camera;
            game = camera.game;

            ChatLogManager.Initialize(this);


        }


        public void AddMessage(string message)
        {
            chatLog.Add($"{message}");
            if (chatLog.Count > 13) chatLog.RemoveAt(0);
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            gamePaused = textPrompt.pausedMode;
            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatKey.Value) && chatOverlay == null && !chatActive && !textPrompt.pausedMode)
            {
                RainMeadow.Debug("creating chat box");
                chatOverlay = new ChatOverlay(game.manager, game, chatLog);
                chatActive = true;
                chatCoolDown = 1;
            }

            chatOverlay?.GrafUpdate(timeStacker);

            if (Input.GetKeyDown(KeyCode.Return) && chatActive && chatCoolDown <= 0) ShutDownChat();
        }
        public void ShutDownChat()
        {
            RainMeadow.Debug("shut down chat overlay");
            if (chatOverlay != null)
            {
                chatOverlay.ShutDownProcess();
                chatOverlay = null;
            }
            chatActive = false;
        }
        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby.gameMode is not MeadowGameMode)
            {
                if (RainMeadow.isArenaMode(out var _))
                {
                    if (game.pauseMenu != null || game.manager.upcomingProcess != null && chatOverlay != null)
                    {
                        ShutDownChat();
                    }
                }
                else
                {

                    if ((game.pauseMenu != null || camera.hud.map.visible || game.manager.upcomingProcess != null) && chatOverlay != null)
                    {
                        ShutDownChat();
                    }

                }
                chatOverlay?.Update();

                if (chatOverlay != null)
                {

                    if (chatCoolDown > 0) chatCoolDown--;
                }
            }
        }
    }
}