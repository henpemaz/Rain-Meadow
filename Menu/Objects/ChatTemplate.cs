﻿using UnityEngine;
using Menu;
using Menu.Remix.MixedUI;

namespace RainMeadow
{
    public abstract class ChatTemplate : ButtonTemplate
    {
        public HSLColor labelColor;
        public MenuLabel menuLabel;
        public RoundedRect roundedRect;

        public FSprite _selection;
        public FSpriteWrap selectionWrap;

        public FSprite _cursor;
        public float _cursorWidth;
        public FSpriteWrap cursorWrap;

        public ChatTemplate(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.White);
            roundedRect = new RoundedRect(menu, owner, pos, size, true);
            this.subObjects.Add(roundedRect);

            this._selection = new FSprite("pixel", true);
            this._selection.height = 14f;
            this._selection.width = 0f;
            this._selection.color = Color.grey;
            this._selection.SetAnchor(0f, 0.5f);
            this._selection.SetPosition(0f, (float)(this.size.y * 0.5) - 1f);
            selectionWrap = new FSpriteWrap(menu, owner, _selection);
            this.subObjects.Add(selectionWrap);

            menuLabel = new MenuLabel(menu, owner, displayText, new Vector2(-roundedRect.size.x / 2 + 10f + pos.x, 0f), size, false);
            menuLabel.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(menuLabel);

            this._cursor = new FSprite("modInputCursor", true);
            this._cursor.SetPosition(menuLabel.size.x, (float)(this.size.y * 0.5));
            cursorWrap = new FSpriteWrap(menu, owner, _cursor);
            this.subObjects.Add(cursorWrap);

        }

        public override void Update()
        {
            var lowest = ChatTextBox.selectionPos != -1 ? Mathf.Min(ChatTextBox.cursorPos, ChatTextBox.selectionPos) : ChatTextBox.cursorPos;
            _cursorWidth = LabelTest.GetWidth(menuLabel.label.text.Substring(0, lowest), false);
            cursorWrap.sprite.x = _cursorWidth + (ChatTextBox.cursorPos < menuLabel.label.text.Length ? 8f : 15f) + this.pos.x;
            cursorWrap.sprite.alpha = Mathf.PingPong(Time.time * 4f, 1f);
            cursorWrap.sprite.isVisible = ChatTextBox.selectionPos == -1;
            if (ChatTextBox.selectionPos != -1)
            {
                selectionWrap.sprite.isVisible = true;
                selectionWrap.sprite.x = _cursorWidth + this.pos.x + 7f;
                selectionWrap.sprite.width = LabelTest.GetWidth(menuLabel.label.text.Substring(lowest, Mathf.Abs(ChatTextBox.selectionPos - ChatTextBox.cursorPos)), false);
            }
            else selectionWrap.sprite.isVisible = false;
                base.Update();
            this.roundedRect.fillAlpha = 1.0f;
        }
    }
}
