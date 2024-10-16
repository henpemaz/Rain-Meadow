using Menu;
using UnityEngine;
using Steamworks;
using System;
using Menu.Remix.MixedUI;

namespace RainMeadow
{
    public class ChatTextBox : SimpleButton, ICanBeTyped
    {
        private ButtonTypingHandler typingHandler;
        private GameObject gameObject;
        public Action<char> OnKeyDown { get; set; }
        public int textLimit;
        public string value;
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, "", pos, size)
        {
            gameObject ??= new GameObject("typingHandler");
            OnKeyDown = (Action<char>)Delegate.Combine(OnKeyDown, new Action<char>(CaptureInputs));
            if (typingHandler == null)
            {
                typingHandler = gameObject.AddComponent<ButtonTypingHandler>();
            }
            typingHandler.Assign(this);
        }
        private void CaptureInputs(char input)
        {
            if (input == '\b')
            {
                if (value.Length > 0)
                {
                    value = value.Substring(0, value.Length - 1);
                }
            }
            else
            {
                value += input.ToString();
            }
            menuLabel.text = value;
        }
    }
}