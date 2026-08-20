using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class ChatMenuBox : RectangularMenuObject
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
            messageScroller.RefreshWithHistory();
            ChatLogManager.Subscribe(messageScroller);
        }

        public override void RemoveSprites()
        {
            ChatLogManager.Unsubscribe(messageScroller);
        }



        public RoundedRect roundedRect;
        public ChatTextBox chatTypingBox;
        public ChatScroller messageScroller;
    }
}
