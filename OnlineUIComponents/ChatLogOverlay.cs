using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow
{
    public class ChatLogOverlay : MenuObject
    {
        public ChatScroller scroller;
        private ChatHud chatHud;
        private float bgSideOffset = 20;
        private const int maxVisibleMessages = 13;
        
        public ChatLogOverlay(ChatHud chatHud, ProcessManager manager) : base(RMOverlayHUDMenu.GetOverlayMenu(), RMOverlayHUDMenu.GetOverlayMenu().pages[0])
        {
            // if (chatHud.hud is RMOverlayHUD) this.container = chatHud.hud.fContainers[1];
            
            this.chatHud = chatHud;

            scroller = new(this.menu, this, new(1366f - 660f - manager.rainWorld.screenSize.x / 2 - bgSideOffset, 330 - maxVisibleMessages * 20), new(manager.rainWorld.screenSize.x / 2.7f + bgSideOffset, maxVisibleMessages * 20))
            {
                buttonHeight = 20,
                textAnchor = RainMeadow.rainMeadowOptions.ChatTextDownscroll.Value 
                    ? ButtonScroller.TextAnchor.Bottom 
                    : ButtonScroller.TextAnchor.Top 
            };
            ChatLogManager.Subscribe(scroller);
            scroller.RefreshWithHistory();
            scroller.Background = true;
            this.subObjects.Add(scroller);
            
            scroller.scrollOffset = scroller.DownScrollOffset = chatHud.logScrollPos == -1? scroller.MaxDownScroll : chatHud.logScrollPos;
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
            ChatLogManager.Unsubscribe(scroller);
        }

        public override void Update()
        {
            base.Update();
            scroller.FadeOut = !chatHud.chatInputActive;
        }
    }
}