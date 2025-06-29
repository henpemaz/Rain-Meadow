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
    public class ButtonDisplayer : ButtonScroller //basically implements level displayer stuff (however it lacks transitions)
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
                    isCurrentlyLargeDisplay = value;
                    menu.PlaySound(isCurrentlyLargeDisplay ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
                    displayToggleButton.symbolSprite.SetElementByName(GetDisplayButtonSprite);
                    displayToggleButton.description = DescriptionOfDisplayButton();
                    CallForRefresh();
                }
            }
        }
        public virtual string GetDisplayButtonSprite => isCurrentlyLargeDisplay ? "Menu_Symbol_Show_Thumbs" : "Menu_Symbol_Show_List";
        public ButtonDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, int amtOfLargeButtonsToView, float listSizeX, (float, float) heightSpacingOfLargeButton, Vector2 sliderPosOffset = default, float sliderSizeYOffset = -40)
            : this(menu, owner, pos, new(listSizeX, CalculateHeightBasedOnAmtOfButtons(amtOfLargeButtonsToView, heightSpacingOfLargeButton.Item1, heightSpacingOfLargeButton.Item2)), sliderPosOffset, sliderSizeYOffset)
        {
            buttonHeight = heightSpacingOfLargeButton.Item1;
            buttonSpacing = heightSpacingOfLargeButton.Item2;
        }
        public ButtonDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, Vector2 sliderPosOffset = default, float sliderSizeYOffset = -40) : base(menu, owner, pos, size, sliderPosOffset: sliderPosOffset == default? new(0, 9) : sliderPosOffset, sliderSizeYOffset: sliderSizeYOffset)
        {
            greyOutWhenNoScroll = true;
            AddScrollUpDownButtons();
            displayToggleButton = AddSideButton(GetDisplayButtonSprite, "", DescriptionOfDisplayButton(), "Display_Toggle");
            displayToggleButton.OnClick += _ => IsCurrentlyLargeDisplay = !IsCurrentlyLargeDisplay;
            subObjects.Add(displayToggleButton);
        }
        public void CallForRefresh()
        {
            RemoveAllButtons(false);
            AddScrollObjects(refreshDisplayButtons?.Invoke(this, IsCurrentlyLargeDisplay));
            ConstrainScroll();
        }
        public virtual string DescriptionOfDisplayButton() => "";
        protected bool isCurrentlyLargeDisplay = true;
        public SideButton displayToggleButton;
        public Func<ButtonDisplayer, bool, IPartOfButtonScroller[]>? refreshDisplayButtons; //you can call height change here
    }
}
