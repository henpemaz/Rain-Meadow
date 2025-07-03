using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace RainMeadow
{
    public class ChatLogOverlay : Menu.Menu
    {
        public (string, string)[] myChatLog = [];
        public ButtonScroller scroller; //idk, makes things easier to manage ;-;
        private ChatHud chatHud;
        private FSprite chatBg;
        private float bgSideOffset = 20, bgSideYOffset = 10;
        private const int maxVisibleMessages = 13;

        public ChatLogOverlay(ChatHud chatHud, ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatHud = chatHud;
            pages.Add(new Page(this, null, "chat", 0));
            chatBg = new("pixel")
            {
                anchorX = 0,
                anchorY = 0,
                color = Color.black,
                alpha = 0.2f,
            };
            pages[0].Container.AddChild(chatBg);
            scroller = new(this, pages[0], new(1366f - 660f - manager.rainWorld.screenSize.x / 2 - bgSideOffset, 330 - maxVisibleMessages * 20), new(manager.rainWorld.screenSize.x / 2.7f + bgSideOffset, maxVisibleMessages * 20))
            {
                buttonHeight = 20,
            };
            scroller.ClearMenuObject(scroller.scrollSlider); //dont make it null but clear it
            pages[0].subObjects.Add(scroller);
            UpdateLogDisplay();
            scroller.scrollOffset = scroller.DownScrollOffset = chatHud.logScrollPos == -1? scroller.MaxDownScroll : chatHud.logScrollPos;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 pos = scroller.DrawPos(timeStacker), size = scroller.DrawSize(timeStacker);
            chatBg.x = pos.x;
            chatBg.y = pos.y - bgSideYOffset;
            chatBg.scaleX = size.x;
            chatBg.scaleY = size.y + bgSideYOffset * 2;


        }
        public void UpdateLogDisplay()
        {
            if (chatHud.chatLog.Count > myChatLog.Length)
            {
                ChatLogManager.UpdatePlayerColors();
                float desiredXWidth = scroller.size.x - bgSideOffset * 2, xPos = bgSideOffset;
                var newMessages = chatHud.chatLog.Skip(myChatLog.Length);
                foreach (var (username, message) in newMessages)
                {
                    bool isSystemMessage = username is null or "";
                    List<string> splitMessages = [.. MenuHelpers.SmartSplitIntoFixedStrings($"{message}", desiredXWidth - (isSystemMessage ? 0 : LabelTest.GetWidth($"{username}: ", false)), 1, out string remainingMessage)];
                    splitMessages.AddRange(MenuHelpers.SmartSplitIntoStrings(remainingMessage, desiredXWidth));
                    for (int i = 0; i < splitMessages.Count; i++)
                    {
                        float yPos = scroller.GetIdealYPosWithScroll(scroller.buttons.Count);
                        string s = splitMessages[i];
                        if (isSystemMessage)
                        {
                            AlignedMenuLabel systemMessageLabel = new(this, scroller, s, new Vector2(xPos, yPos), new Vector2(0, 20), false);
                            systemMessageLabel.label.alignment = FLabelAlignment.Left;
                            systemMessageLabel.label.color = ChatLogManager.defaultSystemColor;
                            scroller.AddScrollObjects(systemMessageLabel);
                        }
                        else if (i == 0)
                        {
                            AlignedMenuLabel usernameLabel = new(this, scroller, username!, new Vector2(xPos, yPos), new Vector2(0, 20), false);
                            usernameLabel.label.alignment = FLabelAlignment.Left;
                            usernameLabel.label.color = ChatLogManager.GetDisplayPlayerColor(username!);

                            AlignedMenuLabel messagewithUserLabel = new(this, usernameLabel, $": {s}", new Vector2(LabelTest.GetWidth(username) + 2, 0), new Vector2(0, 20), false)
                            { labelPosAlignment = FLabelAlignment.Left };
                            messagewithUserLabel.label.alignment = FLabelAlignment.Left;
                            usernameLabel.subObjects.Add(messagewithUserLabel);

                            scroller.AddScrollObjects(usernameLabel);
                        }
                        else
                        {
                            AlignedMenuLabel messageLabel = new(this, scroller, s, new Vector2(xPos, yPos), new Vector2(0, 20), false);
                            messageLabel.label.alignment = FLabelAlignment.Left;
                            scroller.AddScrollObjects(messageLabel);
                        }
                    }
                }
                myChatLog = [.. chatHud.chatLog];

            }
        }
    }
}