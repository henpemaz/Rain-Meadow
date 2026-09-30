using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components.Base;

public class EventfulSymbolButton(
    Menu.Menu menu,
    MenuObject owner,
    string symbolName,
    Vector2 pos,
    string description = "",
    Action<EventfulSymbolButton>? onClick = null
) : SymbolButton(menu, owner, symbolName, "", pos), IHaveADescription
{
    public SoundID? SoundOnClick = SoundID.MENU_Button_Standard_Button_Pressed;

    public event Action<EventfulSymbolButton>? OnClick = onClick;

    public string Description { get; set; } = description;

    public override void Clicked()
    {
        if (SoundOnClick is not null)
            menu.PlaySound(SoundOnClick);
        OnClick?.Invoke(this);
    }
}
