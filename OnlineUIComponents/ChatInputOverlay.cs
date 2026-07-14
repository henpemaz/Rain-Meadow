using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatInputOverlay : MenuObject
    {
        //public ChatLogOverlay chatLogOverlay;  // TODO: should we handle this separate from the toggle?
        public ChatTextBox chat;

        public ChatInputOverlay(ProcessManager manager) : base(RMOverlayHUDMenu.GetOverlayMenu(), RMOverlayHUDMenu.GetOverlayMenu().pages[0])
        {
            chat = new ChatTextBox(this.menu, this, "", new Vector2(manager.rainWorld.options.ScreenSize.x * 0.001f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 0), new(750, 30));
            this.subObjects.Add(chat);
        }
    }
}
