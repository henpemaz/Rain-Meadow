using UnityEngine;
using Menu;

namespace RainMeadow
{
    public abstract class ChatButton : ButtonTemplate
    {
        public HSLColor labelColor;

        public MenuLabel menuLabel;

        public RoundedRect roundedRect;

        public ChatButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            RainMeadow.Error("This is my parent constructor coming to life");
            labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.White);
            roundedRect = new RoundedRect(menu, owner, new Vector2(0f, 0f), size, true);
            this.subObjects.Add(roundedRect);
            menuLabel = new MenuLabel(menu, owner, displayText, new Vector2(-roundedRect.size.x / 2 + 10f, 0f), size, false);
            menuLabel.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(menuLabel);
        }
    }
}
