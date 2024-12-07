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
        private ChatButtonOverlay chatButtonOverlay;
        private RainWorldGame game;
        public static bool chatLogActive = false;
        public static bool chatButtonActive = false;

        public static bool gamePaused;
        private List<string> chatLog = new List<string>();
        private int chatCoolDown = 0;
        private int chatTextButtonCooldown = 0;

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

            if (chatButtonActive)
            {
                game.devToolsActive = false;
                game.devToolsLabel.isVisible = false;
            }

            if (!chatButtonActive)
            {
                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatLogKey.Value) && chatOverlay == null && !chatLogActive && !textPrompt.pausedMode)
                {
                    RainMeadow.Debug("creating overlay");
                    chatOverlay = new ChatOverlay(game.manager, game, chatLog);
                    chatLogActive = true;
                    chatCoolDown = 1;
                }

                if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatLogKey.Value) && chatLogActive && chatCoolDown <= 0) ShutDownChatLog();
            }
            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatTalkingKey.Value) && chatButtonOverlay == null && !chatButtonActive && !textPrompt.pausedMode)
            {
                RainMeadow.Debug("creating chat box");
                chatButtonOverlay = new ChatButtonOverlay(game.manager, game, chatLog);
                chatButtonActive = true;
                chatTextButtonCooldown = 1;

                if (chatOverlay == null && !chatLogActive)
                {
                    RainMeadow.Debug("creating overlay");
                    chatOverlay = new ChatOverlay(game.manager, game, chatLog);
                    chatLogActive = true;
                    chatCoolDown = 1;
                }
            }

            chatOverlay?.GrafUpdate(timeStacker);
            chatButtonOverlay?.GrafUpdate(timeStacker);


            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatTalkingKey.Value) && chatButtonActive && chatTextButtonCooldown <= 0) ShutDownChatButton();

        }
        public void ShutDownChatLog()
        {
            RainMeadow.Debug("shut down chat log overlay");
            if (chatOverlay != null)
            {
                chatOverlay.ShutDownProcess();
                chatOverlay = null;
            }
            chatLogActive = false;
        }

        public void ShutDownChatButton()
        {
            RainMeadow.Debug("shut down chat button");
            if (chatButtonOverlay != null)
            {
                chatButtonOverlay.chat.DelayedUnload(0.1f);
                chatButtonOverlay.ShutDownProcess();
                chatButtonOverlay = null;
            }
            chatButtonActive = false;
        }
        public override void Update()
        {
            base.Update();

            if (OnlineManager.lobby.gameMode is not MeadowGameMode)
            {
                if (RainMeadow.isArenaMode(out var _))
                {
                    if (game.pauseMenu != null || game.manager.upcomingProcess != null)
                    {
                        if (chatButtonOverlay != null)
                        {
                            ShutDownChatButton();
                        }
                        if (chatOverlay != null)
                        {
                            ShutDownChatLog();
                        }
                    }
                }
                else
                {

                    if ((game.pauseMenu != null || camera.hud.map.visible || game.manager.upcomingProcess != null))
                    {
                        if (chatButtonOverlay != null)
                        {
                            ShutDownChatButton();
                        }
                        if (chatOverlay != null)
                        {
                            ShutDownChatLog();
                        }
                    }

                }
                chatOverlay?.Update();
                chatButtonOverlay?.Update();


                if (chatOverlay != null)
                {
                    if (chatCoolDown > 0) chatCoolDown--;
                }
                if (chatButtonOverlay!= null)
                {
                    if (chatTextButtonCooldown > 0) chatTextButtonCooldown--;
                }
            }
        }
    }
}