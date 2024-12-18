using System.Collections.Generic;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatOverlay : Menu.Menu
    {
        private ChatHud chatHud;
        public static bool isReceived = false;
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatTextBox chat;
        private Dictionary<string, Color> colorDictionary = new();

        public ChatOverlay(ChatHud chatHud, ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatHud = chatHud;
            this.game = game;
            pages.Add(new Page(this, null, "chat", 0));
            isReceived = true;
        }

        public override void Update()
        {
            if (isReceived)
            {
                UpdateLogDisplay();
                isReceived = false;
            }
        }

        public void UpdateLogDisplay()
        {
            if (chatHud.chatLog.Count > 0)
            {
                var logsToRemove = new List<MenuObject>();

                // First, collect all the logs to remove
                foreach (var log in pages[0].subObjects)
                {
                    log.RemoveSprites();
                    logsToRemove.Add(log);
                }

                // Now remove the logs from the original collection
                foreach (var log in logsToRemove)
                {
                    pages[0].RemoveSubObject(log);
                }

                // username:color
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                {
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo)
                    {
                        if (!colorDictionary.ContainsKey(opo.owner.id.name) && opo.TryGetData<SlugcatCustomization>(out var customization))
                        {
                            colorDictionary.Add(opo.owner.id.name, customization.bodyColor);
                        }
                    }
                }

                float yOffSet = 0;
                foreach (var (username, message) in chatHud.chatLog)
                {
                    var usernameLabel = new MenuLabel(this, pages[0], username,
                        new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f, 330f - yOffSet),
                        new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                    usernameLabel.label.alignment = FLabelAlignment.Left;
                    usernameLabel.label.color = colorDictionary.TryGetValue(username, out var color) ? color : Color.white;

                    var usernameWidth = username.Length * 5;
                    var messageLabel = new MenuLabel(this, pages[0], $": {message}",
                        new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f + usernameWidth + 10, 330f - yOffSet),
                        new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                    messageLabel.label.alignment = FLabelAlignment.Left;

                    pages[0].subObjects.Add(usernameLabel);
                    pages[0].subObjects.Add(messageLabel);
                    yOffSet += 20f;
                }
            }
        }
    }
}
