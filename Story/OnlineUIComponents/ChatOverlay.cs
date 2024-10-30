using System.Collections.Generic;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatOverlay : Menu.Menu
    {
        private List<string> chatLog;
        public static bool isReceived = false;
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatTextBox chat;
        private int ticker;
        public ChatOverlay(ProcessManager manager, RainWorldGame game, List<string> chatLog) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatLog = chatLog;
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
            float yOfftset = 0;
            if (chatLog.Count > 0)
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


                foreach (string message in chatLog)
                {
                    var partsOfMessage = message.Split(':');

                    // Check if we have at least two parts (username and message)
                    if (partsOfMessage.Length >= 2)
                    {
                        string username = partsOfMessage[0].Trim(); // Extract and trim the username
                        if (OnlineManager.lobby.gameMode.usersIDontWantToChatWith.Contains(username))
                        {
                            continue;
                        }
                    }

                    var chatMessageLabel = new MenuLabel(this, pages[0], message, new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f - 660f, 300f - yOfftset), new Vector2(1400f, 30f), false);
                    chatMessageLabel.label.alignment = FLabelAlignment.Left;
                    pages[0].subObjects.Add(chatMessageLabel);
                    yOfftset += 20f;
                }
            }
        }
    }
}
