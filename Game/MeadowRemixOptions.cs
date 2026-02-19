using HarmonyLib;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
    public readonly Configurable<bool> ChatLogOnOff, ChatPing;
    public readonly Configurable<int> ArenaCountDownTimer;
    public readonly Configurable<int> ArenaSaintAscendanceTimer;
    public readonly Configurable<int> ArenaWatcherCamoTimer;

    public readonly Configurable<bool> ProfanityFilter;

    public readonly Configurable<bool> ArenaSAINOT;
    public readonly Configurable<bool> PainCatThrows;
    public readonly Configurable<bool> PainCatEgg;
    public readonly Configurable<bool> PainCatLizard;
    public readonly Configurable<bool> WeaverWatcher;
    public readonly Configurable<bool> VoidMaster;

    public readonly Configurable<int> AmoebaDuration;
    public readonly Configurable<bool> AmoebaControl;


    public readonly Configurable<bool> BlockMaul;
    public readonly Configurable<bool> BlockArtiStun, ArenaAllowMidJoin;
    public readonly Configurable<bool> WearingCape;
    public readonly Configurable<bool> SlugpupHellBackground;
    public readonly Configurable<bool> StoryItemSteal;
    public readonly Configurable<bool> ArenaItemSteal;
    public readonly Configurable<bool> WeaponCollisionFix;

    public readonly Configurable<bool> EnableBombs;
    public readonly Configurable<bool> EnableCorpseGrab;
    public readonly Configurable<bool> EnableBees;

    public readonly Configurable<bool> EnablePiggyBack;
    public readonly Configurable<StreamMode> StreamerMode;
    public readonly Configurable<int> ArenaWatcherRippleLevel;

    public readonly Configurable<Color> MartyrTeamColor, OutlawsTeamColor, DragonSlayersTeamColor, ChieftainTeamColor;
    public readonly Configurable<string> MartyrTeamName;
    public readonly Configurable<string> OutlawsTeamName;
    public readonly Configurable<string> DragonSlayersTeamName;
    public readonly Configurable<string> ChieftainTeamName;
    public readonly Configurable<float> TeamColorLerp;

    public readonly Configurable<float> ScrollSpeed, ChatBgOpacity, ChatInactivityOpacity;
    public readonly Configurable<bool> ShowPing;
    public readonly Configurable<int> ShowPingLocation, ChatInactivityTimer;

    public readonly Configurable<string> LanUserName;
    public readonly Configurable<int> UdpTimeout;
    public readonly Configurable<int> UdpHeartbeat;
    public readonly Configurable<bool> DisableMeadowPauseAnimation;
    public readonly Configurable<bool> StopMovementWhileSpectateOverlayActive;

    public readonly Configurable<bool> DevNightskySkin;
    public readonly Configurable<bool> EnableAchievementsOnline;

    public readonly Configurable<IntroRoll> PickedIntroRoll;
    private readonly Configurable<string> LobbyMusic;
    public readonly Configurable<bool> AnniversaryCape;
    public readonly Configurable<int> MeadowCoins;

    public enum IntroRoll
    {
        Meadow,
        Vanilla,
        Downpour,
        Watcher
    }

    public enum StreamMode
    {
        None,
        Me,
        Everyone
    }


    private UIelement[] OnlineMeadowSettings;
    private UIelement[] GeneralUIArrPlayerOptions;
    private UIelement[] OnlineArenaSettings;
    private UIelement[] OnlineStorySettings;
    private UIelement[] OnlineLANSettings;
    private UIelement[] OnlineAdvancedSettings;
    private UIelement[] OnlineGameplay;



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
        ChatPing = config.Bind("ChatPing", true);
        ArenaCountDownTimer = config.Bind("ArenaCountDownTimer", 5);

        ArenaSaintAscendanceTimer = config.Bind("ArenaSaintAscendanceTimer", 3);
        ArenaWatcherCamoTimer = config.Bind("ArenaWatcherCamoTimer", 12);

        ProfanityFilter = config.Bind("ProfanityFilter", true);

        ArenaSAINOT = config.Bind("ArenaSAINOT", false);
        ArenaAllowMidJoin = config.Bind("ArenaAllowMidJoin", true);

        PainCatThrows = config.Bind("PainCatThrows", false);
        PainCatEgg = config.Bind("PainCatEgg", true);
        PainCatLizard = config.Bind("PainCatLizard", true);
        BlockMaul = config.Bind("BlockMaul", false);
        BlockArtiStun = config.Bind("BlockArtiStun", false);
        WeaverWatcher = config.Bind("WeaverWatcher", false);
        VoidMaster = config.Bind("VoidMaster", false);
        AmoebaDuration = config.Bind("AmoebaDuration", 7);
        AmoebaControl = config.Bind("AmoebaControl", false);
        ArenaWatcherRippleLevel = config.Bind("ArenaWatcherRippleLevel", 1);


        MartyrTeamColor = config.Bind("MartyrTeamColor", new Color(1, 0.49f, 0.49f));
        OutlawsTeamColor = config.Bind("OutlawsTeamColor", new Color(1, 1, 0.49f));
        DragonSlayersTeamColor = config.Bind("DragonSlayersTeamColor", new Color(0.49f, 1, 0.49f));
        ChieftainTeamColor = config.Bind("ChieftainTeamColor", new Color(0.49f, 0.49f, 1));

        MartyrTeamName = config.Bind("MartyrTeamName", "Martyrs");
        OutlawsTeamName = config.Bind("OutlawsTeamName", "Outlaws");
        DragonSlayersTeamName = config.Bind("DragonSlayersTeamName", "Dragonslayers");
        ChieftainTeamName = config.Bind("ChieftainTeamName", "Chieftains");
        TeamColorLerp = config.Bind("TeamColorLerp", 1f);


        SlugpupHellBackground = config.Bind("SlugpupHellBackground", false);

        WeaponCollisionFix = config.Bind("WeaponCollisionFix", true);
        EnableBees = config.Bind("EnableBees", true);
        EnableBombs = config.Bind("EnableBombs", true);
        EnableCorpseGrab = config.Bind("EnableCorpseGrab", true);

        ShowPing = config.Bind("ShowPing", false);
        ShowPingLocation = config.Bind("ShowPingLocation", 0);
        ScrollSpeed = config.Bind("ScrollSpeed", 10f);
        WearingCape = config.Bind("WearingCape", true);

        StoryItemSteal = config.Bind("StoryItemSteal", false);
        ArenaItemSteal = config.Bind("ArenaItemSteal", false);
        EnablePiggyBack = config.Bind("EnablePiggyBack", true);


        PickedIntroRoll = config.Bind("PickedIntroRoll", IntroRoll.Meadow);
        LobbyMusic = config.Bind("MeadowLobbyMusic", "default"); // Happy One Year, Meadow

        LanUserName = config.Bind("LanUserName", "");
        UdpTimeout = config.Bind("UdpTimeout", 3000);
        UdpHeartbeat = config.Bind("UdpHeartbeat", 500);

        DisableMeadowPauseAnimation = config.Bind("DisableMeadowPauseAnimation", false);
        StopMovementWhileSpectateOverlayActive = config.Bind("StopMovementWhileSpectateOverlayActive", false);

        ChatBgOpacity = config.Bind("ChatBgOpacity", 0.2f);
        ChatInactivityOpacity = config.Bind("ChatInactivityOpacity", 0.35f);
        ChatInactivityTimer = config.Bind("ChatInactivityTimer", 30);
        StreamerMode = config.Bind("StreamerMode", StreamMode.None);

        DevNightskySkin = config.Bind("DevNightskySkin", false);
        EnableAchievementsOnline = config.Bind("EnableAchievementsOnline", false);
        AnniversaryCape = config.Bind("AnniversaryCape", true);
        MeadowCoins = config.Bind("MeadowCoins", 0);

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
            OpTab onlineTab = new OpTab(this, Translate("Gameplay"));



            Tabs = new OpTab[] { opTab, meadowTab, arenaTab, storyTab, lanTab, onlineTab };

            List<UIelement> meadowCheats;
            OpTextBox meadowCheatBox;
            OpSimpleButton cheatEmote;
            OpSimpleButton cheatSkin;
            OpSimpleButton cheatCharacter;
            OpSimpleButton cheatReset;
            float cheaty = 130f;
            OpTextBox chatBgOpacity;
            OpTextBox chatInactivityOpacity;
            OpTextBox chatInactivityTimer;
            OnlineGameplay = new UIelement[]
          {
            new OpLabel(10f, 550f, Translate("Gameplay"), bigText: true),
            new OpLabel(10f, 530f, Translate("Note: These inputs are not used in Meadow mode"), bigText: false),

            new OpLabel(10, 490f, Translate("Show usernames")),
            new OpKeyBinder(FriendsListKey, new Vector2(10f, 460f), new Vector2(150f, 30f)),

            new OpLabel(310f, 490f, Translate("Username Toggle"), bigText: false),
            new OpCheckBox(FriendViewClickToActivate, new Vector2(310f, 465f)),
            new OpLabel(340f, 475f, RWCustom.Custom.ReplaceLineDelimeters(Translate("Replace holding with toggling"))),

            new OpLabel(10, 400f, Translate("Key used for toggling spectator mode")),
            new OpKeyBinder(SpectatorKey, new Vector2(10f, 370f), new Vector2(150f, 30f)),

            new OpLabel(310, 445f, Translate("Stop Inputs While Spectating")),
            new OpCheckBox(StopMovementWhileSpectateOverlayActive, new Vector2(310f, 420)),

            new OpLabel(310, 400f, Translate("Pointing Key")),
            new OpKeyBinder(PointingKey, new Vector2(310f, 370f), new Vector2(150f, 30f)),
            
            new OpLabel(10f, 340, Translate($"Player Menu Scroll Speed for Spectate, Story menu, Arena results.  Default: ${ScrollSpeed.Value}"), bigText: false),
            new OpTextBox(ScrollSpeed, new Vector2(10, 310), 160f)
                {
                    accept = OpTextBox.Accept.Float
                },

            new OpLabel(10, 250f, Translate("Streamer Mode")),
            new OpComboBox2(StreamerMode, new Vector2(10f, 220f), 160f, OpResourceSelector.GetEnumNames(null, typeof(StreamMode)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = Menu.MenuColorEffect.rgbWhite },

            new OpLabel(10, 180f, Translate("Chat Log Toggle")),
            new OpKeyBinder(ChatLogKey, new Vector2(10f, 150), new Vector2(150f, 30f)),

            new OpLabel(210, 180f, Translate("Chat Talk Button")),
            new OpKeyBinder(ChatButtonKey, new Vector2(210f, 150), new Vector2(150f, 30f)),

            new OpLabel(410, 180, Translate("Chat Background Opacity")),
            chatBgOpacity = new OpTextBox(ChatBgOpacity, new Vector2(410f, 153f), 90),


            new OpLabel(10, 120f, Translate("Profanity Filter")),
            new OpCheckBox(ProfanityFilter, new Vector2(10f, 90f)),

            new OpLabel(210, 120f, Translate("Show Ping")),
            new OpCheckBox(ShowPing, new Vector2(210, 90f)),

            new OpLabel(10, 60f, Translate("Sound on Mention")),
            new OpCheckBox(ChatPing, new Vector2(10, 30f)),

            new OpLabel(410, 120f, Translate("Chat Inactivity Opacity")),
            chatInactivityOpacity = new OpTextBox(ChatInactivityOpacity, new Vector2(410f, 93f), 90),

            new OpLabel(410, 60f, Translate("Chat Inactivity Timer")),
            chatInactivityTimer = new OpTextBox(ChatInactivityTimer, new Vector2(410f, 33f), 90),

            new OpLabel(210, 60f, Translate("Chat Log On/Off")),
            new OpCheckBox(ChatLogOnOff, new Vector2(210f, 30f)),


          };
            onlineTab.AddItems(OnlineGameplay);

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
            OpComboBox2 music;
            OpLabel downpourWarning;
            OpLabel watcherWarning;

            OpSimpleButton editSyncRequiredModsButton;
            OpSimpleButton editBannedModsButton;

            OpLabel devOptions;
            GeneralUIArrPlayerOptions = new UIelement[]
            {
                new OpLabel(10f, 550f, Translate("General"), bigText: true),
                devOptions = new OpLabel(410f, 560f, Translate("Dev options")),
                new OpCheckBox(DevNightskySkin, new Vector2(410f, 535f)),
                new OpLabel(440f, 535f, Translate("Nightsky Skin")),


                new OpLabel(10f, 490f, RWCustom.Custom.ReplaceLineDelimeters(Translate("Control which mods are permitted on clients by editing the files below.<LINE>Instructions included within."))),
                editSyncRequiredModsButton = new OpSimpleButton(new Vector2(10f, 450f), new Vector2(150f, 30f), Translate("Edit High-Impact Mods")),
                editBannedModsButton = new OpSimpleButton(new Vector2(185f, 450f), new Vector2(150f, 30f), Translate("Edit Banned Mods")),


                new OpLabel(10, 420, Translate("Playtesting Gift")),
                new OpCheckBox(WearingCape, new Vector2(10, 390f)),

                new OpLabel(120, 420, Translate("Anniversary Gift")),
                new OpCheckBox(AnniversaryCape, new Vector2(120, 390f)),
                
                new OpLabel(10, 370, Translate("Introroll")),
                introroll = new OpComboBox2(PickedIntroRoll, new Vector2(10, 340f), 160f, OpResourceSelector.GetEnumNames(null, typeof(IntroRoll)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = Menu.MenuColorEffect.rgbWhite },
                downpourWarning = new OpLabel(introroll.pos.x + 170, 70, Translate("Downpour DLC is not activated, vanilla intro will be used instead")),
                watcherWarning = new OpLabel(introroll.pos.x + 170, 70, Translate("Watcher DLC is not activated, vanilla intro will be used instead")),

                new OpLabel(10, 250, Translate("Lobby Music")),
                music = new OpComboBox2(LobbyMusic, new Vector2(10, 220f), 160f, SongsItemList()) { colorEdge = Menu.MenuColorEffect.rgbWhite },
            };
            if (!MatchmakingManager.instances.Values.OfType<MatchmakingManager>().Any(x => x.IsDev(OnlineManager.mePlayer.id)))
            {
                GeneralUIArrPlayerOptions.Skip(GeneralUIArrPlayerOptions.IndexOf(devOptions)).Take(3).Do(e => e.Hidden = true);
            }

            introroll.OnValueChanged += (UIconfig config, string value, string oldValue) =>
            {
                if (value == "Downpour" && !ModManager.MSC)
                {
                    downpourWarning.Show();
                }
                else downpourWarning.Hide();
                if (value == "Watcher" && !ModManager.Watcher)
                {
                    watcherWarning.Show();
                }
                else watcherWarning.Hide();
            };
            if (!ModManager.MSC && introroll.value == "Downpour")
            {
                downpourWarning.Hidden = false;
                watcherWarning.Hidden = true;
            }
            else
            {
                downpourWarning.Hidden = ModManager.MSC;
            }


            if (!ModManager.Watcher && introroll.value == "Watcher")
            {
                watcherWarning.Hidden = false;
                downpourWarning.Hidden = true;
            }
            else
            {
                watcherWarning.Hidden = ModManager.Watcher;
            }

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

            OnlineStorySettings =
            [   new OpLabel(10f, 550f, Translate("Story"), bigText: true),

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
                },

                new OpCheckBox(StoryItemSteal, new Vector2(10, 260)),

                new OpLabel(40f, 260, RWCustom.Custom.ReplaceLineDelimeters(Translate("Steal items from other players in Story mode")))
                {
                    verticalAlignment = OpLabel.LabelVAlignment.Center
                },

                new OpLabel(new Vector2(40, 170), new(25, 25), Custom.ReplaceLineDelimeters(Translate("Gain achievements online")), FLabelAlignment.Left),
                new OpCheckBox(EnableAchievementsOnline, new Vector2(10, 170)),

            ];
            storyTab.AddItems(OnlineStorySettings);

            OpLabel arenaSpoilerLabel, slugpupHellBackgroundLabel;
            OpHoldButton arenaSpoilerButton;
            OpCheckBox slugpupHellBackgroundCheckbox;

            OnlineArenaSettings =
            [
                new OpLabel(10f, 550f, Translate("Arena"), bigText: true),
                new OpLabel(10f, 520, Custom.ReplaceLineDelimeters(Translate("Match settings have been relocated to the arena lobby menu.<LINE>The remaining options just enable easter eggs."))),
                arenaSpoilerLabel = new OpLabel(10f, 480, Translate("The following option may contain spoilers for Saint's campaign."), bigText: false)
                {
                    color = new Color(0.85f, 0.35f, 0.4f)
                },
                arenaSpoilerButton = new OpHoldButton(new Vector2(10f, 445f), new Vector2(110, 30), Translate("OKIE DOKIE"))
                {
                    colorEdge = new Color(0.85f, 0.35f, 0.4f),
                },
                slugpupHellBackgroundLabel = new OpLabel(10f, 480, Translate("Slugpup: Rubicon background in select menu"), bigText: false),
                slugpupHellBackgroundCheckbox = new OpCheckBox(SlugpupHellBackground, new Vector2(10f, 455)),
            ];
            UIelement[] arenaPotentialSpoilerSettings = [slugpupHellBackgroundLabel, slugpupHellBackgroundCheckbox];
            for (int i = 0; i < arenaPotentialSpoilerSettings.Length; i++) arenaPotentialSpoilerSettings[i].Hide();
            arenaTab.AddItems(OnlineArenaSettings);
            arenaSpoilerButton.OnPressDone += btn =>
            {
                OpTab.DestroyItems([arenaSpoilerButton, arenaSpoilerLabel]);
                for (int i = 0; i < arenaPotentialSpoilerSettings.Length; i++) arenaPotentialSpoilerSettings[i].Show();
            };

            OnlineLANSettings = new UIelement[7]
            {
                new OpLabel(10f, 550f, Translate("LAN"), bigText: true),
                new OpLabel(10f, 505, Translate("Username"), bigText: false),
                new OpTextBox(LanUserName, new Vector2(10f, 480), 160f)
                {
                    accept = OpTextBox.Accept.StringASCII
                },
                new OpLabel(10f, 455, Translate("UDP timeout (ms)"), bigText: false),
                new OpTextBox(UdpTimeout, new Vector2(10f, 420), 160f)
                {
                    accept = OpTextBox.Accept.Int
                },
                new OpLabel(10f, 395, Translate("UDP heartbeat (ms)"), bigText: false),
                new OpTextBox(UdpHeartbeat, new Vector2(10f, 370), 160f)
                {
                    accept = OpTextBox.Accept.Int
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

    private const string DefaultLobbyMusic = "Woodback"; // Happy One Year, Meadow
    public bool GetLobbyMusic(out string result)
    {
        if (LobbyMusic.Value == "default")
        {
            result = DefaultLobbyMusic;
            return true;
        }
        if (LobbyMusic.Value == "none")
        {
            result = null;
            return false;
        }
        if (GetSongNames().Contains(LobbyMusic.Value))
        {
            result = LobbyMusic.Value;
            return true;
        }
        result = null;
        return false;
    }

    private static List<ListItem> SongsItemList()
    {
        int i = -1;
        return
        [
            new ListItem
            {
                displayName = Translate("Default"),
                name = "default",
                value = i++
            },
            new ListItem
            {
                displayName = Translate("None"),
                name = "none",
                value = i++
            },
            .. GetSongNames().Select(songName => new ListItem
            {
                displayName = songName,
                name = songName,
                value = i++
            }),
        ];
    }

    private static List<string> GetSongNames()
    {
        string[] rawDirData = AssetManager.ListDirectory("Music/Songs");
        List<string> allOggFiles = new List<string>();

        for (int j = 0; j < rawDirData.Length; j++)
        {
            string fileName = rawDirData[j];

            if (fileName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
            {
                string[] nameParts = fileName.Split(Path.DirectorySeparatorChar);
                string cleanFileNameWithExtension = nameParts[nameParts.Length - 1];

                int extensionIndex = cleanFileNameWithExtension.LastIndexOf('.');

                string cleanFileName = cleanFileNameWithExtension.Substring(0, extensionIndex);
                allOggFiles.Add(cleanFileName);
            }
        }
        return allOggFiles;
    }
}
