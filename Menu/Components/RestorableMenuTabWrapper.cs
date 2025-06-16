using Menu;
using Menu.Remix;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.UI.Components
{
    public class RestorableMenuTabWrapper(Menu.Menu menu, MenuObject owner) : MenuTabWrapper(menu, owner), IRestorableMenuObject
    {
        public FContainer PrevContainer => owner == null? menu.container : owner.Container;
        public override void RemoveSprites()
        {
            myContainer.RemoveFromContainer();
            _tab._container.RemoveFromContainer();
            _glowContainer.RemoveFromContainer();
            for (int i = 0; i < subObjects.Count; i++)
            {
                subObjects[i].RemoveSprites();
            }
        }
        public void RestoreSprites()
        {
            PrevContainer.AddChild(myContainer);
            myContainer.AddChild(_tab._container);
            myContainer.AddChild(_glowContainer);
        }
        public void RestoreSelectables()
        { 
        }
    }
}
