using System;
using RainMeadow;
using BepInEx.Logging;
using Menu.Remix.MixedUI;
using UnityEngine;
using RainMeadow;

public class RainMeadowOptions : OptionInterface
{
    public readonly Configurable<KeyCode> FriendsListKey;
    public readonly Configurable<bool> SlugcatCustomToggle;
    private bool slugcatToggle = false;

    private UIelement[] UIArrPlayerOptions;
    


    public RainMeadowOptions(global::RainMeadow.RainMeadow instance)
    {

        FriendsListKey = config.Bind("OpenMenuKey", KeyCode.J);
        SlugcatCustomToggle = config.Bind("SlugToggle", false);

    }

    public override void Initialize()
    {
        try
        {
            OpTab opTab = new OpTab(this, "Options");
            Tabs = new OpTab[1] { opTab };
            UIArrPlayerOptions = new UIelement[8]
            {
                new OpLabel(10f, 550f, "Options", bigText: true),
                new OpKeyBinder(FriendsListKey, new Vector2(10f, 480f), new Vector2(150f, 30f)),
                new OpLabel(166f, 480f, "Key used for viewing friends usernames")
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },
            
                new OpLabel(10f, 230f, "[Experimental Features]", bigText: true),
                new OpLabel(10f, 215f, "WARNING: Experimental features may cause data corruption, back up your saves", bigText: false),

                new OpLabel(10f, 185f, "Custom Story Slugcat", bigText: false),

                new OpCheckBox(SlugcatCustomToggle, new Vector2(10f, 160f)),
                new OpLabel(40f, 160f, "If selected, clients can choose their own Slugcats inside a Story campaign")
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                }
};
            opTab.AddItems(UIArrPlayerOptions);
        }
        catch (Exception ex)
        {
            RainMeadow.RainMeadow.Error("Error opening RainMeadow Options Menu" + ex);
        }
    }

    public override void Update()
    {

    }
}