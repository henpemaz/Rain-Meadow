using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class UpdateDialog : Dialog
{
    public UpdateDialog(ProcessManager manager)
        : base(manager)
    {
        Vector2 center = UIUtils.ScreenCenter(manager);

        DialogBox dialogBox = new(
            this,
            dialogPage,
            Translate(
                    "Rain Meadow version <NEW_VERSION> is now available.<LINE><LINE>Update to join the newest lobbies and get the latest features & fixes."
                )
                .Replace("<NEW_VERSION>", RainMeadow.NewVersionAvailable),
            center - (UIUtils.DIALOG_SIZE / 2),
            UIUtils.DIALOG_SIZE
        );

        SimplerButton helpButton = new(
            this,
            dialogPage,
            Translate("HOW TO UPDATE"),
            center + new Vector2(40, -130),
            new Vector2(110, 30)
        );
        helpButton.OnClick += (btn) =>
        {
            manager.StopSideProcess(this);
            manager.ShowDialog(
                new NotifyDialog(
                    manager,
                    Translate(
                        "For Steam: Restart your game. If Rain Meadow doesn't update automatically, resubscribe to force an update.<LINE><LINE>For Other Platforms: Visit our GitHub releases page to download the latest release.<LINE><LINE>Updating won't affect your save data."
                    ),
                    UIUtils.DIALOG_SIZE
                )
            );
        };

        SimplerButton okButton = new(
            this,
            dialogPage,
            Translate("OK"),
            center + new Vector2(-150, -130),
            new Vector2(110, 30)
        );
        okButton.OnClick += (btn) => manager.StopSideProcess(this);

        dialogPage.subObjects.AddRange([dialogBox, helpButton, okButton]);
    }
}
