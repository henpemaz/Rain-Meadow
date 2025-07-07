using Menu.Remix;
using Menu.Remix.MixedUI;

namespace RainMeadow.UI.Components.Patched;

public class PatchedUIelementWrapper(MenuTabWrapper tabWrapper, UIelement element) : UIelementWrapper(tabWrapper, element)
{
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (IsFocusable) glow.pos = thisElement.ScreenPos;
    }
}