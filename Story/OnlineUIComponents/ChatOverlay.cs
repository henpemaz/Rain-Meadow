using System;
using System.Collections.Generic;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow
{
    public class ChatOverlay : Menu.Menu
    {
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        private List<string> chatLog;
        private List<MenuLabel> chatLabels = new();
        public static bool isReceived = false;
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public static ChatTextBox chat;
        public ChatOverlay(ProcessManager manager, RainWorldGame game, List<string> chatLog) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            gameObject ??= new GameObject();
            typingHandler ??= gameObject.AddComponent<ButtonTypingHandler>();
            this.chatLog = chatLog;
            this.game = game;
            pages.Add(new Page(this, null, "chat", 0));
            InitChat();
        }
        
        public override void ShutDownProcess()
        {
            chatLabels.Clear();
            chat.Unassign();
            chat.DelayedUnload(0.5f);
            base.ShutDownProcess();
            typingHandler.OnDestroy();
        }
        public override void Update()
        {
            base.Update();
            if (isReceived)
            {
                UpdateLogDisplay();
                isReceived = false;
            }
            if (ModManager.DevTools)
            {
                game.devToolsActive = false;
                game.devToolsLabel.isVisible = game.devToolsActive;
            }
        }

        public void UpdateLogDisplay()
        {
            foreach (var label in chatLabels)
            {
                pages[0].subObjects.Remove(label);
            }
            chatLabels.Clear();
            float yOfftset = 0;
            foreach (string message in chatLog)
            {
                var chatMessageLabel = new MenuLabel(this, pages[0], message, new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f - 660f, 300f - yOfftset), new Vector2(1400f, 30f), false);
                chatMessageLabel.label.alignment = FLabelAlignment.Left;
                pages[0].subObjects.Add(chatMessageLabel);
                yOfftset += 20f;
                RainMeadow.Debug($"Message log part: {message}");
            }
        }

        public void InitChat()
        {
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {
                chat = new ChatTextBox(this, pages[0], "", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 700f, 0), new(1400, 30));
                pages[0].subObjects.Add(chat);
            }
        }
    }
}