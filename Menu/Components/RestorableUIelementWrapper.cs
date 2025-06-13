using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;

namespace RainMeadow.UI.Components;

public class RestorableUIelementWrapper(MenuTabWrapper tabWrapper, UIelement element) : UIelementWrapper(tabWrapper, element), IRestorableMenuObject
{
    public override void RemoveSprites()
    {
        thisElement.Hide();
    }

    public void RestoreSelectables()
    {
        if (IsFocusable) page.selectables.Add(this);
    }

    public void RestoreSprites()
    {
        thisElement.Show();
    }
}