using Menu.Remix.MixedUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow;
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
    public readonly Configurable<KeyCode> ChatButtonKey;
    public readonly Configurable<bool> ChatLogOnOff;
    public readonly Configurable<int> ArenaCountDownTimer;
    public readonly Configurable<int> ArenaSaintAscendanceTimer;
    public readonly Configurable<bool> ArenaSAINOT;
    public readonly Configurable<bool> PainCatThrows;
    public readonly Configurable<bool> PainCatEgg;
    public readonly Configurable<bool> PainCatLizard;
    public readonly Configurable<bool> BlockMaul;
    public readonly Configurable<bool> BlockArtiStun;

    public readonly Configurable<float> ScrollSpeed;


    public readonly Configurable<string> LanUserName;
    public readonly Configurable<bool> DisableMeadowPauseAnimation;
    public readonly Configurable<bool> StopMovementWhileSpectateOverlayActive;


    public readonly Configurable<IntroRoll> PickedIntroRoll;

    public enum IntroRoll
    {
        Meadow,
        Vanilla,
        Downpour
    }


    private UIelement[] OnlineMeadowSettings;
    private UIelement[] GeneralUIArrPlayerOptions;
    private UIelement[] OnlineArenaSettings;
    private UIelement[] OnlineStorySettings;
    private UIelement[] OnlineLANSettings;



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
        ChatButtonKey = config.Bind("ChatButtonKey", KeyCode.Return);
        ChatLogOnOff = config.Bind("ChatLogOnOff", true);
        ArenaCountDownTimer = config.Bind("ArenaCountDownTimer", 5);
        ArenaSaintAscendanceTimer = config.Bind("ArenaSaintAscendanceTimer", 120);
        ArenaSAINOT = config.Bind("ArenaSAINOT", false);
        PainCatThrows = config.Bind("PainCatThrows", false);
        PainCatEgg = config.Bind("PainCatEgg", true);
        PainCatLizard = config.Bind("PainCatLizard", true);
        BlockMaul = config.Bind("BlockMaul", false);
        BlockArtiStun = config.Bind("BlockArtiStun", false);

        ScrollSpeed = config.Bind("ScrollSpeed", 10f);


        PickedIntroRoll = config.Bind("PickedIntroRoll", IntroRoll.Meadow);
        LanUserName = config.Bind("LanUserName", "");

        DisableMeadowPauseAnimation = config.Bind("DisableMeadowPauseAnimation", false);
        StopMovementWhileSpectateOverlayActive = config.Bind("StopMovementWhileSpectateOverlayActive", false);

    }

    public override void Initialize()
    {
        try
        {
            OpTab opTab = new OpTab(this, Translate("General"));
            OpTab meadowTab = new OpTab(this, Translate("Meadow"));
            OpTab arenaTab = new OpTab(this, Translate("Arena"));
            OpTab storyTab = new OpTab(this, Translate("Story"));
            OpTab lanTab = new OpTab(this, Translate("LAN"));



            Tabs = new OpTab[] { opTab, meadowTab, arenaTab, storyTab, lanTab };

            List<UIelement> meadowCheats;
            OpTextBox meadowCheatBox;
            OpSimpleButton cheatEmote;
            OpSimpleButton cheatSkin;
            OpSimpleButton cheatCharacter;
            OpSimpleButton cheatReset;
            float cheaty = 130f;
            OnlineMeadowSettings = new UIelement[]
            {
                new OpLabel(10f, 550f, Translate("Meadow"), bigText: true),

                new OpLabel(10f, 505f, Translate("Disable Pause Menu Animation"), bigText: false),
                new OpCheckBox(DisableMeadowPauseAnimation, new Vector2(10f, 480f)),
                new OpLabel(40f, 480f, RWCustom.Custom.ReplaceLineDelimeters(Translate("If selected, disables the sway animation in the pause menu"))),

                meadowCheatBox = new OpTextBox(config.Bind("",""), new Vector2(10f, cheaty), 80f),
                new OpLabel(110f, cheaty, Translate("Input “cheats” to access cheats")),
                new OpLabel(110f, cheaty - 24f, Translate("Just make sure not to ruin the fun for yourself...")),
                new OpLabel(110f, cheaty - 48f, Translate("Emote and skin unlocks will affect the currently selected character")),

                cheatEmote = new OpSimpleButton(new Vector2(10f, cheaty - 80f), new Vector2(110f, 30f), Translate("Unlock Emote")) { greyedOut = MeadowProgression.NextUnlockableEmote() == null},
                cheatSkin = new OpSimpleButton(new Vector2(130f, cheaty - 80f), new Vector2(110f, 30f), Translate("Unlock Skin")) { greyedOut = MeadowProgression.NextUnlockableSkin() == null},
                cheatCharacter = new OpSimpleButton(new Vector2(250f, cheaty - 80f), new Vector2(110f, 30f), Translate("Unlock Character")) { greyedOut = MeadowProgression.NextUnlockableCharacter() == null},
                cheatReset = new OpSimpleButton(new Vector2(10f, cheaty - 120f), new Vector2(230f, 30f), Translate("Reset all progression")),
            };
            meadowCheats = OnlineMeadowSettings.Skip(OnlineMeadowSettings.IndexOf(meadowCheatBox) + 2).ToList();
            meadowCheats.ForEach(cheat => cheat.Hidden = true);
            meadowCheatBox.OnValueChanged += (UIconfig config, string value, string oldValue) => { if (value == "cheats") meadowCheats.ForEach(cheat => cheat.Show()); else meadowCheats.ForEach(cheat => cheat.Hide()); };
            cheatEmote.OnClick += (UIfocusable trigger) => { trigger.Menu.PlaySound(SoundID.HUD_Game_Over_Prompt); if (MeadowProgression.NextUnlockableEmote() != null) while (MeadowProgression.EmoteProgress() == null) ; (trigger as OpSimpleButton).greyedOut = MeadowProgression.NextUnlockableEmote() == null; };
            cheatSkin.OnClick += (UIfocusable trigger) => { trigger.Menu.PlaySound(SoundID.HUD_Game_Over_Prompt); if (MeadowProgression.NextUnlockableSkin() != null) while (MeadowProgression.SkinProgress() == null) ; (trigger as OpSimpleButton).greyedOut = MeadowProgression.NextUnlockableSkin() == null; };
            cheatCharacter.OnClick += (UIfocusable trigger) => { trigger.Menu.PlaySound(SoundID.HUD_Game_Over_Prompt); if (MeadowProgression.NextUnlockableCharacter() != null) while (MeadowProgression.CharacterProgress() == null) ; (trigger as OpSimpleButton).greyedOut = MeadowProgression.NextUnlockableCharacter() == null; };
            cheatReset.OnClick += (UIfocusable trigger) => { trigger.Menu.PlaySound(SoundID.HUD_Karma_Reinforce_Flicker); MeadowProgression.progressionData = null; MeadowProgression.LoadDefaultProgression(); cheatEmote.greyedOut = cheatSkin.greyedOut = cheatCharacter.greyedOut = false; };

            meadowTab.AddItems(OnlineMeadowSettings);

            OpComboBox2 introroll;
            OpLabel downpourWarning;
            OpSimpleButton editSyncRequiredModsButton;
            OpSimpleButton editBannedModsButton;

            GeneralUIArrPlayerOptions = new UIelement[]
            {
                new OpLabel(10f, 550f, Translate("General"), bigText: true),
                new OpLabel(10f, 530f, Translate("Note: These inputs are not used in Meadow mode"), bigText: false),

                new OpLabel(10, 490f, Translate("Show usernames")),
                new OpKeyBinder(FriendsListKey, new Vector2(10f, 460f), new Vector2(150f, 30f)),

                new OpLabel(310f, 490f, Translate("Username Toggle"), bigText: false),
                new OpCheckBox(FriendViewClickToActivate, new Vector2(310f, 465f)),
                new OpLabel(340f, 475f, RWCustom.Custom.ReplaceLineDelimeters(Translate("Replace holding with toggling"))),

                new OpLabel(10, 400f, Translate("Key used for toggling spectator mode")),
                new OpKeyBinder(SpectatorKey, new Vector2(10f, 370f), new Vector2(150f, 30f)),

                new OpLabel(310, 400f, Translate("Stop Inputs While Spectating")),
                new OpCheckBox(StopMovementWhileSpectateOverlayActive, new Vector2(310f, 375f)),

                new OpLabel(10f, 300f, RWCustom.Custom.ReplaceLineDelimeters(Translate("Control which mods are permitted on clients by editing the files below.<LINE>Instructions included within."))),
                editSyncRequiredModsButton = new OpSimpleButton(new Vector2(10f, 260f), new Vector2(150f, 30f), Translate("Edit High-Impact Mods")),
                editBannedModsButton = new OpSimpleButton(new Vector2(185f, 260f), new Vector2(150f, 30f), Translate("Edit Banned Mods")),

                new OpLabel(10, 180f, Translate("Chat Log Toggle")),
                new OpKeyBinder(ChatLogKey, new Vector2(10f, 150), new Vector2(150f, 30f)),

                new OpLabel(210, 180f, Translate("Chat Talk Button")),
                new OpKeyBinder(ChatButtonKey, new Vector2(210f, 150), new Vector2(150f, 30f)),

                new OpLabel(410, 180f, Translate("Chat Log On/Off")),
                new OpCheckBox(ChatLogOnOff, new Vector2(440f, 150f)),

                new OpLabel(10, 120, Translate("Introroll")),
                introroll = new OpComboBox2(PickedIntroRoll, new Vector2(10, 90f), 160f, OpResourceSelector.GetEnumNames(null, typeof(IntroRoll)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = Menu.MenuColorEffect.rgbWhite },
                downpourWarning = new OpLabel(introroll.pos.x + 170, 90, Translate("Downpour DLC is not activated, vanilla intro will be used instead")),
                new OpLabel(10f, 50, Translate("Player Menu Scroll Speed for Spectate, Story menu, Arena results.  Default: 5"), bigText: false),
                new OpTextBox(ScrollSpeed, new Vector2(10, 25), 160f)
                {
                    accept = OpTextBox.Accept.Float
                },
        };
            introroll.OnValueChanged += (UIconfig config, string value, string oldValue) => { if (value == "Downpour" && introroll.Menu.manager.rainWorld.dlcVersion == 0) downpourWarning.Show(); else downpourWarning.Hide(); };
            downpourWarning.Hidden = PickedIntroRoll.Value != IntroRoll.Downpour && introroll.Menu.manager.rainWorld.dlcVersion == 0;

            editSyncRequiredModsButton.OnClick += _ =>
            {
                try
                {
                    RainMeadowModManager.GetRequiredMods();
                    System.Diagnostics.Process.Start(AssetManager.ResolveFilePath(RainMeadowModManager.SyncRequiredModsFileName));
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            };

            editBannedModsButton.OnClick += _ =>
            {
                try
                {
                    RainMeadowModManager.GetBannedMods();
                    System.Diagnostics.Process.Start(AssetManager.ResolveFilePath(RainMeadowModManager.BannedOnlineModsFileName));
                }
                catch (Exception e)
                {
                    RainMeadow.Error(e);
                }
            };

            opTab.AddItems(GeneralUIArrPlayerOptions);

            OnlineStorySettings = new UIelement[9]
           {    new OpLabel(10f, 550f, Translate("Story"), bigText: true),

                new OpLabel(10f, 500, Translate("Ready to shelter/gate"), bigText: false),

                new OpCheckBox(ReadyToContinueToggle, new Vector2(10f, 470)),
                new OpLabel(40f, 470, RWCustom.Custom.ReplaceLineDelimeters(Translate("If selected, usernames and icons will allways appear onscreen for slugcats in a gate or shelter.")))
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },

                new OpLabel(10f, 400, Translate("[Experimental Features]"), bigText: true),
                new OpLabel(10f, 380, Translate("WARNING: Experimental features may cause data corruption, back up your saves"), bigText: false),

                new OpLabel(10f, 350, Translate("Custom Story Slugcat:"), bigText: false),

                new OpCheckBox(SlugcatCustomToggle, new Vector2(160f, 350)),

                new OpLabel(40f, 320, RWCustom.Custom.ReplaceLineDelimeters(Translate("If selected, hosts can choose slugcat campaigns that are unstable.")))
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                }
           };
            storyTab.AddItems(OnlineStorySettings);



            OnlineArenaSettings = new UIelement[17]

            {
                new OpLabel(10f, 550f, Translate("Arena"), bigText: true),
                new OpLabel(10f, 505, Translate("Countdown timer. Default: 5s"), bigText: false),
                new OpTextBox(ArenaCountDownTimer, new Vector2(10, 480), 160f)
                {
                    accept = OpTextBox.Accept.Int
                },

                new OpLabel(10f, 455, Translate("Sain't: Disable Saint ascendance"), bigText: false),
                new OpCheckBox(ArenaSAINOT, new Vector2(10f, 430)),

                new OpLabel(10f, 410, Translate("Saint ascendance duration timer. Default: 120"), bigText: false),
                new OpTextBox(ArenaSaintAscendanceTimer, new Vector2(10, 385), 160f)
                {
                    accept = OpTextBox.Accept.Int
                },
                new OpLabel(10f, 350, Translate("Inv: Enable spear throws at 0 throw skill"), bigText: false),
                new OpCheckBox(PainCatThrows, new Vector2(10f, 315)),

                new OpLabel(10f, 285, Translate("Inv: Enable egg at 0 throw skill"), bigText: false),
                new OpCheckBox(PainCatEgg, new Vector2(10f, 250)),


                new OpLabel(10f, 215, Translate("Inv: Enable ???"), bigText: false),
                new OpCheckBox(PainCatLizard, new Vector2(10f, 185)),

                new OpLabel(10f, 160, Translate("Artificer: Disable Stun"), bigText: false),
                new OpCheckBox(BlockArtiStun, new Vector2(10f, 125)),

                new OpLabel(10f, 100, Translate("Mauling: Disable"), bigText: false),
                new OpCheckBox(BlockMaul, new Vector2(10f, 75)),


        };
            arenaTab.AddItems(OnlineArenaSettings);

            OnlineLANSettings = new UIelement[3]
            {
                new OpLabel(10f, 550f, Translate("LAN"), bigText: true),
                new OpLabel(10f, 505, Translate("Username"), bigText: false),

                new OpTextBox(LanUserName, new Vector2(10f, 480), 160f)
                {
                    accept = OpTextBox.Accept.StringASCII
                }

        };
            lanTab.AddItems(OnlineLANSettings);

        }

        catch (Exception ex)
        {
            RainMeadow.Error("Error opening RainMeadow Options Menu" + ex);
        }
    }

    public override void Update()
    {
    }
}
