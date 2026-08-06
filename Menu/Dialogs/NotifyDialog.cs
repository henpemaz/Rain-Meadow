using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

// DialogNotify does exist... but it requires passing in an action to show continue button
public class NotifyDialog : Dialog
{
    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        bool forceWrapping = false
    )
        : base(manager)
    {
        dialogPage.subObjects.Add(
            new DialogBoxNotify(
                this,
                dialogPage,
                Translate(message),
                "",
                UIUtils.ScreenCenter(manager) - (size / 2),
                size,
                forceWrapping
            )
        );
    }

    // since there's only one object and one signal
    public override void Singal(MenuObject sender, string message) => manager.StopSideProcess(this);
}
