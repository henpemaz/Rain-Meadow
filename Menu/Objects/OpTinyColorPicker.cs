using UnityEngine;
using Menu;
using Menu.Remix.MixedUI;
using System;
using Menu.Remix;

namespace RainMeadow
{
    internal class OpTinyColorPicker : OpSimpleButton
    {
        public OpColorPicker colorPicker;
        private bool currentlyPicking;
        private const int focusTimeout = 10;
        private int loseFocusCounter;

        public OpTinyColorPicker(Menu.Menu menu, Vector2 pos, string defaultHex) : base(pos, new Vector2(30, 30))
        {
            this.colorPicker = new OpColorPicker(new Configurable<Color>(MenuColorEffect.HexToColor(defaultHex)), pos);
            UIelementWrapper wrapper = new UIelementWrapper((menu as SmartMenu).tabWrapper, colorPicker);
            colorPicker.Hide();

            this.currentlyPicking = false;

            this.colorFill = colorPicker.valueColor;
            this._rect.fillAlpha = 1f;

            OnClick += Signal;
            OnReactivate += Reactivated;
        }

        public void Signal(UIfocusable trigger)
        {
            if (!currentlyPicking)
            {
                this.colorPicker.pos = (this.InScrollBox ? (this.GetPos() + scrollBox.GetPos() + new Vector2(0f, scrollBox.ScrollOffset)) : this.GetPos()) + new Vector2(-60, 30);
                colorPicker.Show();
                currentlyPicking = true;
                colorPicker.NonMouseSetHeld(true);
                colorPicker.held = true;
                Menu.selectedObject = this.colorPicker.wrapper;
            }
            else
            {
                currentlyPicking = false;
                colorFill = colorPicker.valueColor;
                OnValueChangedEvent?.Invoke();
                
                if(Menu.selectedObject == this.colorPicker.wrapper)
                    Menu.selectedObject = this.wrapper;

                colorPicker.Hide();
            }
        }

        internal delegate void OnValueChangedHandler();
        public event OnValueChangedHandler OnValueChangedEvent;

        public Color valuecolor
        {
            get
            {
                return colorPicker.valueColor;
            }
            set
            {
                colorPicker.valueColor = value;
                colorFill = value;
            }
        }

        public override void Update()
        {
            var mouseMode = MenuMouseMode;

            base.Update();
            if (currentlyPicking)
            {
                OnValueChangedEvent?.Invoke();

                if (!mouseMode && !colorPicker.held)
                {
                    RainMeadow.Debug("lost focus, not held");
                    this.Signal(this);
                }
            }

            if (currentlyPicking && (mouseMode ? (!this.MouseOver && !colorPicker.MouseOver) : (!Focused && !colorPicker.Focused)))
            {
                loseFocusCounter++;
            }
            else
            {
                loseFocusCounter = 0;
            }
            if (loseFocusCounter >= focusTimeout)
            {
                RainMeadow.Debug("lost focus!");
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

        public void Reactivated()
        {
            colorPicker.Hide();
        }
    }
}
