using Menu.Remix.MixedUI;
using System;
using UnityEngine;
public class RainMeadowOptions : OptionInterface
{
    public readonly Configurable<KeyCode> FriendsListKey;
    public readonly Configurable<bool> ShowFriends;
    public readonly Configurable<bool> SlugcatCustomToggle;
    public readonly Configurable<bool> ReadyToContinueToggle;
    public readonly Configurable<bool> FriendViewClickToActivate;
    public readonly Configurable<Color> BodyColor;
    public readonly Configurable<Color> EyeColor;
    public readonly Configurable<KeyCode> SpectatorKey;
    public readonly Configurable<KeyCode> PointingKey;
    public readonly Configurable<KeyCode> ChatLogKey;
    public readonly Configurable<KeyCode> ChatTalkingKey;
    public readonly Configurable<int> ArenaCountDownTimer;
    public readonly Configurable<int> ArenaSaintAscendanceTimer;
    public readonly Configurable<bool> ArenaSAINOT;



    private UIelement[] OnlineMeadowSettings;
    private UIelement[] GeneralUIArrPlayerOptions;
    private UIelement[] OnlineArenaSettings;
    private UIelement[] OnlineStorySettings;


    public RainMeadowOptions(global::RainMeadow.RainMeadow instance)
    {
        FriendsListKey = config.Bind("OpenMenuKey", KeyCode.J);
        ShowFriends = config.Bind("ShowFriends", false);
        SlugcatCustomToggle = config.Bind("SlugToggle", false);
        ReadyToContinueToggle = config.Bind("ContinueToggle", false);
        FriendViewClickToActivate = config.Bind("FriendViewHoldOrToggle", false);
        BodyColor = config.Bind("BodyColor", Color.white);
        EyeColor = config.Bind("EyeColor", Color.black);
        SpectatorKey = config.Bind("SpectatorKey", KeyCode.Tab);
        PointingKey = config.Bind("PointingKey", KeyCode.Mouse0);
        ChatLogKey = config.Bind("ChatLogKey", KeyCode.Comma);
        ChatTalkingKey = config.Bind("ChatTalkingKey", KeyCode.Return);
        ArenaCountDownTimer = config.Bind("ArenaCountDownTimer", 300);
        ArenaSaintAscendanceTimer = config.Bind("ArenaSaintAscendanceTimer", 260);
        ArenaSAINOT = config.Bind("ArenaSAINOT", false);


    }

    public override void Initialize()
    {
        try
        {
            OpTab meadowTab = new OpTab(this, "Meadow");
            OpTab opTab = new OpTab(this, "General");
            OpTab arenaTab = new OpTab(this, "Arena");
            OpTab storyTab = new OpTab(this, "Story");



            Tabs = new OpTab[4] { meadowTab, opTab, arenaTab, storyTab };

            OnlineMeadowSettings = new UIelement[1]
            {
            new OpLabel(10f, 550f, "Meadow", bigText: true),


            };
            meadowTab.AddItems(OnlineMeadowSettings);
            GeneralUIArrPlayerOptions = new UIelement[15]
            {
                new OpLabel(10f, 550f, "General", bigText: true),
                new OpLabel(10f, 530f, "Note: These inputs are not used in Meadow mode", bigText: false),


                new OpLabel(10, 490f, "Key used for viewing friends' usernames"),
                new OpKeyBinder(FriendsListKey, new Vector2(10f, 460f), new Vector2(150f, 30f)),

                new OpLabel(10f, 400f, "Username Toggle", bigText: false),
                new OpCheckBox(FriendViewClickToActivate, new Vector2(10f, 380f)),
                new OpLabel(40f, 385, RWCustom.Custom.ReplaceLineDelimeters("If selected, replaces holding to toggling to view usernames")),

                new OpLabel(10, 320f, "Key used for toggling spectator mode"),
                new OpKeyBinder(SpectatorKey, new Vector2(10f, 280f), new Vector2(150f, 30f)),

                new OpLabel(10, 245f, "Pointing"),
                new OpKeyBinder(PointingKey, new Vector2(10f, 215), new Vector2(150f, 30f)),

                new OpLabel(10, 180f, "Chat Log Toggle"),
                new OpKeyBinder(ChatLogKey, new Vector2(10f, 150), new Vector2(150f, 30f)),

                new OpLabel(10, 125f, "Chat Button"),
                new OpKeyBinder(ChatTalkingKey, new Vector2(10f, 95), new Vector2(150f, 30f)),
            };

            opTab.AddItems(GeneralUIArrPlayerOptions);

            OnlineStorySettings = new UIelement[9]
           {    new OpLabel(10f, 550f, "Story", bigText: true),

                new OpLabel(10f, 500, "Ready to shelter/gate", bigText: false),

                new OpCheckBox(ReadyToContinueToggle, new Vector2(10f, 470)),
                new OpLabel(40f, 470, RWCustom.Custom.ReplaceLineDelimeters("If selected, usernames and icons will allways appear onscreen for slugcats in a gate or shelter."))
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },

                new OpLabel(10f, 400, "[Experimental Features]", bigText: true),
                new OpLabel(10f, 380, "WARNING: Experimental features may cause data corruption, back up your saves", bigText: false),

                new OpLabel(10f, 350, "Custom Story Slugcat:", bigText: false),

                new OpCheckBox(SlugcatCustomToggle, new Vector2(160f, 350)),

                new OpLabel(40f, 320, RWCustom.Custom.ReplaceLineDelimeters("If selected, hosts can choose slugcat campaigns that are unstable."))
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                }
           };
            storyTab.AddItems(OnlineStorySettings);



            OnlineArenaSettings = new UIelement[7]
            {
                new OpLabel(10f, 550f, "Arena", bigText: true),
                new OpLabel(10f, 505, "Countdown timer. 60 == 1s", bigText: false),
                new OpTextBox(ArenaCountDownTimer, new Vector2(10, 480), 160f)
                {
                    accept = OpTextBox.Accept.Int
                },

                new OpLabel(10f, 455, "Sain't: Disable Saint ascendance", bigText: false),
                new OpCheckBox(ArenaSAINOT, new Vector2(10f, 430)),

                new OpLabel(10f, 410, "Saint ascendance duration timer. 60 == 1s", bigText: false),
                new OpTextBox(ArenaSaintAscendanceTimer, new Vector2(10, 385), 160f)
                {
                    accept = OpTextBox.Accept.Int
                }

        };
            arenaTab.AddItems(OnlineArenaSettings);

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
