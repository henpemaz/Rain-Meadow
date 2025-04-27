using System;
using Menu;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class SimplerCheckbox : CheckBox, CheckBox.IOwnCheckBox, IRestorableMenuObj
{
    private bool boxChecked;
    public event Action<bool>? OnClick;

    public SimplerCheckbox(Menu.Menu menu, MenuObject owner, Vector2 pos, float textWidth, string displayText, bool textOnRight = false)
        : base(menu, owner, null, pos, textWidth, displayText, null, textOnRight)
    {
        reportTo = this;
    }

    public bool GetChecked(CheckBox box)
    {
        if (box == this) return boxChecked;
        throw new Exception("Another CheckBox is using a SimplerCheckbox as a CheckBox handler");
    }

    public void SetChecked(CheckBox box, bool c)
    {
        if (box != this) throw new Exception("Another CheckBox is using a SimplerCheckbox as a CheckBox handler");
        boxChecked = c;
        OnClick?.Invoke(c);
    }

    public void RestoreSprites()
    {
        for (int i = 0; i < roundedRect.sprites.Length; i++) Container.AddChild(roundedRect.sprites[i]);
        Container.AddChild(label.label);
        Container.AddChild(symbolSprite);
    }

    public void RestoreSelectables()
    {
        page.selectables.Add(this);
    }
}
