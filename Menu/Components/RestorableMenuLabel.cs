using Menu;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components;

public class RestorableMenuLabel(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, bool bigText, FTextParams? textParams = null)
    : MenuLabel(menu, owner, text, pos, size, bigText, textParams), IRestorableMenuObject
{
    public void RestoreSelectables() { }

    public void RestoreSprites()
    {
        Container.AddChild(label);
    }
}