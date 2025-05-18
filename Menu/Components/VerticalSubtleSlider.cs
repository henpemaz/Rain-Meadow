using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components;

/// <summary>
/// when the slider's position is shifted on the x-axis, the lines will now not spasm. Moving the slider's position on the y-axis still has this issue, but why would you ever move the position of a slider on the y-axis?
/// </summary>
public class VerticalSliderPlus(Menu.Menu menu, MenuObject owner, string? text, Vector2 pos, Vector2 size, Slider.SliderID ID, bool subtleSlider)
    : VerticalSlider(menu, owner, text, pos, size, ID, subtleSlider)
{
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        lineSprites[0].x = DrawX(timeStacker) + 15f;
        lineSprites[1].x = DrawX(timeStacker) + 15f;
        lineSprites[2].x = DrawX(timeStacker) + 15f;
        lineSprites[3].x = DrawX(timeStacker) + 15f;
    }
}