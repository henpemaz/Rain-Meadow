using System;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow.UI.Dialogs;

public class DirectConnectionDialog : InputDialog
{
    public OpTextBox passwordBox;

    public event Action<string, string?>? OnDirectConnectConfirm;

    public DirectConnectionDialog(ProcessManager manager, Vector2 size)
        : base(manager, "Direct Connection", size)
    {
        MenuLabel passwordLabel = new(
            this,
            dialogPage,
            "Password",
            UIUtils.ScreenCenter(manager) - new Vector2(0, 40),
            Vector2.zero,
            false
        );

        passwordBox = new OpTextBox(
            new Configurable<string>(""),
            UIUtils.ScreenCenter(manager) - new Vector2(80, 74),
            160f
        )
        {
            accept = OpTextBox.Accept.StringASCII,
            allowSpace = true,
            greyedOut = true,
        };

        SimplerCheckbox hasPasswordCheckBox = new(
            this,
            dialogPage,
            UIUtils.ScreenCenter(manager) - new Vector2(114, 74),
            0,
            ""
        );
        hasPasswordCheckBox.OnClick += (checkBox) => passwordBox.greyedOut = !passwordBox.greyedOut;

        new UIelementWrapper(tabWrapper, passwordBox);
        dialogPage.subObjects.AddRange([passwordLabel, hasPasswordCheckBox]);

        OnConfirm += (ip) =>
            OnDirectConnectConfirm?.Invoke(
                ip,
                hasPasswordCheckBox.Checked ? passwordBox.value : null
            );
    }
}
