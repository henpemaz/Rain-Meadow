using UnityEngine;
using Menu;
using Menu.Remix.MixedUI;

namespace RainMeadow
{
    internal class OpTinyColorPicker : OpSimpleButton
    {
        public OpColorPicker colorPicker;
        private bool currentlyPicking;
        private const int mouseTimeout = 10;
        private int mouseOutCounter;

        public OpTinyColorPicker(Menu.Menu menu, Vector2 pos, string defaultHex) : base(pos, new Vector2(30, 30))
        {
            //this.colorPicker = new OpColorPicker(pos + new Vector2(-60, 24), "", defaultHex);
            this.colorPicker = FlatColorPicker.MakeFlatColorpicker(menu, pos + new Vector2(-60, 30), defaultHex);
            this.currentlyPicking = false;

            this.colorFill = colorPicker.valueColor;
            this._rect.fillAlpha = 1f;

            OnClick += Signal;
            OnReactivate += Show;
        }

        private class FlatColorPicker : OpColorPicker
        {
            private FlatColorPicker(Vector2 pos, string defaultHex = "FFFFFF") : base(new Configurable<Color>(MenuColorEffect.HexToColor(defaultHex)), pos) { }

            public override void Change()
            {
                Vector2 oldPos = this._pos;
                this._pos = Vector2.zero;
                base.Change();
                this._pos = oldPos;
                this.myContainer.SetPosition(this.ScreenPos);
            }

            public static OpColorPicker MakeFlatColorpicker(Menu.Menu menu, Vector2 pos, string defaultHex = "FFFFFF")
            {
                FContainer container = new FContainer();
                FContainer pgctr = menu.pages[0].Container;
                menu.pages[0].Container = container;
                FlatColorPicker pkr = new FlatColorPicker(pos, defaultHex);
                menu.pages[0].Container = pgctr;
                FContainer pfkcontainer = pkr.myContainer;
                pfkcontainer.AddChildAtIndex(container, 0);
                return pkr;
            }
        }

        public void Signal(UIfocusable trigger)
        {
            // base.Signal();
            if (!currentlyPicking)
            {
                //this.colorPicker.pos = (this.inScrollBox ? (this.GetPos() + scrollBox.GetPos()) : this.GetPos()) + new Vector2(-60, 24);
                this.colorPicker.pos = (this.InScrollBox ? (this.GetPos() + scrollBox.GetPos() + new Vector2(0f, scrollBox.ScrollOffset)) : this.GetPos()) + new Vector2(-60, 24);
                colorPicker.Show();
                currentlyPicking = true;
            }
            else
            {
                currentlyPicking = false;
                colorFill = colorPicker.valueColor;
                OnValueChangedEvent?.Invoke();
                colorPicker.Hide();
            }
        }

        internal delegate void OnValueChangedHandler();
        public event OnValueChangedHandler OnValueChangedEvent;

        public Color valuecolor => colorPicker.valueColor;

        //public event OnFrozenUpdateHandler OnFrozenUpdate;

        public override void Update()
        {
            // we do a little tricking
            //if (currentlyPicking && !this.MouseOver) this.held = false;
            base.Update();
            if (currentlyPicking && !this.MouseOver)
            {
                colorPicker.Update();
                //base.OnFrozenUpdate?.Invoke();
                OnValueChangedEvent?.Invoke();
                this.held = true;
            }

            if (currentlyPicking && !this.MouseOver && !colorPicker.MouseOver)
            {
                mouseOutCounter++;
            }
            else
            {
                mouseOutCounter = 0;
            }
            if (mouseOutCounter >= mouseTimeout)
            {
                this.Signal(this);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (currentlyPicking)
            {
                this.colorFill = colorPicker.valueColor;
            }
            _rect.fillAlpha = 1f;
        }

        public void Show()
        {
            colorPicker.Hide();
        }
    }
}
