
namespace RainMeadow.UI.Components.Configurables;

// gets called before the visibility and position update
public interface IUpdateWithSlugcatSetting
{
    public void OnUpdateVisibility();
    public void OnUpdatePosition();
}