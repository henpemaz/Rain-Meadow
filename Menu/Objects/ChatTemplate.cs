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
        public RoundedRect selectRect;
        public float fadeAlpha;

        public FSprite _selection;
        public FSpriteWrap selectionWrap;

        public FSprite _cursor;
        public float _cursorWidth;
        public FSpriteWrap cursorWrap;

        public static string lastSentMessage = "";
        public bool focused = true;
        public ChatTemplate(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, pos, size)
        {
            labelColor = Menu.Menu.MenuColor(Menu.Menu.MenuColors.White);
            roundedRect = new RoundedRect(menu, owner, pos, size, true);
            this.subObjects.Add(roundedRect);
            selectRect = new RoundedRect(menu, owner, pos, size, false);
            this.subObjects.Add(roundedRect);

            this._selection = new FSprite("pixel", true);
            this._selection.height = 14f;
            this._selection.width = 0f;
            this._selection.color = Color.grey;
            this._selection.SetAnchor(0f, 0.5f);
            this._selection.SetPosition(0f, (float)(this.size.y * 0.5) - 1f);
            selectionWrap = new FSpriteWrap(menu, owner, _selection);
            this.subObjects.Add(selectionWrap);

            menuLabel = new MenuLabel(menu, owner, displayText, new Vector2(-roundedRect.size.x / 2 + 10f + pos.x, pos.y), size, false);
            menuLabel.label.alignment = FLabelAlignment.Left;
            this.subObjects.Add(menuLabel);

            this._cursor = new FSprite("modInputCursor", true);
            this._cursor.SetPosition(menuLabel.size.x, (float)(this.size.y * 0.5));
            cursorWrap = new FSpriteWrap(menu, owner, _cursor);
            this.subObjects.Add(cursorWrap);
        }

        public void SetSize(Vector2 newSize)
        {
            size = newSize;
            roundedRect.size = newSize;
            selectRect.size = newSize;
            menuLabel.size = newSize;
        }

        public override void Update()
        {
            if (!buttonBehav.clicked && Input.GetMouseButtonDown(0)) focused = false;

            var lowest = ChatTextBox.selectionPos != -1 ? Mathf.Min(ChatTextBox.cursorPos, ChatTextBox.selectionPos) : ChatTextBox.cursorPos;
            _cursorWidth = LabelTest.GetWidth(menuLabel.label.text.Substring(0, lowest), false); // FYI this line WILL crash the game if there are multiple text box instances (something like ArguementOutOfRangeException i think)
            cursorWrap.sprite.x = _cursorWidth + (ChatTextBox.cursorPos < menuLabel.label.text.Length ? 8f : 15f) + this.pos.x;
            cursorWrap.sprite.y = pos.y + size.y / 2;
            if (focused) cursorWrap.sprite.alpha = Mathf.PingPong(Time.time * 4f, 1f);
            else cursorWrap.sprite.alpha = 0f;
            cursorWrap.sprite.isVisible = ChatTextBox.selectionPos == -1;
            if (ChatTextBox.selectionPos != -1)
            {
                selectionWrap.sprite.isVisible = true;
                selectionWrap.sprite.x = _cursorWidth + this.pos.x + 7f;
                selectionWrap.sprite.width = LabelTest.GetWidth(menuLabel.label.text.Substring(lowest, Mathf.Abs(ChatTextBox.selectionPos - ChatTextBox.cursorPos)), false);
            }
            else selectionWrap.sprite.isVisible = false;

            base.Update();
            buttonBehav.Update();
            roundedRect.fillAlpha = 1.0f;
            roundedRect.addSize = new Vector2(5f, 3f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14f)) * (buttonBehav.clicked ? 0f : 1f);
            selectRect.addSize = new Vector2(1f, -1f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.14f)) * (buttonBehav.clicked ? 0f : 1f);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            menuLabel.label.color = this.InterpColor(timeStacker, this.labelColor);

            for (int i = 0; i < 9; i++)
            {
                roundedRect.sprites[i].color = Color.black;
            }
            float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(this.buttonBehav.lastSin, this.buttonBehav.sin, timeStacker) / 30f * 3.1415927f * 2f);
            num *= buttonBehav.sizeBump;
            for (int j = 0; j < 8; j++)
            {
                selectRect.sprites[j].color = this.MyColor(timeStacker);
                selectRect.sprites[j].alpha = num * fadeAlpha;
            }
        }

        public override void Clicked()
        {
            base.Clicked();
            focused = true;
        }
    }
}