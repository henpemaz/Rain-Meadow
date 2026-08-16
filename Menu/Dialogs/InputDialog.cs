using System;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class InputDialog : Dialog
{
    public MenuTabWrapper tabWrapper;

    public event Action<string>? OnConfirm;

    public InputDialog(
        ProcessManager manager,
        string text,
        Vector2 size,
        bool forceWrapping = false
    )
        : base(manager)
    {
        Vector2 center = UIUtils.ScreenCenter(manager);

        DialogBox dialogBox = new(
            this,
            dialogPage,
            Translate(text),
            center - (size / 2f),
            size,
            forceWrapping
        );

        tabWrapper = new MenuTabWrapper(this, dialogPage);

        textBox = new(new Configurable<string>(""), center - new Vector2(80f, 15f), 160f)
        {
            accept = OpTextBox.Accept.StringASCII,
            allowSpace = true,
        };
        textBox.OnKeyDown = (Action<char>)Delegate.Combine(new Action<char>(HasPressedEnter), textBox.OnKeyDown);

        SimplerButton continueButton = new(
            this,
            dialogPage,
            Translate("CONFIRM"),
            center - new Vector2(55, 140),
            new Vector2(110f, 30f)
        );
        continueButton.OnClick += (btn) => Enter();

        SimplerSymbolButton cancelButton = new(
            this,
            dialogPage,
            "Menu_Symbol_Clear_All",
            "",
            center + (size / 2) - new Vector2(40f, 40f)
        );
        cancelButton.OnClick += (btn) => manager.StopSideProcess(this);

        new UIelementWrapper(tabWrapper, textBox);
        dialogPage.subObjects.AddRange([dialogBox, tabWrapper, continueButton, cancelButton]);
    }
    private void HasPressedEnter(char input)
    {
        if (textBox._keyboardOn 
            && !string.IsNullOrWhiteSpace(textBox.value) 
            && (input == '\n' || input == '\r')) 
                Enter();
    }
    private void Enter()
    {
        manager.StopSideProcess(this);
        OnConfirm?.Invoke(textBox.value);
    }
    private readonly OpTextBox textBox;
}
