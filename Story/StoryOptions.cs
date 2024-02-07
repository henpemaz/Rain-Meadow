
// BeastMaster.BeastMasterOptions
using System;
using RainMeadow;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;
using RainMeadow;

public class FriendsToggleOption : OptionInterface
{
    public readonly Configurable<KeyCode> FriendsListKey;

    private UIelement[] UIArrPlayerOptions;

    // TODO: Controller support
    public FriendsToggleOption(global::RainMeadow.RainMeadow instance)
    {
        FriendsListKey = config.Bind("OpenMenuKey", KeyCode.F11);
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
