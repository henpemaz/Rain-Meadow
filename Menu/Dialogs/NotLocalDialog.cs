using System;
using Menu;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class NotLocalDialog : Dialog
{
    public event Action? OnConfirm;

    public NotLocalDialog(ProcessManager manager)
        : base(manager)
    {
        Futile.atlasManager.LoadAtlas("illustrations/notlocalwarning");

        Vector2 center = UIUtils.ScreenCenter(manager);

        DialogBox dialogBox = new(
            this,
            dialogPage,
            Translate(
                "This address is possibly not local to your current network.<LINE>If so, This is very unstable and will most likely NOT work<LINE>Are you SURE you know what you're doing?"
            ),
            center - (UIUtils.DIALOG_SIZE / 2),
            UIUtils.DIALOG_SIZE
        );

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

        SimplerButton yesButton = new(
            this,
            dialogPage,
            Translate("YES"),
            center + new Vector2(-150, -130),
            new Vector2(110, 30)
        );
        yesButton.OnClick += (btn) => OnConfirm?.Invoke();

        SimplerButton theButtonThatYouClickToDesideThatYouDoNotWantToDoThisAction = new(
            this,
            dialogPage,
            Translate("NEVER MIND"),
            center + new Vector2(40, -130),
            new Vector2(110, 30)
        );
        theButtonThatYouClickToDesideThatYouDoNotWantToDoThisAction.OnClick += (btn) =>
            manager.StopSideProcess(this);

        dialogPage.subObjects.AddRange([
            dialogBox,
            warningSurvivor,
            warningInv,
            yesButton,
            theButtonThatYouClickToDesideThatYouDoNotWantToDoThisAction,
        ]);
    }
}
