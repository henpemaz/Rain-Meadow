using System.Collections.Generic;
using System.Linq;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class TextScroller : ButtonScroller
{
    public TextScroller(
        Menu.Menu menu,
        MenuObject owner,
        Vector2 pos,
        Vector2 size,
        bool sliderOnRight = false,
        Vector2 sliderPosOffset = default,
        float sliderSizeYOffset = 0,
        bool sliderDefaultIsDown = false
    )
        : base(menu, owner, pos, size, sliderOnRight, sliderPosOffset, sliderSizeYOffset)
    {
        this.sliderDefaultIsDown = sliderDefaultIsDown;
        buttonHeight = 15;
        buttonSpacing = 3;
        startEndWithSpacing = false;
    }

    // treat different elements as separate lines
    public void AddText(IEnumerable<string> text, bool header = false) =>
        AddText(string.Join("\n", text), header);

    public void AddText(string text, bool header = false)
    {
        string[] textLines =
        [
            .. text.Split('\n')
                .SelectMany(t => MenuHelpers.SmartSplitIntoStrings(t, size.x, false)),
        ];
        AlignedMenuLabel[] labels = new AlignedMenuLabel[textLines.Length];
        for (int i = 0; i < textLines.Length; i++)
        {
            AlignedMenuLabel label = new(
                menu,
                this,
                textLines[i],
                GetIdealPosWithScrollForButton(i + buttons.Count),
                new Vector2(size.x, buttonHeight),
                false
            )
            {
                labelPosAlignment = header ? FLabelAlignment.Center : FLabelAlignment.Left,
                verticalLabelPosAlignment = sliderDefaultIsDown
                    ? OpLabel.LabelVAlignment.Bottom
                    : OpLabel.LabelVAlignment.Top,
            };
            label.label.alignment = header ? FLabelAlignment.Center : FLabelAlignment.Left;
            label.label.color = header ? MenuColorEffect.rgbWhite : MenuColorEffect.rgbMediumGrey;

            labels[i] = label;
        }
        AddScrollObjects(labels);
    }

    public void AddBlankLine()
    {
        AddScrollObjects(
            new AlignedMenuLabel(
                menu,
                this,
                "",
                GetIdealPosWithScrollForButton(buttons.Count),
                new Vector2(0, buttonHeight),
                false
            )
        );
    }
}
