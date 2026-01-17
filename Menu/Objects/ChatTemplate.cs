using UnityEngine;
using Menu;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;

namespace RainMeadow
{
    public abstract class ChatTemplate : ButtonTemplate
    {
        public bool MultiView;
        public int maxVisibleLength;

        public HSLColor labelColor;
        public AlignedMenuLabel menuLabel;
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

            //menuLabel = new(menu, owner, displayText, new(-roundedRect.size.x / 2 + 10f + pos.x, size.y / 2), size, false);
            menuLabel = new(menu, this, displayText, new(10, 0), new(size.x - 30, size.y), false)
            {labelPosAlignment = FLabelAlignment.Left};
            menuLabel.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(menuLabel);

            this._cursor = new FSprite("modInputCursor", true);
            this._cursor.SetPosition(menuLabel.size.x, (float)(this.size.y * 0.5));
            cursorWrap = new FSpriteWrap(menu, owner, _cursor);
            this.subObjects.Add(cursorWrap);

        }

        public override void GrafUpdate(float timeStacker)
        {
            Vector2 screenPos = ScreenPos;
            for (int i = 0; i < 9; i++)
            {
                roundedRect.sprites[i].color = Color.black;
            }
            var lowest = ChatTextBox.selectionPos != -1 ? Mathf.Min(ChatTextBox.cursorPos, ChatTextBox.selectionPos) : ChatTextBox.cursorPos;
            var highest = Mathf.Abs(ChatTextBox.selectionPos - ChatTextBox.cursorPos);
            _cursorWidth = LabelTest.GetWidth(ChatTextBox.lastSentMessage.Substring(0, lowest > maxVisibleLength ? menuLabel.label.text.Length : lowest), false);
            cursorWrap.sprite.x = _cursorWidth + (ChatTextBox.cursorPos < menuLabel.label.text.Length ? 11f : 15f) + screenPos.x;
            cursorWrap.sprite.y = screenPos.y + size.y / 2;
            cursorWrap.sprite.alpha = IsFoucsed() ? Mathf.PingPong(Time.time * 4f, 1f) : 0;
            cursorWrap.sprite.isVisible = ChatTextBox.selectionPos == -1;

            int firstLetterViewed = ChatTextBox.cursorPos > maxVisibleLength ? ChatTextBox.cursorPos - maxVisibleLength : 0,
                lastLetterViewed = Mathf.Max(0, ChatTextBox.cursorPos > maxVisibleLength ? maxVisibleLength : Mathf.Min(maxVisibleLength, ChatTextBox.lastSentMessage.Length));

            menuLabel.text = ChatTextBox.lastSentMessage.Substring(firstLetterViewed, lastLetterViewed);

            int lowestCursorPos = ChatTextBox.selectionPos != -1 ? Mathf.Min(ChatTextBox.cursorPos, ChatTextBox.selectionPos) : ChatTextBox.cursorPos;

            if (ChatTextBox.selectionPos != -1)
            {
                int start = lowestCursorPos > firstLetterViewed ? lowestCursorPos - firstLetterViewed : firstLetterViewed > 0 ? 0 : lowestCursorPos;
                float width = LabelTest.GetWidth(menuLabel.text.Substring(start, Mathf.Min(Mathf.Abs(ChatTextBox.selectionPos - ChatTextBox.cursorPos), maxVisibleLength - start)), false);
                selectionWrap.sprite.isVisible = true;
                selectionWrap.sprite.x = _cursorWidth + screenPos.x + 11f;
                selectionWrap.sprite.y = screenPos.y + size.y / 2;
                selectionWrap.sprite.width = width;
            }
            else 
                selectionWrap.sprite.isVisible = false;
            selectionWrap.sprite.alpha = IsFoucsed() ? 1f : 0f;
                base.GrafUpdate(timeStacker);
            this.roundedRect.fillAlpha = 1.0f;
        }

        public virtual bool IsFoucsed()
        {
            return true;
        }
    }
}
