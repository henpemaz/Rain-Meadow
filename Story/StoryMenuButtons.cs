using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class StoryMenuPlayerButton : ButtonScroller.ScrollerButton
    {
        public StoryMenuPlayerButton(Menu.Menu menu, MenuObject owner, OnlinePlayer oP, bool isHost) : base(menu, owner, oP.id.name, Vector2.zero, new(110, 30))
        {
            onScrollBoxButtonClick = (_) => { oP.id.OpenProfileLink(); };
            canKick = isHost;
            if (canKick)
            {
                //putting kick button's parent to this to update their pos if this pos updates
                kickButton = new(menu, this, "Menu_Symbol_Clear_All", "KICKPLAYER", new(size.x + 15, 0));
                kickButton.OnClick += (_) =>
                {
                    BanHammer.BanUser(oP);
                };
                subObjects.Add(kickButton);
            }
        }
        public override void UpdateFade(float fade)
        {
            base.UpdateFade(fade);
            if (kickButton != null)
            {
                kickButton.symbolSprite.alpha = fade;
                for (int i = 0; i < kickButton.roundedRect.sprites.Length; i++)
                {
                    kickButton.roundedRect.sprites[i].alpha = fade;
                    kickButton.roundedRect.fillAlpha = fade / 2;
                }
            }    
        }
        public bool canKick;
        public SimplerSymbolButton kickButton;
    }
}
