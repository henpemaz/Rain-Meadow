// HACK
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using System;
using System.Diagnostics;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow;

public class LobbyCreateMenu : SmartMenu
{
    private OpComboBox2 visibilityDropDown;
    private OpTextBox lobbyLimitNumberTextBox;
    private int maxPlayerCount;
    private SimplerButton createButton;
    private OpComboBox2 modeDropDown;
    private ProperlyAlignedMenuLabel modeDescriptionLabel;
    private OpTextBox passwordInputBox;
    private OpCheckBox enablePasswordCheckbox;
    private MenuDialogBox? popupDialog;
    private SimplerButton editSyncRequiredModsButton;
    private SimplerButton editBannedModsButton;
    public override MenuScene.SceneID GetScene => ModManager.MMF ? manager.rainWorld.options.subBackground : MenuScene.SceneID.Landscape_SU;

    public LobbyCreateMenu(ProcessManager manager) : base(manager, RainMeadow.Ext_ProcessID.LobbyCreateMenu)
    {
        // title at the top
        this.scene.AddIllustration(new MenuIllustration(this, this.scene, "illustrations/rainmeadowtitle", Utils.GetMeadowTitleFileName(true), new Vector2(-2.99f, 265.01f), true, false));
        this.scene.AddIllustration(new MenuIllustration(this, this.scene, "illustrations/rainmeadowtitle", Utils.GetMeadowTitleFileName(false), new Vector2(-2.99f, 265.01f), true, false));
        this.scene.flatIllustrations[this.scene.flatIllustrations.Count - 1].sprite.shader = this.manager.rainWorld.Shaders["MenuText"];

        // creation button in bottom right
        createButton = new SimplerButton(this, mainPage, Translate("CREATE!"), new Vector2(1056f, 50f), new Vector2(110f, 30f));
        createButton.OnClick += CreateLobby;
        mainPage.subObjects.Add(createButton);

        // game mode selection in top center
        var where = new Vector2(500f, 550);
        var modeLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Mode:"), where, new Vector2(200, 20f), false);
        mainPage.subObjects.Add(modeLabel);
        where.x += 80;
        modeDropDown = new OpComboBox2(new Configurable<OnlineGameMode.OnlineGameModeType>(OnlineGameMode.OnlineGameModeType.Meadow), where, 160, OpResourceSelector.GetEnumNames(null, typeof(OnlineGameMode.OnlineGameModeType)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
        modeDropDown.OnChanged += UpdateModeDescription;
        new UIelementWrapper(this.tabWrapper, modeDropDown);
        where.x -= 80;
        where.y -= 35;
        modeDescriptionLabel = new ProperlyAlignedMenuLabel(this, mainPage, "", where, new Vector2(0, 20f), false);
        mainPage.subObjects.Add(modeDescriptionLabel);

        // visibility setting in upper center
        where.y -= 45;
        var visibilityLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Visibility:"), where, new Vector2(200, 20f), false);
        mainPage.subObjects.Add(visibilityLabel);
        where.x += 80;
        visibilityDropDown = new OpComboBox2(new Configurable<MatchmakingManager.LobbyVisibility>(MatchmakingManager.LobbyVisibility.Public), where, 160, OpResourceSelector.GetEnumNames(null, typeof(MatchmakingManager.LobbyVisibility)).Select(li => { li.displayName = Translate(li.displayName); return li; }).ToList()) { colorEdge = MenuColorEffect.rgbWhite };
        new UIelementWrapper(this.tabWrapper, visibilityDropDown);

        // password setting in lower center
        where.x -= 80;
        where.y -= 45;
        enablePasswordCheckbox = new OpCheckBox(new Configurable<bool>(false), where); //new Vector2(600f, 400f));//where);
        new UIelementWrapper(tabWrapper, enablePasswordCheckbox);
        where.x += 160;
        passwordInputBox = new OpTextBox(new Configurable<string>(""), where, 160f)
        {
            accept = OpTextBox.Accept.StringASCII,
            allowSpace = true,
            description = Utils.Translate("Lobby Password"),
        };
        passwordInputBox.PosX = modeDropDown.pos.x;
        passwordInputBox.label.text = Utils.Translate("Password");
        new UIelementWrapper(this.tabWrapper, passwordInputBox);

        // lobby limit setting in bottom center
        where.x -= 160;
        where.y -= 45;
        var limitNumberLabel = new ProperlyAlignedMenuLabel(this, mainPage, Translate("Player max:"), where, new Vector2(400, 20f), false);
        mainPage.subObjects.Add(limitNumberLabel);
        where.x += 80;
        where.y -= 5;
        lobbyLimitNumberTextBox = new OpTextBox(new Configurable<int>(maxPlayerCount = 4), where, 160f)
        {
            accept = OpTextBox.Accept.Int,
            maxLength = 2,
            description = Utils.Translate("Maximum number of players that can be in the lobby (up to 32)"),
        };
        new UIelementWrapper(this.tabWrapper, lobbyLimitNumberTextBox);
        where.y += 5;

        // display version
        MenuLabel versionLabel = new MenuLabel(this, pages[0], $"{Utils.Translate("Rain Meadow Version:")} {RainMeadow.MeadowVersionStr}", new Vector2((1336f - manager.rainWorld.screenSize.x) / 2f + 20f, manager.rainWorld.screenSize.y - 768f), new Vector2(200f, 20f), false, null);
        versionLabel.size = new Vector2(versionLabel.label.textRect.width, versionLabel.size.y);
        mainPage.subObjects.Add(versionLabel);

        if (backObject is SimplerButton backButton) backButton.menuLabel.text = Utils.Translate("CANCEL");

        UpdateModeDescription();

        if (manager.musicPlayer != null)
        {
            manager.musicPlayer.MenuRequestsSong("Establish", 1, 0);
            if (manager.musicPlayer.nextSong == null)
            {
                manager.musicPlayer.song.Loop = true; //well if you want that you gotta also make it disable when out of the menu hehe
            }
            else
            {
                manager.musicPlayer.nextSong.Loop = true; //well if you want that you gotta also make it disable when out of the menu hehe
            }
        }

        var editSyncBannedLabel = new ProperlyAlignedMenuLabel(this, mainPage, Custom.ReplaceLineDelimeters(Translate("Control which mods are permitted on clients by editing the files below.<LINE>Instructions included within.")), new Vector2(500f, 325f), new Vector2(200, 20f), false);
        mainPage.subObjects.Add(editSyncBannedLabel);

        editSyncRequiredModsButton = new SimplerButton(this, mainPage, Translate("Edit High-Impact Mods"), new Vector2(500f, 275f), new Vector2(150f, 30f));
        editSyncRequiredModsButton.OnClick += _ =>
        {
            RainMeadowModManager.GetRequiredMods();
            Process.Start(AssetManager.ResolveFilePath(RainMeadowModManager.SyncRequiredModsFileName));
        };
        mainPage.subObjects.Add(editSyncRequiredModsButton);

        editBannedModsButton = new SimplerButton(this, mainPage, Translate("Edit Banned Mods"), new Vector2(675f, 275f), new Vector2(150f, 30f));
        editBannedModsButton.OnClick += _ =>
        {
            RainMeadowModManager.GetBannedMods();
            Process.Start(AssetManager.ResolveFilePath(RainMeadowModManager.BannedOnlineModsFileName));
        };
        mainPage.subObjects.Add(editBannedModsButton);

    }

    private void UpdateModeDescription()
    {
        modeDescriptionLabel.text = Custom.ReplaceLineDelimeters(Translate(OnlineGameMode.OnlineGameModeType.descriptions[new OnlineGameMode.OnlineGameModeType(modeDropDown.value)]));
    }

    private void CreateLobby(SimplerButton obj)
    {
        if (ModManager.JollyCoop)
        {
            ShowErrorDialog("Please disable JollyCoop before playing Online");
            return;
        }

        ShowLoadingDialog("Creating lobby...");
        ApplyLobbyLimit();
        RainMeadow.Debug($"Creating a lobby with a max player limit of {maxPlayerCount}");
        RequestLobbyCreate();
    }

    private void ApplyLobbyLimit()
    {
        maxPlayerCount = lobbyLimitNumberTextBox.valueInt;
        if (lobbyLimitNumberTextBox.valueInt > 32) lobbyLimitNumberTextBox.valueInt = 32;
        if (lobbyLimitNumberTextBox.valueInt < 2) lobbyLimitNumberTextBox.valueInt = 2;
    }

    private void RequestLobbyCreate()
    {
        RainMeadow.DebugMe();
        Enum.TryParse<MatchmakingManager.LobbyVisibility>(visibilityDropDown.value, out var value);
        MatchmakingManager.currentInstance.CreateLobby(value, modeDropDown.value, enablePasswordCheckbox.GetValueBool() ? passwordInputBox.value : null, maxPlayerCount);
    }

    private void ShowLoadingDialog(string text)
    {
        if (popupDialog != null) HideDialog();

        text = Utils.Translate(text);

        popupDialog = new DialogBoxAsyncWait(this, mainPage, text, new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
        mainPage.subObjects.Add(popupDialog);
    }

    private void ShowErrorDialog(string error)
    {
        if (popupDialog != null) HideDialog();

        popupDialog = new DialogBoxNotify(this, mainPage, error, "HIDE_DIALOG", new Vector2(manager.rainWorld.options.ScreenSize.x / 2f - 240f + (1366f - manager.rainWorld.options.ScreenSize.x) / 2f, 224f), new Vector2(480f, 320f));
        mainPage.subObjects.Add(popupDialog);
    }

    private void HideDialog()
    {
        if (popupDialog == null) return;

        mainPage.RemoveSubObject(popupDialog);
        popupDialog.RemoveSprites();
        popupDialog = null;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message == "HIDE_DIALOG") HideDialog();
    }
}
