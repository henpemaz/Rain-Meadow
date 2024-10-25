using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatOverlay : Menu.Menu
    {
        private List<string> chatLog;
        public static bool isReceived = false;
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatTextBox chat;
        private int ticker;
        public ChatOverlay(ProcessManager manager, RainWorldGame game, List<string> chatLog) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatLog = chatLog;
            this.game = game;
            pages.Add(new Page(this, null, "chat", 0));
            InitChat();
            isReceived = true;
            ticker = 100;
        }

        public override void Update()
        {

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
            float yOfftset = 0;
            foreach (string message in chatLog)
            {
                var chatMessageLabel = new MenuLabel(this, pages[0], message, new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f - 660f, 300f - yOfftset), new Vector2(1400f, 30f), false);
                chatMessageLabel.label.alignment = FLabelAlignment.Left;
                pages[0].subObjects.Add(chatMessageLabel);
                yOfftset += 20f;
            }
        }

        public void InitChat()
        {
            if (chat != null)
            {
                chat.RemoveSprites();
                this.pages[0].RemoveSprites();
                this.pages[0].RemoveSubObject(chat);
            }

            chat = new ChatTextBox(this, pages[0], "", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 700f, 0), new(1400, 30));
            pages[0].subObjects.Add(chat);

        }
    }
}