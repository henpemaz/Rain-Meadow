using Menu;
using Menu.Remix;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NullLobbyError : MenuDialogBox
{
    SimpleButton okButton;
    MenuTabWrapper tabWrapper;

    public NullLobbyError(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string description, bool forceWrapping)
        : base(menu, owner, description, pos, size, forceWrapping)
    {
        tabWrapper = new MenuTabWrapper(menu, this);

        okButton = new SimpleButton(
            menu, 
            this, 
            menu.Translate("ok"), 
            "OK", 
            new Vector2((this.roundedRect.size.x * 0.5f) - 55f, Mathf.Max(this.size.y * 0.04f, 7f)), 
            new Vector2(110f, 30f)
        );        
        subObjects.Add(okButton);

    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "OK":
                menu.manager.RequestMainProcessSwitch(RainMeadow.RainMeadow.Ext_ProcessID.LobbySelectMenu);
                break;
        }
    }

    public void SetText(string caption)
    {
        descriptionLabel.text = caption;
    }
}