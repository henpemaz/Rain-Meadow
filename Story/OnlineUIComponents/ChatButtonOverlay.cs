using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatButtonOverlay : Menu.Menu
    {
        public ChatOverlay chatOverlay;
        public ChatTextBox chat;
        private int ticker;
        public ChatButtonOverlay(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            pages.Add(new Page(this, null, "chatButton", 0));
            InitChat();
        }

        public void InitChat()
        {
            if (chat != null)
            {
                chat.RemoveSprites();
                this.pages[0].RemoveSprites();
                this.pages[0].RemoveSubObject(chat);
            }

            chat = new ChatTextBox(this, pages[0], "", new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 0), new(750, 30));
            pages[0].subObjects.Add(chat);
        }
    }
}
