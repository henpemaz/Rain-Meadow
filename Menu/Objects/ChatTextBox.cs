using Menu;
using UnityEngine;
using Steamworks;
using System;

namespace RainMeadow
{
    public class ChatTextBox : SimpleButton
    {
        public Action<char> KeyDown { get; set; }
        public int textLimit;
        public string value;
        public ChatTextBox(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, "", pos, size)
        {
            KeyDown = (Action<char>)Delegate.Combine(KeyDown, new Action<char>(CaptureInputs));
        }
        
        public override void Update()
        {
            base.Update();

            string input = Input.inputString;
            foreach (char c in input)
            {
                KeyDown.Invoke(c);
            }
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