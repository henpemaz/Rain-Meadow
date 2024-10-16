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
            this.menu = menu;
            gameObject ??= new GameObject();
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
                    menu.PlaySound(SoundID.MENY_Already_Selected_MultipleChoice_Clicked);
                    value = value.Substring(0, value.Length - 1);
                }
            }
            else
            {
                menu.PlaySound(SoundID.MENU_Checkbox_Check);
                value += input.ToString();
            }

            if (value.Length > 0)
            {
                if (input == '\n' || input == '\r')
                {
                    typingHandler.Unassign(this);
                    RainMeadow.Debug(value);
                }
            }
            menuLabel.text = value;
        }
    }
}