﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class StoryMenuPlayerButton : ButtonScroller.ScrollerButton
    {
        public StoryMenuPlayerButton(Menu.Menu menu, MenuObject owner, OnlinePlayer oP, bool canKick, Vector2 size = default) : base(menu, owner, oP.id.name, Vector2.zero, size == default ? new(110, 30) : size)
        {
            OnClick += (_) => 
            { 
                oP.id.OpenProfileLink(); 
            };
            if (canKick)
            {
                kickButton = new(menu, this, "Menu_Symbol_Clear_All", "KICKPLAYER", new(this.size.x + 15, 0));
                kickButton.OnClick += (_) =>
                {
                    BanHammer.BanUser(oP);
                };
                subObjects.Add(kickButton);
            }
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            this.ClearMenuObject(ref kickButton);
        }
        public override void UpdateAlpha(float alpha)
        {
            base.UpdateAlpha(alpha);
            if (kickButton != null)
            {
                kickButton.symbolSprite.alpha = alpha;
                for (int i = 0; i < kickButton.roundedRect.sprites.Length; i++)
                {
                    kickButton.roundedRect.sprites[i].alpha = alpha;
                    kickButton.roundedRect.fillAlpha = alpha / 2;
                }
                kickButton.GetButtonBehavior.greyedOut = alpha < 1;
            }    
        }
        public SimplerSymbolButton? kickButton;
    }
    public class StoryMenuSlugcatButton : ButtonScroller.ScrollerButton
    {
        public StoryMenuSlugcatButton(Menu.Menu menu, MenuObject owner, SlugcatStats.Name slugcat, Action<SlugcatStats.Name> onReceieveSlugcat, Vector2 size = default) : base(menu, owner, "", Vector2.zero, size == default? new(110, 30) : size)
        {
            slug = slugcat;
            OnClick += (_) =>
            {
                onReceieveSlugcat?.Invoke(slug);
            };
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (menuLabel != null)
            {
                menuLabel.text = menu.Translate(SlugcatStats.getSlugcatName(slug));
            }
        }
        public SlugcatStats.Name slug;
    }
    public class StoryMenuSlugcatSelector : ButtonSelector
    {
        public StoryMenuSlugcatSelector(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfScugsToShow, float spacing, SlugcatStats.Name currentSlugcat, Func<StoryMenuSlugcatSelector, ButtonScroller, StoryMenuSlugcatButton[]> populateSlugButtons) : base(menu, owner, "", pos, new(110, 30), amtOfScugsToShow, spacing, menu.Translate("Press on the button to open/close the slugcat selection list"))
        {
            slug = currentSlugcat;
            populateList = (selector, scroller) =>
            {
                return populateSlugButtons != null ? populateSlugButtons.Invoke((StoryMenuSlugcatSelector)selector, scroller) : [];
            };
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (menuLabel != null)
            {
                menuLabel.text = menu.Translate(SlugcatStats.getSlugcatName(slug));
            }
        }
        public SlugcatStats.Name Slug
        {
            get
            {
                return slug;
            }
            set
            {
                if (value != slug)
                {
                    slug = value;
                    RefreshScrollerList();
                }
            }
        }
        public SlugcatStats.Name slug;
    }
}
