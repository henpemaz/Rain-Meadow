using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Steamworks;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class StoryMenu : SmartMenu, SelectOneButton.SelectOneButtonOwner
    {
        private RainEffect rainEffect;

        private EventfulHoldButton startButton;
        private EventfulBigArrowButton prevButton;
        private EventfulBigArrowButton nextButton;
        private PlayerInfo[] players;

        private SlugcatSelectMenu ssm;
        private List<SlugcatSelectMenu.SlugcatPage> characterPages;
        private EventfulSelectOneButton[] playerButtons;

        private List<SlugcatStats.Name> playableCharacters;
        private Dictionary<CharacterSelection.Character, List<CharacterSelection.Skin>> characterSkins;

        public override MenuScene.SceneID GetScene => null;
        public StoryMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.StoryMenu)
        {
            RainMeadow.DebugMe();
            this.rainEffect = new RainEffect(this, this.pages[0]);
            this.pages[0].subObjects.Add(this.rainEffect);
            this.rainEffect.rainFade = 0.3f;
            this.characterPages = new List<SlugcatSelectMenu.SlugcatPage>();

            ssm = (SlugcatSelectMenu)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(SlugcatSelectMenu));
            ssm.container = container;
            ssm.slugcatPages = characterPages;
            ssm.ID = ProcessManager.ProcessID.MultiplayerMenu;
            ssm.cursorContainer = cursorContainer;
            ssm.manager = manager;
            ssm.pages = pages;

            playableCharacters = CharacterSelection.AllAvailableCharacters();
            characterSkins = new();

            // TODO: Hook original slugcat selection menu?


            for (int j = 0; j < this.playableCharacters.Count; j++)
            {

                this.characterPages.Add(new StoryCharacterSelectPage(this, ssm, 1 + j, this.playableCharacters[j]));
                if (characterPages[j].sceneOffset.y == 0) characterPages[0].sceneOffset = new Vector2(-10f, 100f);
                this.pages.Add(this.characterPages[j]);
                /*
                                var skins = CharacterSelection.AllAvailableSkins(this.playableCharacters[j]);
                                RainMeadow.Debug(skins);
                                RainMeadow.Debug(skins.Select(s => s.ToString()).Aggregate((a, b) => (a + b)));
                                characterSkins[playableCharacters[j]] = skins;*/
            }


            this.startButton = new EventfulHoldButton(this, this.pages[0], base.Translate("ENTER"), new Vector2(683f, 85f), 40f);
            this.startButton.OnClick += (_) => { StartGame(); };
            startButton.buttonBehav.greyedOut = !OnlineManager.lobby.isAvailable;

            this.pages[0].subObjects.Add(this.startButton);
            this.prevButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(345f, 50f), -1);
            this.prevButton.OnClick += (_) =>
            {
                ssm.quedSideInput = Math.Max(-3, ssm.quedSideInput - 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.prevButton);
            this.nextButton = new EventfulBigArrowButton(this, this.pages[0], new Vector2(985f, 50f), 1);
            this.nextButton.OnClick += (_) =>
            {
                ssm.quedSideInput = Math.Min(3, ssm.quedSideInput + 1);
                base.PlaySound(SoundID.MENU_Next_Slugcat);
            };
            this.pages[0].subObjects.Add(this.nextButton);
            this.mySoundLoopID = SoundID.MENU_Main_Menu_LOOP;

            this.pages[0].subObjects.Add(new MenuLabel(this, mainPage, this.Translate("LOBBY"), new Vector2(194, 553), new(110, 30), true));

            List<PlayerInfo> players = new List<PlayerInfo>();
            foreach (OnlinePlayer player in OnlineManager.players)
            {
                CSteamID playerId;
                if (player.id is LocalMatchmakingManager.LocalPlayerId)
                {
                    playerId = default;
                }
                else
                {
                    playerId = (player.id as SteamMatchmakingManager.SteamPlayerId).steamID;
                }
                players.Add(new PlayerInfo(playerId, player.id.name));
            }
            this.players = players.ToArray();

            colorpicker = new OpTinyColorPicker(this, new Vector2(800, 60), "FFFFFF"); // todo read stored
            var wrapper = new UIelementWrapper(this.tabWrapper, colorpicker);
            tabWrapper._tab.AddItems(colorpicker.colorPicker);
/*          colorpicker.colorPicker.wrapper = wrapper;
            colorpicker.colorPicker.Hide();
            colorpicker.OnValueChangedEvent += Colorpicker_OnValueChangedEvent;*/
            // todo update a preview of some sort for the resulting tinted color!

            var label = new MenuLabel(this, mainPage, this.Translate("Tint color"), new Vector2(845, 60), new(0, 30), false);
            label.label.alignment = FLabelAlignment.Left;
            this.pages[0].subObjects.Add(label);

            var slider = new SubtleSlider2(this, mainPage, "Tint amount", new Vector2(800, 30), new Vector2(100, 30));
            this.pages[0].subObjects.Add(slider);

            UpdateCharacterUI();
            MatchmakingManager.instance.OnPlayerListReceived += OnlineManager_OnPlayerListReceived;

            if (OnlineManager.lobby.isAvailable)
            {
                OnLobbyAvailable();
            }
            else
            {
                OnlineManager.lobby.OnLobbyAvailable += OnLobbyAvailable;
            }
        }

