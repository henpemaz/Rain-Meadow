using RainMeadow.UI.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow.UI.Interfaces
{
    public interface IScrollObjectHolder
    {
        public bool ScrollObjectsDirty { get; }
        public FContainer ItemContainer { get; }
        public bool MouseOver { get; }
        public float AlphaOfObject(Vector2 position, Vector2 size);
        public Vector2 PositionOfObject(int index, Vector2 origPosition);
        public Vector2 SizeOfObject(Vector2 origSize);
    }
}
