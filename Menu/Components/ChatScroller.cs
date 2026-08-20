using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Menu;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class ChatScroller : ButtonScroller, IChatSubscriber //call ChatLogManager.Subscribe/Unsubscribe somewhere in mainprocess
    {
        private const int maxVisibleMessages = 25;
        public void RefreshWithHistory()
        {
            this.RemoveAllButtons();
            for (int i = Mathf.Max(0, ChatLogManager.chatLog.Count - Mathf.CeilToInt(MaxVisibleItemsShown) - 1); i < ChatLogManager.chatLog.Count; i++)
            {
                AddNewMessageToScroller(ChatLogManager.chatLog[i].Item1, ChatLogManager.chatLog[i].Item2);
            }
        }

        public bool FadeOut = false;
        public bool Background = false;
        public bool Active => menu.Active;
        public ChatScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfButtonsToView, float listSizeX, (float, float) buttonHeightSpacing, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeYOffset = 0, bool startEndWithSpacing = false) : 
            base(menu, owner, pos, amtOfButtonsToView, listSizeX, buttonHeightSpacing, sliderOnRight, sliderPosOffset, sliderSizeYOffset, startEndWithSpacing)
        {}
        public ChatScroller(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, bool sliderOnRight = false, Vector2 sliderPosOffset = default, float sliderSizeYOffset = 0) :
            base(menu, owner, pos, size, sliderOnRight, sliderPosOffset, sliderSizeYOffset)
        {}

        private FSprite[] chatBg = [];
        public AlignedMenuLabel CreateMessageLabel(string? user, string stg, ChatLogManager.SystemMessageType? systemMessageType, bool withUser, Vector2 pos, Vector2 size)
        {
            if (systemMessageType is not null)
            {
                AlignedMenuLabel systemMessageLabel = new(menu, this, stg, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                systemMessageLabel.label.alignment = FLabelAlignment.Left;
                systemMessageLabel.label.color = ChatLogManager.GetColorOfSystemMessage(systemMessageType);
                return systemMessageLabel;
            }
            if (withUser)
            {
                UsernameMenuLabel userLabel = new(menu, this, user!, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                userLabel.label.alignment = FLabelAlignment.Left;
                userLabel.label.color = ChatLogManager.GetDisplayPlayerColor(user!, MenuColorEffect.rgbMediumGrey);


                AlignedMenuLabel messageWithUserLabel = new(menu, userLabel, $": {stg}", new(LabelTest.GetWidth(user) + 2 + (userLabel.Host ? 14 : 0), 0), userLabel.size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                messageWithUserLabel.label.alignment = FLabelAlignment.Left;
                userLabel.subObjects.Add(messageWithUserLabel);
                return userLabel;
            }
            AlignedMenuLabel messageLabel = new(menu, this, stg, pos, size, false)
            { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
            messageLabel.label.alignment = FLabelAlignment.Left;
            return messageLabel;
        }

        public List<ButtonScroller.IPartOfButtonScroller> AddEmoteItems(string? user, MeadowProgression.Emote emote, Vector2 pos)
        {
            List<ButtonScroller.IPartOfButtonScroller> messageLabels = [];
            if (user is not null)
            {
                UsernameMenuLabel userLabel = new(menu, this, user!, pos, size, false)
                { labelPosAlignment = FLabelAlignment.Left, verticalLabelPosAlignment = OpLabel.LabelVAlignment.Bottom };
                userLabel.label.alignment = FLabelAlignment.Left;
                userLabel.label.color = ChatLogManager.GetDisplayPlayerColor(user!, MenuColorEffect.rgbMediumGrey);
                pos.y += userLabel.Size.y;
                messageLabels.Add(userLabel);
            }

            var emoteLabel = new ChatEmote(ChatLogManager.emoteDict.GetValueSafe(user!) ?? MeadowProgression.Character.Slugcat, emote, menu, this, pos);
            messageLabels.Add(emoteLabel);
            int amount = Mathf.FloorToInt((emoteLabel.Size.y - emoteLabel.Margin.y)/this.ButtonHeightAndSpacing);
            // HACK for differently sized items;
            for (int i = 0; i < amount; i++)
            {
                messageLabels.Add(new ChatEmoteSpace(menu, this, Vector2.zero, Vector2.zero, emoteLabel));
            }
            return messageLabels;

        }

        public void AddNewMessageToScroller(string user, string message)
        {
            bool setNewScrollPosToLatest = this.IsAtBottom();
            AddScrollObjects(CreateMessageLabels(user, message));
            if (setNewScrollPosToLatest) this.MoveAtBottom();
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
                    return [.. AddEmoteItems(user, emote, new(5, this.GetIdealPosWithScrollForButton(this.buttons.Count).y))];
                }
            }

            float desiredXWidth = this.size.x - 5;
            Vector2 desiredSize = new(desiredXWidth, this.buttonHeight);

            bool host = OnlineManager.lobby?.owner.id.GetPersonaName() == user;
            List<string> splitMessages = [.. MenuHelpers.SmartSplitIntoFixedStrings($"{message}", desiredXWidth - (isSystemMessage ? 0 : LabelTest.GetWidth($"{user}: ", false) + (host ? 14f : 0)), 1, out string remainingMessage)];
            splitMessages.AddRange(MenuHelpers.SmartSplitIntoStrings(remainingMessage, desiredXWidth));
            for (int i = 0; i < splitMessages.Count; i++)
                messageLabels.Add(CreateMessageLabel(user, splitMessages[i], systemMessageType, i == 0, new(5, this.GetIdealPosWithScrollForButton(i + this.buttons.Count).y), desiredSize));
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

            inactivityTimer = 0;

            AddNewMessageToScroller(user, message);
        }

        public override void Update()
        {
            base.Update();
            OpacityUpdate();
            BackGroundUpdate();
        }
        private int inactivityTimer;

        int GetFirstIndex()
        {
            for (int i = 0; i < buttons.Count; ++i)
                if (buttons[i].Alpha >= 0.5f && buttons[i].Pos.y >= LowerBound)
                    return i;
            return 0;
        }

        Rect GetChatBounds(IPartOfButtonScroller scrollerButton)
        {
            if (scrollerButton is AlignedMenuLabel label) 
            {
                var quads = label.label._letterQuadLines;
                if (quads.Length == 0) return Rect.zero;
                Rect bound = new Rect(scrollerButton.Pos, Vector2.zero);
                bound.width = LabelTest.GetWidth(label.label.text, false);
                bound.height = scrollerButton.Size.y;
                
                foreach (var subobj in label.subObjects.OfType<AlignedMenuLabel>())
                {
                    Rect subobjbound = GetChatBounds(subobj);
                    Vector2 min = Vector2.Min(subobjbound.min, bound.min);
                    Vector2 max = Vector2.Max(subobjbound.max, bound.max);
                    bound = Rect.MinMaxRect(min.x, min.y, max.x, max.y); 
                }

                return bound;
            }

            return new Rect(scrollerButton.Pos, scrollerButton.Size);
        }
        

        public void BackGroundUpdate()
        {
            if (!Background) return;
            Rect chatRect = new(pos, Vector2.zero);
            if (buttons.Count > 0)
            {
                // TODO only check messages currently visible
                float right = buttons.Select(x => x.Pos.x + x.Size.x).Max() + 20;
                float top = buttons.Select(x => x.Pos.y + x.Size.y).Max() + 20;
                chatRect.yMax = top;
                chatRect.xMax = right;
            }

            int items = Mathf.CeilToInt(MaxVisibleItemsShown);
            if (chatBg.Length < items)
            {
                int offset = items - chatBg.Length;
                Array.Resize(ref chatBg, items);
                for (int i = chatBg.Length - offset; i < chatBg.Length; i++)
                {
                    chatBg[i] = new("pixel")
                    {
                        anchorX = 0,
                        anchorY = 0,
                        color = Color.black,
                        alpha = Mathf.Clamp01(RainMeadow.rainMeadowOptions.ChatBgOpacity.Value),
                    };
                    this.Container.AddChild(chatBg[i]);
                    if (i == 0) chatBg[i].MoveToBack();
                    else chatBg[i].MoveInFrontOfOtherNode(chatBg[i - 1]);
                }
            }
    
            
            int firstIndex = GetFirstIndex();


            for (int i = 0; i < chatBg.Length; ++i)
            {
                int j = firstIndex + i;
                if (j >= 0 && j < buttons.Count)
                {
                    IPartOfButtonScroller part = buttons[j];
                    Rect subobjbound = GetChatBounds(buttons[j]);
                    // We'll bypass IPartOfButtonScroller.Alpha and modify just the labels directly so
                    // messages fading out work as intended.

                    chatBg[i].x = subobjbound.position.x - 4f;
                    chatBg[i].y = subobjbound.position.y - 20f;
                    chatBg[i].scaleX = subobjbound.width + 8f;
                    chatBg[i].scaleY = ButtonHeightAndSpacing + 1f;
                    chatBg[i].alpha = Opacity * Mathf.Clamp01(RainMeadow.rainMeadowOptions.ChatBgOpacity.Value);
                }
            }
        }
        public void OpacityUpdate()
        {
            // If the chat input is open or we aren't in game we won't check for players.
            if (!FadeOut)
            {
                lastOpacity = 1.0f;
                Opacity = 1.0f;
                inactivityTimer = 0;
                return;
            }

            int firstIndex = GetFirstIndex();


            

            inactivityTimer += 1;
            bool fade = false;
            if (inactivityTimer > RainMeadow.rainMeadowOptions.ChatInactivityTimer.Value * 40)
            {
                fade = true;
            }
            else
            {
                if (Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
                foreach (var avatar in OnlineManager.lobby.playerAvatars)
                {
                    var entity = avatar.Value.FindEntity(true);
                    if (entity is OnlineCreature oc && oc.abstractCreature != null && oc.abstractCreature.realizedCreature != null && !oc.abstractCreature.realizedCreature.dead)
                    {
                        for (int i = firstIndex; i < buttons.Count; ++i)
                        {
                            Rect chatRect = new(buttons[i].Pos, buttons[i].Size);
                            if (chatRect.Contains(oc.abstractCreature.realizedCreature.mainBodyChunk.pos - game.cameras[0].pos) && oc.roomSession.absroom == game.cameras[0].room.abstractRoom)
                            {
                                // A player avatar is currently being obscured by chat.
                                fade = true;
                                break;
                            }
                        }
                    }

                    if (fade) break;
                }
            }

            if (fade)
            {
                Opacity = Mathf.Max(RainMeadow.rainMeadowOptions.ChatInactivityOpacity.Value, Opacity - 0.05f);
            }
            else
            {
                Opacity = Mathf.Min(1.0f, Opacity + 0.05f);
            }
        }

        public override void RemoveSprites()
        {
            chatBg.Do(x => x.RemoveFromContainer());
            base.RemoveSprites();
        }
    }
}
