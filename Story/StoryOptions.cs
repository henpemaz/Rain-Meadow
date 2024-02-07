
// BeastMaster.BeastMasterOptions
using System;
using RainMeadow;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;
using RainMeadow;

public class RainMeadowOptions : OptionInterface
{
    public readonly Configurable<KeyCode> FriendsListKey;

    private UIelement[] UIArrPlayerOptions;

    public RainMeadowOptions(global::RainMeadow.RainMeadow instance)
    {
        // TODO: Controller support
        // TODO: Add the ability for users to select a custom color or default to white
        // TODO: Maybe as shadow or box?
        FriendsListKey = config.Bind("OpenMenuKey", KeyCode.O);
    }

    public override void Initialize()
    {
        try
        {
            OpTab opTab = new OpTab(this, "Options");
            Tabs = new OpTab[1] { opTab };
            UIArrPlayerOptions = new UIelement[3]
            {
                new OpLabel(10f, 550f, "Options", bigText: true),
                new OpKeyBinder(FriendsListKey, new Vector2(10f, 480f), new Vector2(150f, 30f)),
                new OpLabel(166f, 480f, "Key used for viewing friends usernames")
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                }
            };
            opTab.AddItems(UIArrPlayerOptions);
        }
        catch (Exception ex)
        {
            RainMeadow.RainMeadow.Debug("Error opening RainMeadow Options Menu" + ex);
        }
    }

    public override void Update()
    {
    }
}
