﻿using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using UnityEngine;

namespace RainMeadow
{
    public class OpTinyColorPicker : OpSimpleButton
    {
        public OpColorPicker colorPicker;
        public bool currentlyPicking;
        private const int focusTimeout = 10;
        private int loseFocusCounter;

        public OpTinyColorPicker(Menu.Menu menu, MenuTabWrapper tabWrapper, Vector2 pos, Color defaultColor) : base(pos, new Vector2(30, 30))
        {
            this.colorPicker = new OpColorPicker(new Configurable<Color>(defaultColor), pos);
            PatchedUIelementWrapper wrapper = new PatchedUIelementWrapper(tabWrapper, colorPicker);
            colorPicker.Hide();

            this.currentlyPicking = false;

            this.colorFill = colorPicker.valueColor;
            this._rect.fillAlpha = 1f;

            OnClick += Signal;
            OnReactivate += Reactivated;
        }
        public OpTinyColorPicker(Menu.Menu menu, Vector2 pos, Color defaultColor, MenuTabWrapper tabWrapper) : base(pos, new Vector2(30, 30))
        {
            this.colorPicker = new OpColorPicker(new Configurable<Color>(defaultColor), pos);
            PatchedUIelementWrapper wrapper = new(tabWrapper, colorPicker), myWrapper = new(tabWrapper, this);
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
                colorPicker.myContainer.MoveToFront();
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

                if (Menu.selectedObject == this.colorPicker.wrapper)
                    Menu.selectedObject = this.wrapper;

                colorPicker.Hide();
            }
        }

        public delegate void OnValueChangedHandler();
        public event OnValueChangedHandler OnValueChangedEvent;

        public Color valuecolor
        {
            get
            {
                // return colorPicker.valueColor; you'd think so but this thing is dogshit
                return RWCustom.Custom.hexToColor(colorPicker.value);
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
                if (colorPicker.value != colorPicker.lastValue) OnValueChangedEvent?.Invoke();

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
