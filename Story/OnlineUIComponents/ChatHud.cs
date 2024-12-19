using System.Collections.Generic;
using HUD;
using UnityEngine;

namespace RainMeadow
{
    public class ChatHud : HudPart
    {
        private TextPrompt textPrompt;
        private RoomCamera camera;
        private RainWorldGame game;

        private ChatLogOverlay? chatLogOverlay;
        private ChatInputOverlay? chatInputOverlay;
        private bool showChatLog = false;
        
        public List<(string, string)> chatLog = new();

        public ChatHud(HUD.HUD hud, RoomCamera camera) : base(hud)
        {
            textPrompt = hud.textPrompt;
            this.camera = camera;
            game = camera.game;

            ChatLogManager.Subscribe(this);
            if (!ChatLogManager.shownChatTutorial)
            {
                hud.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate($"Press 'Enter' to chat, press '{RainMeadow.rainMeadowOptions.ChatLogKey.Value}' to toggle the chat log"), 60, 160, false, true);
                ChatLogManager.shownChatTutorial = true;
            }
        }

        public void AddMessage(string user, string message)
        {
            if (OnlineManager.lobby.gameMode.mutedPlayers.Contains(user)) return;
            chatLog.Add((user, message));
            while (chatLog.Count > 13) chatLog.RemoveAt(0);
            chatLogOverlay?.UpdateLogDisplay();
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);

            if (chatInputOverlay is null && Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatLogKey.Value))
            {
                if (chatLogOverlay is not null)
                {
                    ShutDownChatLog();
                    showChatLog = false;
                }
                else if (!textPrompt.pausedMode)
                {
                    RainMeadow.Debug("creating log");
                    chatLogOverlay = new ChatLogOverlay(this, game.manager, game);
                    showChatLog = true;
                }
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (chatInputOverlay is not null)
                {
                    ShutDownChatInput();
                    if (!showChatLog && chatLogOverlay is not null) ShutDownChatLog();
                }
                else if (!textPrompt.pausedMode)
                {
                    RainMeadow.Debug("creating input");
                    chatInputOverlay = new ChatInputOverlay(game.manager);
                    if (chatLogOverlay is null)
                    {
                        RainMeadow.Debug("creating log");
                        chatLogOverlay = new ChatLogOverlay(this, game.manager, game);
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
            ChatLogManager.Unsubscribe(this);
        }

        public override void Update()
        {
            base.Update();

            if (slatedForDeletion) { Destroy(); return; }

            if (OnlineManager.lobby.gameMode is MeadowGameMode) return;

            if (game.pauseMenu != null || camera.hud?.map?.visible is true || game.manager.upcomingProcess != null)
            {
                if (chatInputOverlay != null) ShutDownChatInput();
                if (chatLogOverlay != null) ShutDownChatLog();
            }

            chatLogOverlay?.Update();
            chatInputOverlay?.Update();
        }
    }
}
