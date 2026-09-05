using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components.Base;

public class EventfulButton(
    Menu.Menu menu,
    MenuObject owner,
    string displayText,
    Vector2 pos,
    Vector2 size,
    string description = "",
    Action<EventfulButton>? onClick = null
) : SimpleButton(menu, owner, displayText, "", pos, size), IHaveADescription
{
    public SoundID? SoundOnClick = SoundID.MENU_Button_Standard_Button_Pressed;

    public event Action<EventfulButton>? OnClick = onClick;

    public string Description { get; set; } = description;

    public override void Clicked()
    {
        if (SoundOnClick is not null)
            menu.PlaySound(SoundOnClick);
        OnClick?.Invoke(this);
    }
}
