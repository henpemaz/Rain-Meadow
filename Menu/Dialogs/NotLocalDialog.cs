using System;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class NotLocalDialog : ConfirmCancelDialog
{
    public NotLocalDialog(ProcessManager manager, Action? onConfirm = null)
        : base(
            manager,
            "This address is possibly not local to your current network.<LINE>If so, This is very unstable and will most likely NOT work<LINE>Are you SURE you know what you're doing?",
            UIUtils.DIALOG_SIZE,
            onConfirm,
            confirmButtonText: "YES",
            cancelButtonText: "NEVER MIND"
        )
    {
        Futile.atlasManager.LoadAtlas("illustrations/notlocalwarning");

        Vector2 center = UIUtils.ScreenCenter(manager);

        PositionedSprite warningSurvivor = new(
            this,
            dialogPage,
            center + new Vector2(-120, -70),
            new FSprite("warning_survivor")
        );
        PositionedSprite warningInv = new(
            this,
            dialogPage,
            center + new Vector2(70, -70),
            new FSprite("warning_inv")
        );

        dialogPage.subObjects.AddRange([warningSurvivor, warningInv]);
    }
}
