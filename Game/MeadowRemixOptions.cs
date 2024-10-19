using Menu.Remix.MixedUI;
using System;
using UnityEngine;

public class RainMeadowOptions : OptionInterface
{
    public readonly Configurable<KeyCode> FriendsListKey;
    public readonly Configurable<bool> SlugcatCustomToggle;
    public readonly Configurable<bool> FriendViewClickToActivate;
    public readonly Configurable<Color> BodyColor;
    public readonly Configurable<Color> EyeColor;
    public readonly Configurable<KeyCode> SpectatorKey;
    public readonly Configurable<KeyCode> PointingKey;



    private UIelement[] UIArrPlayerOptions;

    public RainMeadowOptions(global::RainMeadow.RainMeadow instance)
    {
        FriendsListKey = config.Bind("OpenMenuKey", KeyCode.J);
        SlugcatCustomToggle = config.Bind("SlugToggle", false);
        FriendViewClickToActivate = config.Bind("FriendViewHoldOrToggle", false);
        BodyColor = config.Bind("BodyColor", Color.white);
        EyeColor = config.Bind("EyeColor", Color.black);
        SpectatorKey = config.Bind("SpectatorKey", KeyCode.Tab);
        PointingKey = config.Bind("PointingKey", KeyCode.Mouse0);


    }

    public override void Initialize()
    {
        try
        {
            OpTab opTab = new OpTab(this, "Options");
            Tabs = new OpTab[1] { opTab };
            UIArrPlayerOptions = new UIelement[15]
            {
                new OpLabel(10f, 550f, "Options", bigText: true),

                new OpLabel(10, 500f, "Key used for viewing friends' usernames"),
                new OpKeyBinder(FriendsListKey, new Vector2(10f, 460f), new Vector2(150f, 30f)),

                new OpLabel(10f, 410f, "Username Toggle", bigText: false),
                new OpCheckBox(FriendViewClickToActivate, new Vector2(10f, 380f)),
                new OpLabel(40f, 385, RWCustom.Custom.ReplaceLineDelimeters("If selected, replaces holding to toggling to view usernames")),

                new OpLabel(10, 320f, "Key used for toggling spectator mode"),
                new OpKeyBinder(SpectatorKey, new Vector2(10f, 280f), new Vector2(150f, 30f)),

                new OpLabel(10, 245f, "Story / Arena: Pointing"),
                new OpKeyBinder(PointingKey, new Vector2(10f, 215), new Vector2(150f, 30f)),

                new OpLabel(10f, 105f, "[Experimental Features]", bigText: true),
                new OpLabel(10f, 85, "WARNING: Experimental features may cause data corruption, back up your saves", bigText: false),

                new OpLabel(10f, 65, "Custom Story Slugcat", bigText: false),

                new OpCheckBox(SlugcatCustomToggle, new Vector2(10f, 40)),
                new OpLabel(40f, 45, RWCustom.Custom.ReplaceLineDelimeters("If selected, hosts can choose slugcat campaigns that are unstable. <LINE>Clients can choose their own Slugcats inside a host's Story campaign"))
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