using System;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Base;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class InputDialog : Dialog
{
    public OpTextBox textBox;
    public MenuTabWrapper tabWrapper;

    public event Action<string>? OnConfirm;

    public InputDialog(
        ProcessManager manager,
        string text,
        Vector2 size,
        Action<string>? onConfirm = null,
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

        void OnEnter()
        {
            manager.StopSideProcess(this);
            OnConfirm?.Invoke(textBox.value);
        }

        void HasPressedEnter(char input)
        {
            if (
                textBox._keyboardOn
                && !string.IsNullOrWhiteSpace(textBox.value)
                && (input == '\n' || input == '\r')
            )
                OnEnter();
        }

        textBox = new(new Configurable<string>(""), center - new Vector2(80f, 15f), 160f)
        {
            accept = OpTextBox.Accept.StringASCII,
            allowSpace = true,
        };
        textBox.OnKeyDown = (Action<char>)Delegate.Combine(HasPressedEnter, textBox.OnKeyDown);

        EventfulButton continueButton = new(
            this,
            dialogPage,
            Translate("CONFIRM"),
            center - new Vector2(55, 140),
            new Vector2(110f, 30f),
            onClick: (btn) => OnEnter()
        );

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

        OnConfirm += onConfirm;
    }
}
