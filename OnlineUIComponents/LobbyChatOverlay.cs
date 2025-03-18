using Menu.Remix;
using UnityEngine;

namespace RainMeadow
{
    public class LobbyChatOverlay : CustomLobbyMenu
    {
        public LobbyChatTextBox chat;

        public LobbyChatOverlay(ProcessManager manager, Vector2 pos, Vector2 size) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            chat = new LobbyChatTextBox(new Configurable<string>(""), new Vector2(0, 0), 100, 1000);
            new UIelementWrapper(tabWrapper, chat);
        }
    }
}
