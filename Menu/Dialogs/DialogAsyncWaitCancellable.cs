using Menu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;
public class DialogAsyncWaitCancellable : DialogAsyncWait
{
    public DialogAsyncWaitCancellable(Menu.Menu menu, string description, Vector2 size, Action<DialogAsyncWaitCancellable> action)
        : base(menu, description, size)
    {
        OnCancel = action;

        // From DialogBoxNotify
        continueButton = new SimpleButton(this, dialogPage, Translate("CANCEL"), "", new Vector2((int)(pos.x + size.x / 2f - 55f), (int)(pos.y + 20f)), new Vector2(110f, 30f));
        dialogPage.subObjects.Add(continueButton);
        dialogPage.selectables.Add(continueButton);
        for (int i = 0; i < 4; i++)
        {
            continueButton.nextSelectable[i] = continueButton;
        }

        selectedObject = continueButton;
        dialogPage.lastSelectedObject = continueButton;
        continueButton.buttonBehav.greyedOut = true;
    }

    public override void Update()
    {
        base.Update();
        if (timeOut > 0 && --timeOut <= 0) continueButton.buttonBehav.greyedOut = false;
    }

    public override void RemoveSprites()
    {
        base.RemoveSprites();
        continueButton.RemoveSprites();
        dialogPage.subObjects.Remove(continueButton);
        while (dialogPage.selectables.Contains(continueButton)) dialogPage.selectables.Remove(continueButton);

        selectedObject = null;
        dialogPage.lastSelectedObject = null;
    }
    public override void Singal(MenuObject sender, string message) {OnCancel(this); manager.StopSideProcess(this);}

    public readonly SimpleButton continueButton;
    public int timeOut = 40;
    private readonly Action<DialogAsyncWaitCancellable> OnCancel;
}