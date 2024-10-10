using System;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow
{
    public class ChatTextBox : OpTextBox, ICanBeTyped
    {
        public ChatTextBox(ConfigurableBase config, Vector2 pos, float sizeX) : base(config, pos, sizeX)
        {
            _size = new Vector2(Mathf.Max(IsUpdown ? 40f : 30f, sizeX), this.IsUpdown ? 30f : 24f);
        }

        public Action<char> KeyHitDown { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }
}