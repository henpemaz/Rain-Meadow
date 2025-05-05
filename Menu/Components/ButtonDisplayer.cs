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
        public ButtonDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfLargeButtonsToView, float listSizeX, float heightOfLargeButton, float largeButtonSpacing, float sideButtonsListXSize = 30) : this(menu, owner, pos, new(listSizeX, CalculateHeightBasedOnAmtOfButtons(amtOfLargeButtonsToView, heightOfLargeButton, largeButtonSpacing)), sideButtonsListXSize)
        {
            buttonHeight = heightOfLargeButton;
            buttonSpacing = largeButtonSpacing;
        }
        public ButtonDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, float sideButtonsListXSize = 30) : base(menu, owner, pos, size)
        {
            xSizeOfSideButtonList = sideButtonsListXSize;
            lines = [new FSprite("pixel"), new FSprite("pixel")]; 
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i].anchorX = 0;
                lines[i].anchorY = 0;
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
            Vector2 linePos = DrawPos(timeStacker),lineSize = DrawSize(timeStacker);
            for (int i = 0; i < 2 && i < lines.Length; i++)
            {
                lines[i].x = linePos.x + lineSize.x + ((xSizeOfSideButtonList + lineThickness) * i);
                lines[i].y = linePos.y;
                lines[i].scaleX = lineThickness;
                lines[i].scaleY = lineSize.y;
                lines[i].color = MenuColorEffect.rgbVeryDarkGrey;
            }
            displayToggleButton.pos.x = size.x + (xSizeOfSideButtonList / 2) - (displayToggleButton.size.x / 2) + lineThickness;
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
            DownScrollOffset = IsCurrentlyLargeDisplay? largeDisplayScrollOffset : smallDisplayScrollOffset;
        }

        protected bool isCurrentlyLargeDisplay = true;
        public float xSizeOfSideButtonList, lineThickness = 2, largeDisplayScrollOffset, smallDisplayScrollOffset;
        public FSprite[] lines;
        public SimplerSymbolButton displayToggleButton;
        public Func<ButtonDisplayer, bool, IPartOfButtonScroller[]>? refreshDisplayButtons; //you can call height change here
    }
}
