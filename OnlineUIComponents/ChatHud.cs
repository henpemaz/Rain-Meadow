using System.Collections.Generic;
using HUD;
using Rewired;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class ChatHud : HudPart, IChatSubscriber
    {
        private TextPrompt textPrompt;
        private RoomCamera camera;
        private RainWorldGame game;
        public int currentLogIndex = 0;

        private ChatLogOverlay? chatLogOverlay;
        private ChatInputOverlay? chatInputOverlay;
        public bool chatInputActive => chatInputOverlay is not null;
        public bool showChatLog = false;

        public List<(string, string)> chatLog = new();

        public bool Active => game.processActive;
        public bool ShouldForceCloseChat => game.pauseMenu != null || camera.hud?.map?.visible == true || game.manager.upcomingProcess != null || slatedForDeletion;

        public static bool isLogToggled
        {
            get => RainMeadow.rainMeadowOptions.ChatLogOnOff.Value;
            set => RainMeadow.rainMeadowOptions.ChatLogOnOff.Value = value;
        }
        public ChatHud(HUD.HUD hud, RoomCamera camera) : base(hud)
        {
            textPrompt = hud.textPrompt;
            this.camera = camera;
            game = camera.game;

            ChatLogManager.Subscribe(this);
            if (!ChatLogManager.shownChatTutorial)
            {
                hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("Press '") + (RainMeadow.rainMeadowOptions.ChatButtonKey.Value) + hud.rainWorld.inGameTranslator.Translate("' to chat, press '") + (RainMeadow.rainMeadowOptions.ChatLogKey.Value) + hud.rainWorld.inGameTranslator.Translate("' to toggle the chat log"), 60, 320, true, true);
                ChatLogManager.shownChatTutorial = true;
            }

            if (isLogToggled)
            {
                RainMeadow.Debug("creating log");
                chatLogOverlay = new ChatLogOverlay(this, game.manager);
                showChatLog = true;
            }

            ChatTextBox.OnShutDownRequest += ShutDownChatInput;
        }

        public void AddMessage(string user, string message)
        {
            if (!Active) return;
            if (OnlineManager.lobby == null) return;

            if (OnlineManager.lobby.gameMode.mutedPlayers.Contains(user)) return;
            chatLog.Add((user, message));
            if (chatInputActive) currentLogIndex = 0;
            chatLogOverlay?.UpdateLogDisplay();
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            if (chatInputOverlay is null && Input.GetKeyUp(RainMeadow.rainMeadowOptions.ChatLogKey.Value))
            {
                if (chatLogOverlay is not null)
                {
                    ShutDownChatLog();
                    showChatLog = false;
                    isLogToggled = false;
                }
                else if (!textPrompt.pausedMode && !ShouldForceCloseChat)
                {
                    RainMeadow.Debug("creating log");
                    chatLogOverlay = new ChatLogOverlay(this, game.manager);
                    showChatLog = true;
                    isLogToggled = true;
                }
            }
            if (Input.GetKeyUp(RainMeadow.rainMeadowOptions.ChatButtonKey.Value))
            {
                /*if (chatInputOverlay is not null) remove this cuz blockInput kinda makes this obsolete
                {
                    ShutDownChatInput();
                    if (!showChatLog && chatLogOverlay is not null) ShutDownChatLog();
                }*/
                if (chatInputOverlay == null && !textPrompt.pausedMode && !ShouldForceCloseChat)
                {
                    RainMeadow.Debug("creating input");
                    chatInputOverlay = new ChatInputOverlay(game.manager);
                    if (chatLogOverlay is null)
                    {
                        RainMeadow.Debug("creating log");
                        chatLogOverlay = new ChatLogOverlay(this, game.manager);
                    }
                }
            }
            if (chatInputActive)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    if (currentLogIndex < chatLog.Count - 1)
                    {
                        currentLogIndex++;
                        chatLogOverlay?.UpdateLogDisplay();
                    }
                }

                if (Input.GetKey(KeyCode.DownArrow))
                {
                    if (currentLogIndex > 0)
                    {
                        currentLogIndex--;
                        chatLogOverlay?.UpdateLogDisplay();
                    }
                }
            }
            chatLogOverlay?.GrafUpdate(timeStacker);
            chatInputOverlay?.GrafUpdate(timeStacker);
        }
    
        public void ShutDownChatLog()
        {
            RainMeadow.DebugMe();
            if (chatLogOverlay != null)
            {
                chatLogOverlay.ShutDownProcess();
                chatLogOverlay = null;
            }
        }
        public void ShutDownChatInput()
        {
            RainMeadow.DebugMe();
            if (chatInputOverlay != null)
            {
                chatInputOverlay.chat.DelayedUnload(0.1f);
                chatInputOverlay.ShutDownProcess();
                chatInputOverlay = null;
            }
        }
        public void Destroy()
        {
            RainMeadow.DebugMe();
            if (chatInputOverlay != null) ShutDownChatInput();
            if (chatLogOverlay != null) ShutDownChatLog();
            ChatTextBox.OnShutDownRequest -= ShutDownChatInput;
            ChatLogManager.Unsubscribe(this);
        }
        public override void ClearSprites()
        {
            base.ClearSprites();
            Destroy();
        }
        public override void Update()
        {
            base.Update();
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode) return;

            if (ShouldForceCloseChat)
            {
                if (chatInputOverlay != null) ShutDownChatInput();
                if (chatLogOverlay != null) ShutDownChatLog();
            }
            else if (showChatLog && chatLogOverlay == null)
            {
                chatLogOverlay = new ChatLogOverlay(this, game.manager);
            }
            chatLogOverlay?.Update();
            chatInputOverlay?.Update();
        }
    }
}