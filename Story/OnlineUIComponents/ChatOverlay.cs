using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatOverlay : Menu.Menu
    {
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatOverlay(ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.game = game;
            pages.Add(new Page(this, null, "chat", 0));
            InitChat();
        }
        public void InitChat()
        {
            if (OnlineManager.lobby.gameMode is StoryGameMode)
            {
                var chat = new ChatTextBox(this, this.pages[0], "", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 700f, 0), new(1400, 30));
                pages[0].subObjects.Add(chat);
            }
        }
    }
}