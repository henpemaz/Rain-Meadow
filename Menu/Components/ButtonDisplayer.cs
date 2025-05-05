using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class ButtonDisplayer : ButtonScroller //basically implements level displayer large, small display. Large Display first
    {
        public bool IsCurrentlyLargeDisplay
        {
            get
            {
                return isCurrentlyLargeDisplay;
            }
            set
            {
                if (isCurrentlyLargeDisplay != value)
                {
                    SaveScroll();
                    isCurrentlyLargeDisplay = value;
                    displayToggleButton.symbolSprite.SetElementByName(GetDisplayButtonSprite);
                    CallForRefresh();
                }
            }
        }
        public virtual string GetDisplayButtonSprite => isCurrentlyLargeDisplay ? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List";
        public ButtonDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfLargeButtonsToView, float listSizeX, float heightOfLargeButton, float largeButtonSpacing)
            : this(menu, owner, pos, new(listSizeX, CalculateHeightBasedOnAmtOfButtons(amtOfLargeButtonsToView, heightOfLargeButton, largeButtonSpacing)))
        {
            buttonHeight = heightOfLargeButton;
            buttonSpacing = largeButtonSpacing;
        }
        public ButtonDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            lines = [new FSprite("pixel"), new FSprite("pixel")];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].anchorX = 0;
                lines[i].anchorY = 0;
                lines[i].scaleX = 2;
                Container.AddChild(lines[i]);
            }
            displayToggleButton = new(menu, this, GetDisplayButtonSprite, "Display_Toggle", new(size.x, Mathf.Min(14.01f, size.y)));
            displayToggleButton.OnClick += (_) =>
            {
                IsCurrentlyLargeDisplay = !IsCurrentlyLargeDisplay;
            };
            subObjects.Add(displayToggleButton);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            lines.Do(x => x.RemoveFromContainer());
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            for (int i = 0; i < lines.Length; i++)
            {
                // RAH RETURN OF THE num1 + num2
                float num1 = (i != 0) ? (displayToggleButton.DrawY(timeStacker) + displayToggleButton.DrawSize(timeStacker).y + 0.01f) : DrawY(timeStacker);
                float num2 = (i != lines.Length - 1) ? (displayToggleButton.DrawY(timeStacker) + 0.01f) : (DrawY(timeStacker) + DrawSize(timeStacker).y + 20f);
                lines[i].x = DrawX(timeStacker) + size.x;
                lines[i].y = num1;
                lines[i].scaleY = num2 - num1;
                lines[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
            }

            displayToggleButton.pos.x = size.x - 8f;
        }
        public void CallForRefresh(bool loadScroll = true)
        {
            RemoveAllButtons(false);
            AddScrollObjects(refreshDisplayButtons?.Invoke(this, IsCurrentlyLargeDisplay));
            if (loadScroll)
            {
                LoadScroll();
            }
        }
        public void SaveScroll()
        {
            if (IsCurrentlyLargeDisplay)
            {
                largeDisplayScrollOffset = DownScrollOffset;
                return;
            }
            smallDisplayScrollOffset = DownScrollOffset;
        }
        public void LoadScroll()
        {
            DownScrollOffset = IsCurrentlyLargeDisplay ? largeDisplayScrollOffset : smallDisplayScrollOffset;
        }

        protected bool isCurrentlyLargeDisplay = true;
        public float largeDisplayScrollOffset, smallDisplayScrollOffset;
        public FSprite[] lines;
        public SimplerSymbolButton displayToggleButton;
        public Func<ButtonDisplayer, bool, IPartOfButtonScroller[]>? refreshDisplayButtons; //you can call height change here
    }
}
