using System;
using Menu;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class ConfirmCancelDialog : Dialog
{
    public DialogBox dialogBox;

    public event Action? OnConfirm;
    public event Action? OnCancel;

    public ConfirmCancelDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        Action? onConfirm = null,
        Action? onCancel = null,
        string confirmButtonText = "OK",
        string cancelButtonText = "CANCEL"
    )
        : base(manager)
    {
        Vector2 pos = UIUtils.ScreenCenter(manager) - (size / 2);

        dialogBox = new DialogBox(this, dialogPage, Translate(message), pos, size);

        EventfulButton confirmButton = new(
            this,
            dialogPage,
            Translate(confirmButtonText),
            new Vector2(pos.x + size.x / 2 - 150, pos.y + 30),
            new Vector2(110, 30)
        );
        confirmButton.OnClick += (btn) =>
        {
            manager.StopSideProcess(this);
            OnConfirm?.Invoke();
        };

        EventfulButton cancelButton = new(
            this,
            dialogPage,
            Translate(cancelButtonText),
            new Vector2(pos.x + size.x / 2 + 40, pos.y + 30),
            new Vector2(110, 30)
        );
        cancelButton.OnClick += (btn) =>
        {
            manager.StopSideProcess(this);
            OnCancel?.Invoke();
        };

        dialogPage.subObjects.AddRange([dialogBox, confirmButton, cancelButton]);

        OnConfirm += onConfirm;
        OnCancel += onCancel;
    }
}
