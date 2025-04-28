using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatInputOverlay : Menu.Menu
    {
        //public ChatLogOverlay chatLogOverlay;  // TODO: should we handle this separate from the toggle?
        public ChatTextBox chat;

        public ChatInputOverlay(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            pages.Add(new Page(this, null, "chatButton", 0));
            chat = new ChatTextBox(this, pages[0], "", new Vector2(this.manager.rainWorld.options.ScreenSize.x * 0.001f + (1366f - this.manager.rainWorld.options.ScreenSize.x) / 2f, 0), new(750, 30));
            pages[0].subObjects.Add(chat);
        }
    }
}
