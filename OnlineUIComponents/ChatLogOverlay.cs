using System;
using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow
{
    public class ChatLogOverlay : Menu.Menu
    {
        public (string, string)[] myChatLog = [];
        public ButtonScroller scroller; //idk, makes things easier to manage ;-;
        private ChatHud chatHud;
        private List<float> msgExtents;
        private FSprite[] chatBg;
        private float bgSideOffset = 20;
        private const int maxVisibleMessages = 13;
        private Rect chatRect;

        public float opacity = 1.0f;
        private float lastOpacity = 1.0f;
        public int inactivityTimer;

        private FSprite debug;

        public ChatLogOverlay(ChatHud chatHud, ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.ChatMode)
        {
            this.chatHud = chatHud;
            pages.Add(new Page(this, null, "chat", 0));

            chatBg = [];
            Array.Resize(ref chatBg, maxVisibleMessages);
            for (int i = 0; i < chatBg.Length; ++i)
            {
                chatBg[i] = new("pixel")
                {
                    anchorX = 0,
                    anchorY = 0,
                    color = Color.black,
                    alpha = Mathf.Clamp01(RainMeadow.rainMeadowOptions.ChatBgOpacity.Value),
                };
                pages[0].Container.AddChild(chatBg[i]);
            }
            msgExtents = [];

            scroller = new(this, pages[0], new(1366f - 660f - manager.rainWorld.screenSize.x / 2 - bgSideOffset, 330 - maxVisibleMessages * 20), new(manager.rainWorld.screenSize.x / 2.7f + bgSideOffset, maxVisibleMessages * 20))
            {
                buttonHeight = 20,
            };
            scroller.ClearMenuObject(scroller.scrollSlider); //dont make it null but clear it
            pages[0].subObjects.Add(scroller);
            UpdateLogDisplay();
            scroller.scrollOffset = scroller.DownScrollOffset = chatHud.logScrollPos == -1? scroller.MaxDownScroll : chatHud.logScrollPos;

            chatRect = new Rect(scroller.pos, scroller.size).CloneWithExpansion(20);
            //debug = new("pixel")
            //{
            //    anchorX = 0,
            //    anchorY = 0,
            //    color = Color.red,
            //    alpha = Mathf.Clamp01(RainMeadow.rainMeadowOptions.ChatBgOpacity.Value),
            //};
            //pages[0].Container.AddChild(debug);
        }

        public override void Update()
        {
            base.Update();
            OpacityUpdate();
            inactivityTimer++;
        }

        public override void GrafUpdate(float timeStacker)
        {
            /// Obtains the first visible button index on the scroller
            int GetFirstIndex()
            {
                for (int i = 0; i < scroller.buttons.Count; ++i)
                    if (scroller.buttons[i].Alpha >= 0.5f && scroller.buttons[i].Pos.y >= scroller.LowerBound)
                        return i;
                return 0;
            }
            base.GrafUpdate(timeStacker);

            if (debug != null)
            {
                debug.x = chatRect.x;
                debug.y = chatRect.y;
                debug.width = chatRect.width;
                debug.height = chatRect.height;
            }

            var tOpacity = Mathf.Lerp(lastOpacity, opacity, timeStacker);

            // Make everything "invisible" by default (just 0-sized)
            for (int i = 0; i < chatBg.Length; ++i)
            {
                chatBg[i].scaleX = 0;
                chatBg[i].scaleY = 0;
            }
            int firstIndex = GetFirstIndex();
            float longestMessage = 0;
            for (int i = 0; i < chatBg.Length; ++i)
            {
                int j = firstIndex + i;
                if (j >= 0 && j < scroller.buttons.Count)
                {
                    // We'll bypass IPartOfButtonScroller.Alpha and modify just the labels directly so
                    // messages fading out work as intended.
                    if (scroller.buttons[j] is AlignedMenuLabel label)
                    {
                        label.label.alpha = tOpacity;
                        foreach(var subObj in label.subObjects)
                        {
                            if (subObj is AlignedMenuLabel sub) sub.label.alpha = tOpacity;
                        }
                    }

                    chatBg[i].x = scroller.pos.x + scroller.buttons[j].Pos.x - 4f;
                    chatBg[i].y = scroller.pos.y + scroller.buttons[j].Pos.y;
                    chatBg[i].scaleX = msgExtents[j] + 8f;
                    chatBg[i].scaleY = scroller.ButtonHeightAndSpacing + 1f;
                    chatBg[i].alpha = tOpacity * (scroller.buttons[j].Alpha * Mathf.Clamp01(RainMeadow.rainMeadowOptions.ChatBgOpacity.Value));
                }
            }
        }

        public void OpacityUpdate()
        {
            // If the chat input is open or we aren't in game we won't check for players.
            if (chatHud.chatInputActive || chatHud.camera is null)
            {
                lastOpacity = 1.0f;
                opacity = 1.0f;
                inactivityTimer = 0;
                return;
            }

            lastOpacity = opacity;
            if (msgExtents.Count > 0)
            {
                // TODO only check messages currently visible
                chatRect.width = msgExtents.Max() + 20;
            }

            bool fade = false;

            if (inactivityTimer > 1200)
            {
                fade = true;
            }
            else
            {
                foreach (var avatar in OnlineManager.lobby.playerAvatars)
                {
                    var entity = avatar.Value.FindEntity(true);
                    if (entity is OnlineCreature oc && oc.abstractCreature != null && oc.abstractCreature.realizedCreature != null && !oc.abstractCreature.realizedCreature.dead)
                    {
                        if (chatRect.Contains(oc.abstractCreature.realizedCreature.mainBodyChunk.pos - chatHud.camera.pos))
                        {
                            // A player avatar is currently being obscured by chat.
                            fade = true;
                            break;
                        }
                    }
                }
            }

            if (fade)
            {
                opacity = Mathf.Max(0.35f, opacity - 0.05f);
            }
            else
            {
                opacity = Mathf.Min(1.0f, opacity + 0.05f);
            }
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
                            msgExtents.Add(LabelTest.GetWidth(s) + 2f);
                        }
                        else if (i == 0)
                        {
                            UsernameMenuLabel usernameLabel = new(this, scroller, username!, new Vector2(xPos, yPos), new Vector2(0, 20), false);
                            usernameLabel.label.alignment = FLabelAlignment.Left;
                            usernameLabel.label.color = ChatLogManager.GetDisplayPlayerColor(username!);

                            AlignedMenuLabel messagewithUserLabel = new(this, usernameLabel, $": {s}", new Vector2(LabelTest.GetWidth(username) + 2 + (usernameLabel.Host ? 16 : 0), 0), new Vector2(0, 20), false)
                            { labelPosAlignment = FLabelAlignment.Left };
                            messagewithUserLabel.label.alignment = FLabelAlignment.Left;
                            usernameLabel.subObjects.Add(messagewithUserLabel);
                            scroller.AddScrollObjects(usernameLabel);
                            msgExtents.Add(LabelTest.GetWidth($"{username}: {s}") + 4f + (usernameLabel.Host ? 16f : 0));
                        }
                        else
                        {
                            AlignedMenuLabel messageLabel = new(this, scroller, s, new Vector2(xPos, yPos), new Vector2(0, 20), false);
                            messageLabel.label.alignment = FLabelAlignment.Left;
                            scroller.AddScrollObjects(messageLabel);
                            msgExtents.Add(LabelTest.GetWidth(s) + 4f);
                        }
                    }
                }
                myChatLog = [.. chatHud.chatLog];
                inactivityTimer = 0;
            }
        }
    }
}