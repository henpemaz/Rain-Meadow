using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Interfaces;

namespace RainMeadow.UI.Components.Patched;

public class PatchedUIelementWrapper(MenuTabWrapper tabWrapper, UIelement element) : UIelementWrapper(tabWrapper, element), IPLEASEUPDATEME
{
    public bool IsHidden { get; set; } //only needed in some occasions where uielementwrapper/enutabwrapper is updated
    public override void Update()
    {
        base.Update();
        pos = thisElement.pos;
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        if (IsFocusable) glow.pos = thisElement.ScreenPos;
    }
}