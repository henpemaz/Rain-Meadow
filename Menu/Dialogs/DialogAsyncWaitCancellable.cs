using System;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class DialogAsyncWaitCancellable : DialogAsyncWait
{
    public EventfulButton continueButton;

    public event Action? OnCancel;

    public int timeOut = 40;

    public DialogAsyncWaitCancellable(
        ProcessManager manager,
        string description,
        Vector2 size,
        Action onCancel
    )
        : base(manager, description, size)
    {
        OnCancel = onCancel;

        // From DialogBoxNotify
        continueButton = new EventfulButton(
            this,
            dialogPage,
            Translate("CANCEL"),
            new Vector2((int)(pos.x + size.x / 2f - 55f), (int)(pos.y + 20f)),
            new Vector2(110f, 30f)
        );
        continueButton.OnClick += (btn) =>
        {
            manager.StopSideProcess(this);
            OnCancel?.Invoke();
        };

        dialogPage.subObjects.Add(continueButton);
        dialogPage.selectables.Add(continueButton);
        for (int i = 0; i < 4; i++)
            continueButton.nextSelectable[i] = continueButton;

        selectedObject = continueButton;
        dialogPage.lastSelectedObject = continueButton;
        continueButton.buttonBehav.greyedOut = true;
    }

    public override void Update()
    {
        base.Update();
        if (timeOut > 0 && --timeOut <= 0)
            continueButton.buttonBehav.greyedOut = false;
    }
}
