using UnityEngine;

namespace RainMeadow.UI;

// you want the truth? MenuHelpers is full of extensions and RainMeadow.Utils feels wrong to pollute with dumb constants and helpers
// only really used in this namespace. DON'T ADD THINGS HERE WITHOUT ASKING PLEASE.
public static class UIUtils
{
    // RW always renders to a buffer of this size and then just crops to achieve the desired resolution/aspect ratio
    public const float DEFAULT_SCREEN_WIDTH = 1366f;
    public const float DEFAULT_SCREEN_HEIGHT = 768f;

    // commonly used dialog size
    public static Vector2 DIALOG_SIZE => new(480f, 320f);

    public static Vector2 ScreenCenter(ProcessManager manager) =>
        new(
            (DEFAULT_SCREEN_WIDTH - manager.rainWorld.screenSize.x) / 2f
                + manager.rainWorld.screenSize.x / 2f,
            (DEFAULT_SCREEN_HEIGHT - manager.rainWorld.screenSize.y) / 2f
                + manager.rainWorld.screenSize.y / 2f
        );
}
