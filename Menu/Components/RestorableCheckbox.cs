using Menu;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class RestorableCheckbox : CheckBox, IRestorableMenuObject
    {
        public RestorableCheckbox(Menu.Menu menu, MenuObject owner, CheckBox.IOwnCheckBox reportTo, Vector2 pos, float textWidth, string displayText, string IDString, bool textOnRight = false, string description = "") : base(menu, owner, reportTo, pos, textWidth, displayText, IDString, textOnRight)
        {
            this.description = description;
        }
        public string Description { get => description; set => description = value; }
        public void RestoreSprites()
        {
            for (int i = 0; i < roundedRect.sprites.Length; i++) Container.AddChild(roundedRect.sprites[i]);
            Container.AddChild(label.label);
            Container.AddChild(symbolSprite);
        }
        public void RestoreSelectables()
        {
            page.selectables.Add(this);
        }
        protected string description;
    }
}
