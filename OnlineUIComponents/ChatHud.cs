using System;
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
        public RoomCamera camera;
        private RainWorldGame game;
        private ChatLogOverlay? chatLogOverlay;
        private ChatInputOverlay? chatInputOverlay;
        public bool chatInputActive => chatInputOverlay is not null;
        public bool showChatLog = false;

        public float logScrollPos = -1;

        public bool Active => true; //=> game.processActive;
        public bool ShouldForceCloseChat => game.pauseMenu != null || camera.hud?.map?.visible == true || slatedForDeletion;
        public bool InGameTransition => game.processActive || game.manager.upcomingProcess != null;

        public static bool isLogToggled
        {
            get => RainMeadow.rainMeadowOptions.ChatLogOnOff.Value;
            set => RainMeadow.rainMeadowOptions.ChatLogOnOff.Value = value;
        }
        public ChatHud(RoomCamera camera) : base(RMOverlayHUD.GetOverlay())
        {
            this.textPrompt = camera.hud.textPrompt;
            this.camera = camera;
            this.game = camera.game;

            ChatLogManager.Subscribe(this);
            if (!ChatLogManager.shownChatTutorial)
            {
                this.textPrompt.AddMessage(hud.rainWorld.inGameTranslator.Translate("Press '") + (RainMeadow.rainMeadowOptions.ChatButtonKey.Value) + hud.rainWorld.inGameTranslator.Translate("' to chat, press '") + (RainMeadow.rainMeadowOptions.ChatLogKey.Value) + hud.rainWorld.inGameTranslator.Translate("' to toggle the chat log"), 60, 320, true, true);
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

        public void UpdateCamera(RoomCamera camera)
        {
            this.camera = camera;
            this.game = camera.game;
            this.textPrompt = camera.hud.textPrompt;
        }

        public void AddMessage(string user, string message)
        {
            if (!Active) return;
            if (OnlineManager.lobby == null) return;
            if (ChatLogManager.ShouldMuteMessageFromUser(user)) return;

            MatchmakingManager.currentInstance.FilterMessage(ref message);
            if (ChatLogManager.ShouldPingFromMessage(user, message))
            {
                camera.virtualMicrophone.PlaySound(RainMeadow.Ext_SoundID.RM_Slugcat_Call, 0, 1f, 1.2f);
            }
            if (chatLogOverlay != null)
            {
                if (ChatLogManager.ShouldMakeSoundFromMessage(user, message, out bool quiet))
                {
                    camera.virtualMicrophone.PlaySound(
                        quiet ? SoundID.MENU_First_Scroll_Tick : SoundID.MENU_Scroll_Tick, 
                        0, 
                        quiet ? 0.7f : 1.5f, 
                        quiet ? 0.7f : 0.6f
                    );
                }
                bool shouldGoDown = chatLogOverlay.scroller.DownScrollOffset == chatLogOverlay.scroller.MaxDownScroll;
                chatLogOverlay.UpdateLogDisplay();
                if (shouldGoDown) chatLogOverlay.scroller.scrollOffset = chatLogOverlay.scroller.DownScrollOffset = chatLogOverlay.scroller.MaxDownScroll;
            }
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
            if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.ChatButtonKey.Value))
            {
                // RainMeadow.Debug($"Chat button pressed ! Input is [{chatInputOverlay}], can chat is <{InGameTransition || !textPrompt.pausedMode}><{!ShouldForceCloseChat}>");
                if (chatInputOverlay is null && (InGameTransition || !textPrompt.pausedMode) && !ShouldForceCloseChat)
                {
                    if (ChatTextBox.AnyShift && RainMeadow.rainMeadowOptions.EnableChatLogErrorToggle.Value)
                    {
                        ChatLogManager.ToggleLogErrorInChat();
                    }
                    else
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
            }
            if (chatInputActive)
            {
                if (chatLogOverlay != null)
                {
                    if (Input.GetKey(KeyCode.UpArrow) && !ChatTextBox.AnyCtrl && chatLogOverlay.scroller.CanScrollUp)
                    {
                        chatLogOverlay.scroller.AddScroll(-1);
                        chatLogOverlay.scroller.scrollOffset = chatLogOverlay.scroller.DownScrollOffset;
                    }
                    else if (Input.GetKey(KeyCode.DownArrow) && !ChatTextBox.AnyCtrl && chatLogOverlay.scroller.CanScrollDown)
                    {
                        chatLogOverlay.scroller.AddScroll(1);
                        chatLogOverlay.scroller.scrollOffset = chatLogOverlay.scroller.DownScrollOffset;
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
                logScrollPos = chatLogOverlay.scroller.DownScrollOffset == chatLogOverlay.scroller.MaxDownScroll ? -1 : chatLogOverlay.scroller.DownScrollOffset;
                chatLogOverlay.RemoveSprites();
                chatLogOverlay = null;
            }
        }
        public void ShutDownChatInput()
        {
            RainMeadow.DebugMe();
            if (chatInputOverlay != null)
            {
                if (!string.IsNullOrEmpty(ChatTextBox.lastSentMessage) && chatLogOverlay != null) chatLogOverlay.scroller.scrollOffset = chatLogOverlay.scroller.DownScrollOffset = chatLogOverlay.scroller.MaxDownScroll;
                chatInputOverlay.chat.DelayedUnload(0.1f);
                chatInputOverlay.RemoveSprites();
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
                if (OnlineManager.lobby != null && OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs))
                {
                    cs.isInteracting = false;
                }
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