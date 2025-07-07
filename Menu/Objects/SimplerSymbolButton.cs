using Menu;
using System;
using UnityEngine;

namespace RainMeadow;

public class SimplerSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string singalText, Vector2 pos, string description = "")
    : SymbolButton(menu, owner, symbolName, singalText, pos), IHaveADescription
{
    public string description = description;
    public event Action<SymbolButton>? OnClick;

    public void ResetSubscriptions() => OnClick = delegate { };

    public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }

    public string Description => description;
}
