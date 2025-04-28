using UnityEngine;
using Menu;
using Menu.Remix.MixedUI;

namespace RainMeadow
{
    public abstract class ChatTemplate : ButtonTemplate
    {
        public HSLColor labelColor;
        public MenuLabel menuLabel;
        public RoundedRect roundedRect;

        public FSprite _cursor;
        public float _cursorWidth;
        public FSpriteWrap cursorWrap;

        public ChatTemplate(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.White);
            roundedRect = new RoundedRect(menu, owner, pos, size, true);
            this.subObjects.Add(roundedRect);
            

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
            _cursorWidth = LabelTest.GetWidth(menuLabel.label.text.Substring(0, ChatTextBox.cursorPos), false);
            cursorWrap.sprite.x = _cursorWidth + (ChatTextBox.cursorPos < menuLabel.label.text.Length ? 8f : 15f) + this.pos.x;
            cursorWrap.sprite.alpha = Mathf.PingPong(Time.time * 4f, 1f);
            base.Update();
            this.roundedRect.fillAlpha = 1.0f;
        }
    }
}
