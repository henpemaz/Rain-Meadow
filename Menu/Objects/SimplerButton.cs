using Menu;
using RainMeadow.UI.Interfaces;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class SimplerButton : SimpleButton, IHaveADescription, IRestorableMenuObject
    {
        public SimplerButton(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size, string description = "") : base(menu, owner, displayText, "", pos, size)
        {
            this.description = description;
        }
        public void RestoreSprites()
        {
            foreach (FSprite sprite in roundedRect.sprites)
            {
                roundedRect.Container.AddChild(sprite);
            }
            foreach (FSprite sprite in selectRect.sprites)
            {
                selectRect.Container.AddChild(sprite);
            }
            menuLabel.Container.AddChild(menuLabel.label);
        }
        public void RestoreSelectables()
        {
            page.selectables.Add(this);
        }
        public string description;
        public string Description
        {
            get => description;
            set => description = value;
        }

        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public event Action<SimplerButton> OnClick;
    }
}
