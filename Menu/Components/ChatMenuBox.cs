using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class ChatMenuBox : RectangularMenuObject, IChatSubscriber //call ChatLogManager.Subscribe/Unsubscribe somewhere in mainprocess
    {
        public bool Active => menu.Active;
        public ChatMenuBox(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            roundedRect = new(menu, this, Vector2.zero, this.size, true);
            chatTypingBox = new(menu, this, new(10, 10), new(this.size.x - 30, 30));
            float posYOffset = chatTypingBox.size.y + 10;
            messageScroller = new(menu, this, new(chatTypingBox.pos.x, chatTypingBox.pos.y + posYOffset), new(chatTypingBox.size.x, this.size.y - chatTypingBox.size.y - chatTypingBox.pos.y - 10), true, new(-35, -posYOffset), posYOffset - 25)
            {
                buttonHeight = 20,
                buttonSpacing = 3,
                sliderDefaultIsDown = true,
            };
            subObjects.AddRange([roundedRect, chatTypingBox, messageScroller]);
        }
        public AlignedMenuLabel GetMessageLabel(string? user, string stg, bool isSystemMessage, bool withUser, Vector2 pos, Vector2 size)
        {
            if (isSystemMessage)
            {
                AlignedMenuLabel systemMessageLabel = new(menu, messageScroller, stg, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom};
                systemMessageLabel.label.alignment = FLabelAlignment.Left;
                systemMessageLabel.label.color = ChatLogManager.defaultSystemColor;
                return systemMessageLabel;
            }
            if (withUser)
            {
                AlignedMenuLabel userLabel = new(menu, messageScroller, user!, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                userLabel.label.alignment = FLabelAlignment.Left;
                userLabel.label.color = ChatLogManager.GetDisplayPlayerColor(user!, MenuColorEffect.rgbMediumGrey);


                AlignedMenuLabel messageWithUserLabel = new(menu, userLabel, $": {stg}", new(LabelTest.GetWidth(user) + 2, 0), userLabel.size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                messageWithUserLabel.label.alignment = FLabelAlignment.Left;
                userLabel.subObjects.Add(messageWithUserLabel);
                return userLabel;
            }
            AlignedMenuLabel messageLabel = new(menu, messageScroller, stg, pos, size, false)
            { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
            messageLabel.label.alignment = FLabelAlignment.Left;
            return messageLabel;
        }
        public AlignedMenuLabel[] GetMessageLabels (string user, string message)
        {
            List<AlignedMenuLabel> messageLabels = [];
            bool isSystemMessage = user == null || user == "";
            float desiredXWidth = messageScroller.size.x - 5;
            Vector2 desiredSize = new(desiredXWidth, messageScroller.buttonHeight);

            List<string> splitMessages = [..MenuHelpers.SmartSplitIntoFixedStrings($"{message}", desiredXWidth - (isSystemMessage ? 0 : LabelTest.GetWidth($"{user}: ", false)), 1, out string remainingMessage)];
            splitMessages.AddRange(MenuHelpers.SmartSplitIntoStrings(remainingMessage, desiredXWidth));
            for (int i = 0; i < splitMessages.Count; i++)
                messageLabels.Add(GetMessageLabel(user, splitMessages[i], isSystemMessage, i == 0, new(5, messageScroller.GetIdealPosWithScrollForButton(i + messageScroller.buttons.Count).y), desiredSize));
            return [.. messageLabels];
        }
        public void AddMessage(string user, string message)
        {
            if (!(OnlineManager.lobby?.gameMode?.mutedPlayers.Contains(user) == false)) return;
            bool setNewScrollPosToLatest = messageScroller.DownScrollOffset == messageScroller.MaxDownScroll;
            messageScroller.AddScrollObjects(GetMessageLabels(user, message));
            if (setNewScrollPosToLatest) messageScroller.DownScrollOffset = messageScroller.MaxDownScroll;

        }


        public RoundedRect roundedRect;
        public ChatTextBox2 chatTypingBox;
        public ButtonScroller messageScroller;
    }
}
