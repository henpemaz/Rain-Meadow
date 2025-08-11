using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class HorizontalSlider2 : RectangularMenuObject
    {
        HorizontalSlider slider; // because having the object be the knob that moves itself and has relative-pos stuff was a genious idea, joar
        public HorizontalSlider2(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, Slider.SliderID ID, bool subtleSlider) : base(menu, owner, pos, size)
        {
            this.slider = new HorizontalSlider(menu, this, text, Vector2.zero, size, ID, subtleSlider);
            this.subObjects.Add(slider);
        }
    }
}
