using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class DialogBox(
    Menu.Menu menu,
    MenuObject owner,
    string text,
    Vector2 pos,
    Vector2 size,
    bool forceWrapping = false
) : MenuDialogBox(menu, owner, text, pos, size, forceWrapping);
