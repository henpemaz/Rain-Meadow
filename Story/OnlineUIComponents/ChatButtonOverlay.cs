using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatButtonOverlay : Menu.Menu
    {
        private List<string> chatLog;
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatTextBox chat;
        private int ticker;
        public ChatButtonOverlay(ProcessManager manager, RainWorldGame game, List<string> chatLog) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatLog = chatLog;
            this.game = game;
            pages.Add(new Page(this, null, "chatButton", 0));
            InitChat();
        }

        public override void Update()
        {

            if (ModManager.DevTools)
            {
                game.devToolsActive = false;
                game.devToolsLabel.isVisible = game.devToolsActive;
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
