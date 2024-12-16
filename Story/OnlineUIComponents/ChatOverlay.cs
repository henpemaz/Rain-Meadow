using System.Collections.Generic;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class ChatOverlay : Menu.Menu
    {
        private List<FSprite> sprites;
        private List<string> chatLog;
        private List<string> userLog;
        private Color userColor;
        public static bool isReceived = false;
        public RainWorldGame game;
        public ChatOverlay chatOverlay;
        public ChatTextBox chat;
        private bool displayBg = false;
        private int ticker;
        private string username;
        private Dictionary<string, Color> colorDictionary;
        public ChatOverlay(ProcessManager manager, RainWorldGame game, List<string> chatLog, List<string> userLog, Color userColor) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.userLog = userLog;
            this.chatLog = chatLog;

            this.userColor = userColor;
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
            sprites = new();
            float yOffSet = 0;

            if (!displayBg)
            {
                displayBg = true;
                var background = new FSprite("pixel")
                {
                    color = new Color(0f, 0f, 0f),
                    anchorX = 0f,
                    anchorY = 0f,
                    y = 32,
                    scaleY = 330,
                    scaleX = 755,
                    alpha = 0.5f
                };
                pages[0].Container.AddChild(background);
                sprites.Add(background);
            }
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

                colorDictionary = new Dictionary<string, Color>();
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                    {
                        if (!colorDictionary.ContainsKey(opo.owner.id.name))
                        {
                            if (opo.TryGetData<SlugcatCustomization>(out var customization))
                            {
                                {
                                    colorDictionary.Add(opo.owner.id.name, customization.bodyColor);

                                }
                            }
                            else
                            {
                                colorDictionary.Add(opo.owner.id.name, Color.white);

                            }
                        }


                    }
                }

                foreach (string message in chatLog)
                {
                    var nameLength = 0;
                    foreach (string user in userLog)
                    {
                        var partsOfMessage = user.Split(':'); // gotta make sure this works too

                        // Check if we have at least two parts (username and message)
                        if (partsOfMessage.Length >= 2)
                        {
                            username = partsOfMessage[0].Trim(); // Extract and trim the username
                            if (OnlineManager.lobby.gameMode.mutedPlayers.Contains(username))
                            {
                                continue;
                            }

                            var userLabel = new MenuLabel(this, pages[0], username, new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f, 330f - yOffSet), new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                            userLabel.label.alignment = FLabelAlignment.Left;
                            //userLabel.label.text = username;
                            userLabel.label.color = colorDictionary[username];
                            pages[0].subObjects.Add(userLabel);
                            nameLength = username.Length;
                        }


                    }

                    // I also tested userLabel down here Saddest. Think the yOffset maybe needs to be updated, im not sure.

                    var chatMessageLabel = new MenuLabel(this, pages[0], message, new Vector2((1366f - manager.rainWorld.options.ScreenSize.x) / 2f - 660f + (nameLength * 5) + 5, 330f - yOffSet), new Vector2(manager.rainWorld.options.ScreenSize.x, 30f), false);
                    chatMessageLabel.label.alignment = FLabelAlignment.Left;
                    pages[0].subObjects.Add(chatMessageLabel);
                    yOffSet += 20f;

                }
            }
        }
    }
}