/*        private void Colorpicker_OnValueChangedEvent()
        {
            if (personaSettings != null) personaSettings.tint = colorpicker.valuecolor;
        }

        float tintAmount;
        public override void SliderSetValue(Slider slider, float f)
        {
            tintAmount = f;
            if (personaSettings != null) personaSettings.tintAmount = f;
        }
        public override float ValueOfSlider(Slider slider)
        {
            return tintAmount;
        }*/

        private void UpdateCharacterUI()
        {

            playerButtons = new EventfulSelectOneButton[players.Length];
            for (int i = 0; i < players.Length; i++)
            {
                var player = players[i];
                var btn = new EventfulSelectOneButton(this, mainPage, player.name, "playerButtons", new Vector2(194, 515) - i * new Vector2(0, 38), new(110, 30), playerButtons, i);
                mainPage.subObjects.Add(btn);
                playerButtons[i] = btn;
            }
        }

        public override void Update()
        {
            base.Update();
            if (this.rainEffect != null)
            {
                this.rainEffect.rainFade = Mathf.Min(0.3f, this.rainEffect.rainFade + 0.006f);
            }

            ssm.lastScroll = ssm.scroll;
            ssm.scroll = ssm.NextScroll;
            if (Mathf.Abs(ssm.lastScroll) > 0.5f && Mathf.Abs(ssm.scroll) <= 0.5f)
            {
                this.UpdateCharacterUI();
            }
            if (ssm.scroll == 0f && ssm.lastScroll == 0f)
            {
                if (ssm.quedSideInput != 0)
                {
                    var sign = (int)Mathf.Sign(ssm.quedSideInput);
                    ssm.slugcatPageIndex += sign;
                    ssm.slugcatPageIndex = (ssm.slugcatPageIndex + ssm.slugcatPages.Count) % ssm.slugcatPages.Count;
                    /*                    skinIndex = Mathf.Min(skinIndex, characterSkins[playableCharacters[ssm.slugcatPageIndex]].Count);
                                        if (personaSettings != null) personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][skinIndex];*/
                    ssm.scroll = -sign;
                    ssm.lastScroll = -sign;
                    ssm.quedSideInput -= sign;
                    return;
                }
            }
        }

        private void OnLobbyAvailable()
        {
            startButton.buttonBehav.greyedOut = false;
            // BindSettings(); //Null Ref at the moment, probably Emotes HUD

        }

/*        private void BindSettings()
        {
            this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;
            //personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][skinIndex];
            personaSettings.tint = colorpicker.valuecolor;
            personaSettings.tintAmount = this.tintAmount;

        }*/

    private void StartGame()
    {
        RainMeadow.DebugMe();
        if (OnlineManager.lobby == null || !OnlineManager.lobby.isActive) return;
        manager.arenaSitting = null;
        manager.rainWorld.progression.ClearOutSaveStateFromMemory();
        manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.New;
        manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
    }

    public override void ShutDownProcess()
    {
        RainMeadow.DebugMe();
        if (OnlineManager.lobby != null) OnlineManager.lobby.OnLobbyAvailable -= OnLobbyAvailable;
        if (manager.upcomingProcess != ProcessManager.ProcessID.Game)
        {
            MatchmakingManager.instance.LeaveLobby();
        }
        base.ShutDownProcess();
    }

    int skinIndex;
    //private StoryAvatarSettings personaSettings;
    private OpTinyColorPicker colorpicker;

    public int GetCurrentlySelectedOfSeries(string series) // SelectOneButton.SelectOneButtonOwner
    {
        return skinIndex;
    }

    public void SetCurrentlySelectedOfSeries(string series, int to) // SelectOneButton.SelectOneButtonOwner
    {
        skinIndex = to;
        /*if (personaSettings != null) personaSettings.skin = characterSkins[playableCharacters[ssm.slugcatPageIndex]][to];*/
    }

    private void OnlineManager_OnPlayerListReceived(PlayerInfo[] players)
    {
        this.players = players;
        UpdateCharacterUI();
    }


}
}
