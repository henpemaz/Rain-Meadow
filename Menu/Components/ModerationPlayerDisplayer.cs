using HarmonyLib;
using Menu;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using static RainMeadow.ButtonScroller;
namespace RainMeadow.UI.Components
{
    public class ModerationPlayerDisplayer : ButtonDisplayer
    {
        public bool isBanList;
        public ModerationPlayerDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, List<SteamPlayerRep> players, Func<ModerationPlayerDisplayer, bool, SteamPlayerRep, Vector2, IPartOfButtonScroller> getPlayerButton, int numOfLargeButtonsToView, float xListSize, (float, float) largeButtonHeightSpacing, (float, float) smallButtonHeightSpacing, float scrollSliderSizeYOffset = -40, bool isBanList = false) : base(menu, owner, pos, numOfLargeButtonsToView, xListSize, largeButtonHeightSpacing)
        {
            this.isBanList = isBanList;
            this.getPlayerButton = getPlayerButton;
            this.players = players;
            this.largeButtonHeightSpacing = largeButtonHeightSpacing;
            this.smallButtonHeightSpacing = smallButtonHeightSpacing;
            refreshDisplayButtons = PopulatePlayerDisplays;
            UpdatePlayerList(this.players);
        }
        public void UpdatePlayerList(List<SteamPlayerRep> lobbyOnlinePlayers)
        {
            players = lobbyOnlinePlayers;
            CallForRefresh();
        }
        public IPartOfButtonScroller[] PopulatePlayerDisplays(ButtonDisplayer buttonDisplayer, bool IsCurrentlyLargeDisplay)
        {
            buttonHeight = largeButtonHeightSpacing.Item1;
            buttonSpacing = largeButtonHeightSpacing.Item2;
            List<IPartOfButtonScroller> scrollButtons = [];
            for (int i = 0; i < players?.Count; i++)
            {
                IPartOfButtonScroller? scrollButton = getPlayerButton?.Invoke(this, isBanList, players[i], GetIdealPosWithScrollForButton(scrollButtons.Count));
                if (scrollButton == null)
                {
                    RainMeadow.Debug("Player button gotten was null!");
                    continue;
                }
                scrollButtons.Add(scrollButton);
            }
            return [.. scrollButtons];
        }
        public override string DescriptionOfDisplayButton() => menu.Translate(isCurrentlyLargeDisplay ? "Showing players in thumbnail view" : "Showing players in list view");

        public (float, float) largeButtonHeightSpacing, smallButtonHeightSpacing;
        public List<SteamPlayerRep> players;
        public Func<ModerationPlayerDisplayer, bool, SteamPlayerRep, Vector2, IPartOfButtonScroller> getPlayerButton;
        public SideButton inviteFriends;
    }

    public class ModerationPlayerBox : RectangularMenuObject, IPartOfButtonScroller
    {
        public SteamPlayerRep profileIdentifier;
        public FSprite[] sprites;
        public MenuLabel textOverlayLabel;
        public List<UiLineConnector> lines;
        public ScrollSymbolButton banButton;
        public ProperlyAlignedMenuLabel nameLabel;
        public SlugcatColorableButton slugcatButton;

        public HSLColor? baseColor;
        public float textOverlayFade = 0, lastTextOverlayFade = 0, desiredPortraitSecondaryLerpFactor = 0;
        public bool enabledTextOverlay;
        public ModerationPlayerBox(Menu.Menu menu, MenuObject owner, SteamPlayerRep player, bool banned, Vector2 pos, Vector2 size = default) : base(menu, owner, pos, size == default ? DefaultSize : size)
        {
            profileIdentifier = player;
            sprites = [new("pixel"), new("pixel")];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].anchorX = i < 2 ? 0 : 1;
                sprites[i].anchorY = 0;
                sprites[i].scaleY = i < 2 ? 2 : sprites[i].scaleY;
                Container.AddChild(sprites[i]);
            }
            lines = [];
            slugcatButton = new(menu, this, new(10, 10), new Vector2(100, 100), null, false, signal: "");
            nameLabel = new(menu, this, player.Name, new(slugcatButton.pos.x + slugcatButton.size.x + 10, slugcatButton.pos.y + slugcatButton.size.y - 5), new(80, 30), true);
            nameLabel.label.anchorY = 1f;
            textOverlayLabel = new(menu, slugcatButton, "", Vector2.zero, slugcatButton.size, false);
            InitButtons(banned);
            this.SafeAddSubobjects(slugcatButton, nameLabel, textOverlayLabel, banButton);
            subObjects.AddRange(lines);
        }

        public void InitButtons(bool banned)
        {
            Vector2 basePos = new(nameLabel.pos.x + 10, slugcatButton.pos.y + 10);
            banButton = new(menu, this, banned ? "Meadow_Menu_MutePlayerChat11" : "Meadow_Menu_MutePlayerChat10", banned ? "UNBAN_PLAYER" : "BAN_PLAYER", basePos, new(45, 45));
            banButton.OnClick += (_) =>
            {
                if (banned)
                {
                    BanHammer.UnpermabanUser(profileIdentifier);
                }
                else
                {
                    BanHammer.PermaBanUser(profileIdentifier);
                }
            };
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "BAN_PLAYER")
            {
                
            }
        }

        public override void Update()
        {
            base.Update();
            lastTextOverlayFade = textOverlayFade;
            textOverlayFade = enabledTextOverlay ? Custom.LerpAndTick(textOverlayFade, 1f, 0.02f, 1f / 60f) : Custom.LerpAndTick(textOverlayFade, 0f, 0.12f, 0.1f);
            slugcatButton.isBlackPortrait = enabledTextOverlay;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 size = DrawSize(timeStacker), pos = DrawPos(timeStacker);

            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].scaleX = size.x;
                sprites[i].x = pos.x;
                sprites[i].y = pos.y + (size.y * i); //first sprite is bottomLine, second sprite is topLine
                sprites[i].color = MenuColorEffect.rgbVeryDarkGrey;
            }
            lines.Do(x => x.lineConnector.alpha = 0.5f);
            textOverlayLabel.label.alpha = RWCustom.Custom.SCurve(Mathf.Lerp(lastTextOverlayFade, textOverlayFade, timeStacker), 0.3f);

            lines.Do(x => x.lineConnector.color = MenuColorEffect.rgbDarkGrey);
            HSLColor basecolor = MyBaseColor();
            nameLabel.label.color = basecolor.rgb;
        }

        public HSLColor MyBaseColor()
        {
            return baseColor.GetValueOrDefault(Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey));
        }

        public static Vector2 DefaultSize => new(290, 120);
        public float Alpha { get; set; } = 1;
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
    }
}
