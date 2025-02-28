using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow
{
    public class ChatLogOverlay : Menu.Menu
    {
        private ChatHud chatHud;
        public RainWorldGame game;
        private Dictionary<string, Color> colorDictionary = new();
        private int maxVisibleMessages = 13;
        private int startIndex;

        public Color SYSTEM_COLOR = new(1f, 1f, 0.3333333f);

        public ChatLogOverlay(ChatHud chatHud, ProcessManager manager, RainWorldGame game) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatHud = chatHud;
            this.game = game;
            pages.Add(new Page(this, null, "chat", 0));

            
            UpdateLogDisplay();
        }

        public void UpdateLogDisplay()
        {
            if (chatHud.chatLog.Count > 0)
            {
                startIndex = Mathf.Clamp(chatHud.chatLog.Count - maxVisibleMessages - chatHud.currentLogIndex, 0, chatHud.chatLog.Count - maxVisibleMessages);

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
                var visibleLog = chatHud.chatLog.Skip(startIndex).Take(maxVisibleMessages);
                foreach (var (username, message) in visibleLog)
                {
                    if (username is null or "")
                    {
                        // system message
                        var messageLabel = new MenuLabel(this, pages[0], message,
                            new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                        messageLabel.label.alignment = FLabelAlignment.Left;
                        messageLabel.label.color = SYSTEM_COLOR;
                        pages[0].subObjects.Add(messageLabel);
                    }
                    else
                    {
                        float H = 0f;
                        float S = 0f;
                        float V = 0f;

                        var color = colorDictionary.TryGetValue(username, out var colorOrig) ? colorOrig : Color.white;
                        var colorNew = color;

                        Color.RGBToHSV(color, out H, out S, out V);
                        if (V < 0.8f) { colorNew = Color.HSVToRGB(H, S, 0.8f); }

                        var usernameLabel = new MenuLabel(this, pages[0], username,
                            new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                        usernameLabel.label.alignment = FLabelAlignment.Left;
                        usernameLabel.label.color = colorNew;
                        pages[0].subObjects.Add(usernameLabel);

                        var usernameWidth = LabelTest.GetWidth(usernameLabel.label.text);
                        var messageLabel = new MenuLabel(this, pages[0], $": {message}",
                            new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f + usernameWidth + 2f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                        messageLabel.label.alignment = FLabelAlignment.Left;
                        pages[0].subObjects.Add(messageLabel);
                    }

                    yOffSet += 20f;
                }
            }
        }
    }
}