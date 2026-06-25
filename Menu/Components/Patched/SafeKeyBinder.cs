
using UnityEngine;
public class SafeKeyBinder : Menu.Remix.MixedUI.OpKeyBinder
{
    public SafeKeyBinder(Configurable<KeyCode> config, Vector2 pos, Vector2 size, bool collisionCheck)
        : base(config, pos, size, collisionCheck)
    {
    }

    public override void Unload()
    {
        try
        {
            base.Unload();
        }
        catch (System.NullReferenceException)
        {
            // Swallow the exception caused by Remix looking for a non-existent OptionInterface silly duck
        }
    }
}