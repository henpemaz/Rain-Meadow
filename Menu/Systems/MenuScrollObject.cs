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
        public MenuScrollObject? cachedParentInScroller;
        public IScrollObjectHolder? scroller; //you could have a parent in scroller but have access to this
        public int indexInScroller = -1;
        public bool isValidForScroller;
        public FContainer objectContainer; //default is myContainer, you can change this
        public float desiredAlpha = 1;
        public float ContainedAlpha
        {
            get
            {
                float alpha = LocalAlpha;
                if (cachedParentInScroller != null)
                    alpha *= cachedParentInScroller.LocalAlpha;
                return alpha;
            }
        }
        public bool IsDirectlyInScroller => scroller != null && cachedParentInScroller == null;
        public MenuScrollObject? ParentInScroller
        {
            get
            {
                var parentMenuObj = menuObject.owner?.GetScrollObject();
                if (parentMenuObj == null) return null;
                if (parentMenuObj.scroller != null && parentMenuObj.IsDirectlyInScroller)
                    return parentMenuObj;
                return parentMenuObj.ParentInScroller;
            }
        }
        public bool IsMouseWithinBounds => scroller == null || scroller.MouseOverItemBounds;
        public bool IsWithinBounds => scroller == null || scroller.WithinBounds(ScreenPos, Size);
        public Vector2 ScreenPos => menuObject is PositionedMenuObject posObj ? posObj.ScreenPos : default;
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
        public void AddRemoveSubobjectsToScroller(MenuObject parent, MenuScrollObject owner, bool add)
        {
            for (int i = 0; i < parent.subObjects.Count; i++)
            {
                var sub = parent.subObjects[i];
                if (add)
                    sub.GetScrollObject().ParentAddedIntoScroller(owner);
                else
                    sub.GetScrollObject().ParentRemovedFromScroller(owner);
            }
        }
        public void UpdateIndexFromScroller(IScrollObjectHolder scroller, int indexInScroller)
        {
            if (scroller != this.scroller) return;
            this.indexInScroller = indexInScroller;
        }
        public virtual void UpdateInObject()
        {
            if ((cachedParentInScroller != null && (!cachedParentInScroller.IsDirectlyInScroller || cachedParentInScroller.scroller != scroller)))
            {
                cachedParentInScroller = ParentInScroller;
                scroller = cachedParentInScroller?.scroller;
            }
            if (scroller == null || cachedParentInScroller != null) return;
            if (!scroller.ScrollObjectsDirty) return;
            Size = scroller.SizeOfObject(Size);
            var pos = LocalPos = scroller.PositionOfObject(indexInScroller, LocalPos);
            LocalAlpha = scroller.AlphaOfObject(pos, Size);
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
                throw new InvalidOperationException("This menuobject is invalid for scroller, please check if its IOwnMenuObject else if your item is fully compatible, set isValidForScroller as true.");
            }
            this.scroller = scroller;
            cachedParentInScroller = null;
            indexInScroller = index;
            this.scroller.ItemContainer.AddChild(objectContainer);
            AddRemoveSubobjectsToScroller(menuObject, this, true);
        }
        public virtual void RemovedFromScroller() //assuming this gets destroyed immediately
        {
            if (scroller != null)
            {
                objectContainer.container.AddChild(menuObject.myContainer);
                objectContainer.RemoveFromContainer();
            }
            scroller = null;
            indexInScroller = -1;
            LocalAlpha = 1;
            AddRemoveSubobjectsToScroller(menuObject, this, false);
        }
        public virtual void ParentAddedIntoScroller(MenuScrollObject parentInScroller)
        {
            scroller = parentInScroller.scroller;
            cachedParentInScroller = parentInScroller;
            AddRemoveSubobjectsToScroller(menuObject, parentInScroller, true);
        }
        public virtual void ParentRemovedFromScroller(MenuScrollObject parentInScroller)
        {
            scroller = null;
            cachedParentInScroller = null;
            AddRemoveSubobjectsToScroller(menuObject, parentInScroller, false);
        }
    }
}
