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
            roundedRect = new(menu, this, Vector2.zero, this.size, true) { fillAlpha = 0.3f };
            chatTypingBox = new(menu, this, "", new(10, 10), new(this.size.x - 30, 30), true);
            //chatTypingBox = new(menu, this, "", new(10, 10), new(this.size.x - 30, 30));
            chatTypingBox.OnTextSubmit += () =>
            {
                if (messageScroller != null) messageScroller.MoveAtBottom();
            };
            float posYOffset = chatTypingBox.size.y + 10;
            messageScroller = new(menu, this, new(chatTypingBox.pos.x, chatTypingBox.pos.y + posYOffset), new(chatTypingBox.size.x, this.size.y - chatTypingBox.size.y - chatTypingBox.pos.y - 10), true, new(-5, -posYOffset), posYOffset - 25)
            {
                sliderDefaultIsDown = true,
                buttonHeight = 20,
                buttonSpacing = 3,
                textAnchor = RainMeadow.rainMeadowOptions.ChatTextDownscroll.Value 
                    ? ButtonScroller.TextAnchor.Bottom 
                    : ButtonScroller.TextAnchor.Top
            };
            menu.MutualHorizontalButtonBind(chatTypingBox, messageScroller.scrollSlider);
            subObjects.AddRange([roundedRect, chatTypingBox, messageScroller]);

            for (int i = Mathf.Max(0, ChatLogManager.chatLog.Count - maxVisibleMessages - 1); i < ChatLogManager.chatLog.Count; i++)
            {
                AddNewMessageToScroller(ChatLogManager.chatLog[i].Item1, ChatLogManager.chatLog[i].Item2);
            }
        }
        public AlignedMenuLabel CreateMessageLabel(string? user, string stg, ChatLogManager.SystemMessageType? systemMessageType, bool withUser, Vector2 pos, Vector2 size)
        {
            if (systemMessageType is not null)
            {
                AlignedMenuLabel systemMessageLabel = new(menu, messageScroller, stg, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                systemMessageLabel.label.alignment = FLabelAlignment.Left;
                systemMessageLabel.label.color = ChatLogManager.GetColorOfSystemMessage(systemMessageType);
                return systemMessageLabel;
            }
            if (withUser)
            {
                UsernameMenuLabel userLabel = new(menu, messageScroller, user!, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                userLabel.label.alignment = FLabelAlignment.Left;
                userLabel.label.color = ChatLogManager.GetDisplayPlayerColor(user!, MenuColorEffect.rgbMediumGrey);


                AlignedMenuLabel messageWithUserLabel = new(menu, userLabel, $": {stg}", new(LabelTest.GetWidth(user) + 2 + (userLabel.Host ? 14 : 0), 0), userLabel.size, false)
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

        public List<ButtonScroller.IPartOfButtonScroller> AddEmoteItems(string? user, MeadowProgression.Emote emote, Vector2 pos)
        {
            List<ButtonScroller.IPartOfButtonScroller> messageLabels = [];
            if (user is not null)
            {
                UsernameMenuLabel userLabel = new(menu, messageScroller, user!, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                userLabel.label.alignment = FLabelAlignment.Left;
                userLabel.label.color = ChatLogManager.GetDisplayPlayerColor(user!, MenuColorEffect.rgbMediumGrey);
                pos.y += userLabel.Size.y;
                messageLabels.Add(userLabel);
            }

            var emoteLabel = new ChatEmote(emote, menu, messageScroller, pos);
            messageLabels.Add(emoteLabel);
            int amount = Mathf.FloorToInt((emoteLabel.Size.y - emoteLabel.Margin.y)/messageScroller.ButtonHeightAndSpacing);
            // HACK for differently sized items;
            for (int i = 0; i < amount; i++)
            {
                messageLabels.Add(new ChatSpace(menu, messageScroller, Vector2.zero, Vector2.zero));
            }
            return messageLabels;

        }

        public void AddNewMessageToScroller(string user, string message)
        {
            bool setNewScrollPosToLatest = messageScroller.IsAtBottom();
            messageScroller.AddScrollObjects(CreateMessageLabels(user, message));
            if (setNewScrollPosToLatest) messageScroller.MoveAtBottom();
        }
        public ButtonScroller.IPartOfButtonScroller[] CreateMessageLabels(string user, string message)
        {
            RainMeadow.DebugMe();
            List<AlignedMenuLabel> messageLabels = [];
            ChatLogManager.SystemMessageType? systemMessageType = ChatLogManager.SysMesSignatureToType(user);
            bool isSystemMessage = systemMessageType is not null;

            if (message.StartsWith(":"))
            {
                string emoteMessage =  "emote" + message.TrimStart(':').ToLowerInvariant();
                string symbolMessage =  "symbol" + message.TrimStart(':').ToLowerInvariant();
                RainMeadow.Debug(emoteMessage);
                RainMeadow.Debug(symbolMessage);
                MeadowProgression.Emote? emote = MeadowProgression.emoteEmotes.Find(x => x.value.ToLowerInvariant() == emoteMessage) ?? MeadowProgression.symbolEmotes.Find(x => x.value.ToLowerInvariant() == symbolMessage);
                if (!isSystemMessage && emote is not null)
                {
                    return [.. AddEmoteItems(user, emote, new(5, messageScroller.GetIdealPosWithScrollForButton(messageScroller.buttons.Count).y))];
                }
            }

            float desiredXWidth = messageScroller.size.x - 5;
            Vector2 desiredSize = new(desiredXWidth, messageScroller.buttonHeight);

            bool host = OnlineManager.lobby?.owner.id.GetPersonaName() == user;
            List<string> splitMessages = [.. MenuHelpers.SmartSplitIntoFixedStrings($"{message}", desiredXWidth - (isSystemMessage ? 0 : LabelTest.GetWidth($"{user}: ", false) + (host ? 14f : 0)), 1, out string remainingMessage)];
            splitMessages.AddRange(MenuHelpers.SmartSplitIntoStrings(remainingMessage, desiredXWidth));
            for (int i = 0; i < splitMessages.Count; i++)
                messageLabels.Add(CreateMessageLabel(user, splitMessages[i], systemMessageType, i == 0, new(5, messageScroller.GetIdealPosWithScrollForButton(i + messageScroller.buttons.Count).y), desiredSize));
            return [.. messageLabels];
        }
        public void AddMessage(string user, string message)
        {
            if (ChatLogManager.ShouldMuteMessageFromUser(user)) return;
            
            MatchmakingManager.currentInstance.FilterMessage(ref message);
            if (ChatLogManager.ShouldPingFromMessage(user, message))
            {
                menu.manager.menuMic.PlaySound(RainMeadow.Ext_SoundID.RM_Slugcat_Call, 0, 1f, 1.2f);
            }
            if (ChatLogManager.ShouldMakeSoundFromMessage(user, message, out bool quiet))
            {
                menu.manager.menuMic.PlaySound(
                    quiet ? SoundID.MENU_First_Scroll_Tick : SoundID.MENU_Scroll_Tick, 
                    0, 
                    quiet ? 0.7f : 1.5f, 
                    quiet ? 0.7f : 0.6f
                );
            }
            AddNewMessageToScroller(user, message);
        }


        public RoundedRect roundedRect;
        public ChatTextBox chatTypingBox;
        public ButtonScroller messageScroller;
        private const int maxVisibleMessages = 25;
    }
}
