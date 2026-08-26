using Menu;
using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Systems
{
    public class MenuScrollObject : ScrollObject
    {
        public static ConditionalWeakTable<MenuObject, MenuScrollObject> menuScrollObjects = new();
        public readonly MenuObject menuObject;
        public bool isValidForScroller;
        public FContainer objectContainer; //default is myContainer, you can change this
        public float desiredAlpha = 1;
        public override float LocalAlpha { get => desiredAlpha; set => desiredAlpha = value; }
        public override Vector2 LocalPos 
        {
            get
            {
                if (menuObject is not PositionedMenuObject posObj) return Vector2.zero;
                    return posObj.pos;
            }
            set
            {
                if (menuObject is PositionedMenuObject posObj)
                    posObj.pos = value;
            }
        }
        public override Vector2 Size
        {
            get
            {
                if (menuObject is not RectangularMenuObject rectMenuObj) return Vector2.zero;
                return rectMenuObj.size;
            }
            set
            {
                if (menuObject is RectangularMenuObject rectMenuObj)
                    rectMenuObj.size = value;
            }
        }
        public MenuScrollObject(MenuObject menuObject, bool validForScroller)
        {
            this.menuObject = menuObject;
            isValidForScroller = validForScroller;
            TryInitiateContainer();
        }
        public static bool TryGetScrollObjectFromMenuObject(MenuObject menuObject, out MenuScrollObject? menuScrollObj)
        {
            menuScrollObj = null;
            if (menuObject is IOwnMenuScrollObject)
                menuScrollObj = new MenuScrollObject(menuObject, true);
            else if (menuObject is ButtonScroller.IPartOfButtonScroller scrollerObj)
                menuScrollObj = new ButtonScrollerPartObject(scrollerObj);
            return menuScrollObj != null;
        }
        public static MenuScrollObject GetScrollObjectFromMenuObject(MenuObject menuObject)
        {
            if (TryGetScrollObjectFromMenuObject(menuObject, out MenuScrollObject scrollObj))
                return scrollObj;
            return new MenuScrollObject(menuObject, false);
        }
        public void AddSubobjectsToScroller(MenuObject parent, ScrollObject owner)
        {
            for (int i = 0; i < parent.subObjects.Count; i++)
            {
                var sub = parent.subObjects[i];
                sub.GetScrollObject().ParentAddedIntoScroller(owner);
            }
        }
        public virtual void TryInitiateContainer()
        {
            if (isValidForScroller) //IownMenuScrollObj, this gets called right after menuobj.ctor
            {
                objectContainer = new();
                if (menuObject.myContainer == null)
                {
                    var origContainer = menuObject.Container;
                    objectContainer.AddChild(menuObject.myContainer = new());
                    origContainer.AddChild(objectContainer);     
                }
                else if (objectContainer != menuObject.myContainer.container)
                {
                    menuObject.myContainer.container.AddChild(objectContainer);
                    objectContainer.AddChild(menuObject.myContainer);
                }
                return;
            }
            objectContainer = menuObject.Container;
        }
        public override void GrafUpdateInObject(float timeStacker)
        {
            base.GrafUpdateInObject(timeStacker);
            objectContainer.alpha = LocalAlpha;
        }
        public override void OnRemovedFromScroller() //assuming this gets destroyed immediately
        {
            base.OnRemovedFromScroller();
        }
        public override void AddedIntoScroller(IScrollObjectHolder scroller, int index)
        {
            if (!isValidForScroller)
            {
                throw new InvalidOperationException("This menuobject is invalid for scroller, please check if its IOwnMenuObject else if your item is fully compatible, set isValidForScroller as true");
            }
            base.AddedIntoScroller(scroller, index);
            scroller.ItemContainer.AddChild(objectContainer);
            AddSubobjectsToScroller(menuObject, this);
        }
        public override void ParentAddedIntoScroller(ScrollObject parentInScroller)
        {
            base.ParentAddedIntoScroller(parentInScroller);
            AddSubobjectsToScroller(menuObject, parentInScroller);
        }
    }
}
