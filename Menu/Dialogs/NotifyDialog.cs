using System;
using Menu;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class NotifyDialog : Dialog
{
    public DialogBoxNotify dialogBox;
    public ProcessManager.ProcessID initialProcessID;

    public event Action? OnContinue;

    public SoundID SoundOnButtonPress = SoundID.MENU_Button_Standard_Button_Pressed;

    public bool OnlyShowInInitialProcess;

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        Action? onContinue = null,
        bool forceWrapping = false,
        float timeOut = 1f
    )
        : base(manager)
    {
        dialogPage.subObjects.Add(
            dialogBox = new DialogBoxNotify(
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
        dialogBox.timeOut = timeOut;
        initialProcessID = manager.currentMainLoop.ID;
    }

    public NotifyDialog(
        ProcessManager manager,
        string message,
        Vector2 size,
        ProcessManager.ProcessID processOnContinue,
        bool forceWrapping = false,
        float timeOut = 1f
    )
        : this(manager, message, size, forceWrapping: forceWrapping, timeOut: timeOut)
    {
        OnContinue += () => manager.RequestMainProcessSwitch(processOnContinue);
    }

    public override void Update()
    {
        base.Update();
        if (OnlyShowInInitialProcess && manager.currentMainLoop.ID != initialProcessID)
            manager.StopSideProcess(this);
    }

    public override void Singal(MenuObject sender, string message)
    {
        PlaySound(SoundOnButtonPress);
        manager.StopSideProcess(this);
        OnContinue?.Invoke();
    }
}
