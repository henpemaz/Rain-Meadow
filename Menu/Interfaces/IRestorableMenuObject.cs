namespace RainMeadow.UI.Interfaces;

public interface IRestorableMenuObject
{
    public abstract void RestoreSprites();
    public abstract void RestoreSelectables();
}
public interface ICanHideMenuObject //prehaps will replace IRestorableMenuObject
{
    public abstract void HiddenUpdate();
    public abstract void HiddenGrafUpdate(float timeStacker);
}
