using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class NotifyDialog : Dialog
{
    public event Action? OnConfirm;

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

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        ProcessManager.ProcessID processOnConfirm,
        bool forceWrapping = false
    )
        : this(manager, message, size, forceWrapping)
    {
        OnConfirm += () => manager.RequestMainProcessSwitch(processOnConfirm);
    }

    public override void Singal(MenuObject sender, string message)
    {
        PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        manager.StopSideProcess(this);
        OnConfirm?.Invoke();
    }
}
