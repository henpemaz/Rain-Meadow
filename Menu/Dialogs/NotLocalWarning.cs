using Menu;
using Menu.Remix;
using System;
using System.Collections.Generic;
using UnityEngine;

public class NotLocalWarningDialog : MenuDialogBox
{
    Action OnOk;
    Action OnCancel;
    FSprite fSpriteSurvivor;
    FSprite fSpriteInv;

    SimpleButton okButton;
    SimpleButton cancelButton;

    MenuTabWrapper tabWrapper;

    public NotLocalWarningDialog(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size, string description, bool forceWrapping, Action ok, Action cancel)
        : base(menu, owner, description, pos, size, forceWrapping)
    {
        tabWrapper = new MenuTabWrapper(menu, this);

        okButton = new SimpleButton(menu, this, menu.Translate("ok"), "OK", 
        new Vector2((size.x * 0.5f) - 55f - 110f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
        cancelButton = new SimpleButton(menu, this, menu.Translate("cancel"), "CANCEL", new Vector2((size.x * 0.5f) - 55f + 110f, Mathf.Max(size.y * 0.04f, 7f)), new Vector2(110f, 30f));
        subObjects.Add(okButton);
        subObjects.Add(cancelButton);
        
        OnOk = ok;
        OnCancel = cancel;   
        if (!Futile.atlasManager.DoesContainAtlas("notlocalwarning"))
        {
            HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/notlocalwarning").name);
        }
        fSpriteSurvivor = new FSprite("warning_survivor");
        fSpriteInv = new FSprite("warning_inv");

        var center = pos + (size/2.0f) + new Vector2(7f, -40f);
        fSpriteSurvivor.SetPosition(center - new Vector2(size.x/5, 0.0f));
        fSpriteInv.SetPosition(center + new Vector2(size.x/5, 0.0f));

        Container.AddChild(fSpriteSurvivor);
        Container.AddChild(fSpriteInv);
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        switch (message)
        {
            case "OK":
                if (OnOk != null)
                {
                    OnOk();
                }
                break;
            case "CANCEL":
                if (OnCancel != null)
                {
                    OnCancel();
                }
                break;
        }
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        fSpriteSurvivor.RemoveFromContainer();
        fSpriteInv.RemoveFromContainer();
    }

    public void SetText(string caption)
    {
        descriptionLabel.text = caption;
    }
}