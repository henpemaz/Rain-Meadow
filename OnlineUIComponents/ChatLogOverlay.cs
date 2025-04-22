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
        private const int maxVisibleMessages = 13;
        private int startIndex;

        public ChatLogOverlay(ChatHud chatHud, ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatHud = chatHud;
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

                ChatLogManager.UpdatePlayerColors();

                float yOffSet = 0;
                var visibleLog = chatHud.chatLog.Skip(startIndex).Take(maxVisibleMessages);
                foreach (var (username, message) in visibleLog)
                {
                    if (username is null or "")
                    {
                        // system message
                        var messageLabel = new MenuLabel(this, pages[0], message,
                            new Vector2(1366f - manager.rainWorld.screenSize.x - 660f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.screenSize.x, 30f), false);
                        messageLabel.label.alignment = FLabelAlignment.Left;
                        messageLabel.label.color = ChatLogManager.defaultSystemColor;
                        pages[0].subObjects.Add(messageLabel);
                    }
                    else
                    {
                        var color = ChatLogManager.GetDisplayPlayerColor(username);

                        var usernameLabel = new MenuLabel(this, pages[0], username,
                            new Vector2(1366f - manager.rainWorld.screenSize.x - 660f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.screenSize.x, 30f), false);
                        usernameLabel.label.alignment = FLabelAlignment.Left;
                        usernameLabel.label.color = color;
                        pages[0].subObjects.Add(usernameLabel);

                        var usernameWidth = LabelTest.GetWidth(usernameLabel.label.text);
                        var messageLabel = new MenuLabel(this, pages[0], $": {message}",
                            new Vector2(1366f - manager.rainWorld.screenSize.x - 660f + usernameWidth + 2f, 330f - yOffSet),
                            new Vector2(manager.rainWorld.screenSize.x, 30f), false);
                        messageLabel.label.alignment = FLabelAlignment.Left;
                        pages[0].subObjects.Add(messageLabel);
                    }

                    yOffSet += 20f;
                }
            }
        }
    }
}