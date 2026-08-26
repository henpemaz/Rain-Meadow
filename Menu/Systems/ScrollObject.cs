using RainMeadow.UI.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Systems
{
    public abstract class ScrollObject
    {
        public IScrollObjectHolder? scroller;
        public ScrollObject? parentInScroller;
        public int indexInScroller;
        public float ContainedAlpha
        {
            get
            {
                float alpha = LocalAlpha;
                if (parentInScroller != null)
                    alpha *= parentInScroller.LocalAlpha;
                return alpha;
            }
        }
        public abstract float LocalAlpha { get; set; }
        public abstract Vector2 LocalPos { get; set; }
        public abstract Vector2 Size { get; set; }
        public virtual void UpdateIndexFromScroller(IScrollObjectHolder scroller, int newIndex)
        {
            if (scroller != this.scroller) return;
            indexInScroller = newIndex;
        }
        public virtual void AddedIntoScroller(IScrollObjectHolder scroller, int index)
        {
            this.scroller = scroller;
            indexInScroller = index;
        }
        public virtual void ParentAddedIntoScroller(ScrollObject parentInScroller)
        {
            this.scroller = parentInScroller.scroller;
            this.parentInScroller = parentInScroller;
        }
        public virtual void OnRemovedFromScroller()
        {
            this.scroller = null;
            parentInScroller = null;
            indexInScroller = 0;
            LocalAlpha = 1;
        }
        public virtual void ParentRemovedFromScroller()
        {
            scroller = null;
            parentInScroller = null;
        }
        public virtual void UpdateInObject()
        {
            if (scroller == null) return;
            if (!scroller.ScrollObjectsDirty) return;
            if (parentInScroller != null)
                return;
            Size = scroller.SizeOfObject(Size);
            var pos = LocalPos = scroller.PositionOfObject(indexInScroller, LocalPos);
            LocalAlpha = scroller.AlphaOfObject(pos);
        }
        public virtual void GrafUpdateInObject(float timeStacker)
        {

        }
    }
}
