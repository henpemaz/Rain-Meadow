using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatInputOverlay : MenuObject
    {
        //public ChatLogOverlay chatLogOverlay;  // TODO: should we handle this separate from the toggle?
        public ChatTextBox chat;

        public ChatInputOverlay() : base(RMOverlayHUDMenu.GetOverlayMenu(), RMOverlayHUDMenu.GetOverlayMenu().pages[0])
        {
            RainMeadow.DebugMe();
            chat = new ChatTextBox(this.menu, this, "", new Vector2(0, 0), new(750, 30));
            this.subObjects.Add(chat);
        }
    }
}
