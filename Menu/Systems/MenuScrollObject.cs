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
    public class MenuScrollObject
    {
        public static ConditionalWeakTable<MenuObject, MenuScrollObject> menuScrollObjects = new();
        public readonly MenuObject menuObject;
        public IScrollObjectHolder? myScroller;
        public int indexInScroller;
        public bool isValidForScroller;
        public FContainer objectContainer; //default is myContainer, you can change this
        public float desiredAlpha = 1;
        public float ContainedAlpha
        {
            get
            {
                float alpha = LocalAlpha * (myScroller?.WithinBounds(ScreenPos, Size) == false? 0 : 1);
                if (ParentInScroller is MenuScrollObject scrollObj)
                    alpha *= scrollObj.LocalAlpha;
                return alpha;
            }
        }
        public MenuScrollObject? ParentInScroller
        {
            get
            {
                if (myScroller != null) return null;
                var menuObj = menuObject.owner;
                while (menuObj != null)
                {
                    if (menuObj.GetScrollObject().myScroller != null) break;
                    menuObj = menuObj.owner;
                }
                return menuObj?.GetScrollObject();
            }
        }
        public Vector2 ScreenPos => menuObject is PositionedMenuObject posObj ? posObj.ScreenPos : default;
        public IScrollObjectHolder? ScrollerInAncestory => myScroller ?? ParentInScroller?.myScroller;
        public virtual float LocalAlpha { get => desiredAlpha; set => desiredAlpha = value; }
        public virtual Vector2 LocalPos 
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
        public virtual Vector2 Size
        {
            get
            {
                if (menuObject is  RectangularMenuObject rectMenuObj)
                    return rectMenuObj.size;
                return default;
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
            if (menuObject is ButtonScroller.IPartOfButtonScroller scrollerObj)
                menuScrollObj = new ButtonScrollerPartObject(scrollerObj);
            else if (menuObject is IOwnMenuScrollObject)
                menuScrollObj = new MenuScrollObject(menuObject, true);
            return menuScrollObj != null;
        }
        public static MenuScrollObject GetScrollObjectFromMenuObject(MenuObject menuObject)
        {
            if (TryGetScrollObjectFromMenuObject(menuObject, out MenuScrollObject? scrollObj))
                return scrollObj!;
            return new MenuScrollObject(menuObject, false);
        }
        public void UpdateIndexFromScroller(IScrollObjectHolder scroller, int indexInScroller)
        {
            if (scroller != this.myScroller) return;
            this.indexInScroller = indexInScroller;
        }
        public virtual void UpdateInObject()
        {
            if (myScroller == null) return;
            if (!myScroller.ScrollObjectsDirty) return;
            Size = myScroller.SizeOfObject(Size);
            var pos = LocalPos = myScroller.PositionOfObject(indexInScroller, LocalPos);
            LocalAlpha = myScroller.AlphaOfObject(pos, Size);
        }
        public virtual void GrafUpdateInObject(float timeStacker)
        {
            objectContainer.alpha = LocalAlpha;
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
            else objectContainer = menuObject.Container;
        }
        public virtual void AddedIntoScroller(IScrollObjectHolder scroller, int index)
        {
            if (!isValidForScroller)
            {
                throw new InvalidOperationException("This menuobject is invalid for scroller, please check if its IOwnMenuObject else if your item is fully compatible, set isValidForScroller as true");
            }
            myScroller = scroller;
            indexInScroller = index;
            scroller.ItemContainer.AddChild(objectContainer);
        }
        public virtual void RemovedFromScroller() //assuming this gets destroyed immediately
        {
            if (myScroller != null)
            {
                objectContainer.container.AddChild(menuObject.myContainer);
                objectContainer.RemoveFromContainer();
            }
            myScroller = null;
            indexInScroller = 0;
            LocalAlpha = 1;
        }
    }
}
