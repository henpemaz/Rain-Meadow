using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class NotifyDialog : Dialog
{
    public ProcessManager.ProcessID? initialProcessID;

    public event Action? OnConfirm;

    public bool onlyShowInInitialProcess;

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        bool forceWrapping = false,
        bool onlyShowInInitialProcess = false
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

        this.onlyShowInInitialProcess = onlyShowInInitialProcess;
        if (onlyShowInInitialProcess)
            initialProcessID = manager.currentMainLoop.ID;
    }

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        ProcessManager.ProcessID processOnConfirm,
        bool forceWrapping = false,
        bool onlyShowInInitialProcess = false
    )
        : this(manager, message, size, forceWrapping, onlyShowInInitialProcess)
    {
        OnConfirm += () => manager.RequestMainProcessSwitch(processOnConfirm);
    }

    public override void Update()
    {
        base.Update();
        if (onlyShowInInitialProcess && manager.currentMainLoop.ID != initialProcessID)
            manager.StopSideProcess(this);
    }

    public override void Singal(MenuObject sender, string message)
    {
        PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        manager.StopSideProcess(this);
        OnConfirm?.Invoke();
    }
}
