using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class NotifyDialog : Dialog
{
    public ProcessManager.ProcessID initialProcessID;

    public event Action? OnContinue;

    public bool onlyShowInInitialProcess;

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        Action? onContinue = null,
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

        OnContinue += onContinue;

        this.onlyShowInInitialProcess = onlyShowInInitialProcess;
        initialProcessID = manager.currentMainLoop.ID;
    }

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        ProcessManager.ProcessID processOnContinue,
        bool forceWrapping = false,
        bool onlyShowInInitialProcess = false
    )
        : this(
            manager,
            message,
            size,
            forceWrapping: forceWrapping,
            onlyShowInInitialProcess: onlyShowInInitialProcess
        )
    {
        OnContinue += () => manager.RequestMainProcessSwitch(processOnContinue);
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
        OnContinue?.Invoke();
    }
}
