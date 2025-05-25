using System;
using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using HarmonyLib;
using static RainMeadow.ButtonScroller;

namespace RainMeadow.UI.Components
{
    public class ArenaPlayerBox : RectangularMenuObject, IPartOfButtonScroller
    {
        public static float GetLerpedRainbowHue(float alternatingSpeed = 0.167f, float length = 1) //3sf 1/6
        {
            return Mathf.PingPong(Time.time * alternatingSpeed, length);
        }
        public static Color MyRainbowColor(HSLColor rainbowColor, bool showRainbow, float alpha = 0.5f)
        {
            Color color = rainbowColor.rgb;
            return new(color.r, color.g, color.b, showRainbow ? alpha : 0);
        }
        public static string GetMuteSymbol(bool isMuted)
        {
            float gen = UnityEngine.Random.Range(0, 100), type = 0;
            if (gen < 5) type = 1;
            return $"Meadow_Menu_MutePlayerChat{type}{(isMuted ? 0 : 1)}";
        }
        public static void AddOrRemoveFromMute(OnlinePlayer player)
        {
            if (OnlineManager.lobby?.gameMode == null) return;

            if (OnlineManager.lobby.gameMode.mutedPlayers.Contains(player.id.name))
                OnlineManager.lobby.gameMode.mutedPlayers.Remove(player.id.name);
            else OnlineManager.lobby.gameMode.mutedPlayers.Add(player.id.name);
        }
        public static Vector2 DefaultSize => new(290, 120);
        public float Alpha { get; set; } = 1;
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public ArenaPlayerBox(Menu.Menu menu, MenuObject owner, OnlinePlayer player, bool canKick, Vector2 pos, Vector2 size = default) : base(menu, owner, pos, size == default ? DefaultSize : size)
        {
            profileIdentifier = player;
            rainbowColor = new(0, 1, 0.5f);
            sprites = [new("pixel"), new("pixel"), new("Meadow_Menu_Ping")];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].anchorX = i < 2 ? 0 : 1;
                sprites[i].anchorY = 0;
                sprites[i].scaleY = i < 2 ? 2 : sprites[i].scaleY;
                Container.AddChild(sprites[i]);
            }
            pingLabel = new(Custom.GetFont(), "")
            {
                anchorX = 1,
                anchorY = 0,
            };
            Container.AddChild(pingLabel);
            lines = [];
            slugcatButton = new(menu, this, new(10, 10), new Vector2(16, 16), null, false);
            nameLabel = new(menu, this, player.id.name, new(slugcatButton.pos.x + slugcatButton.size.x + 10, slugcatButton.pos.y + slugcatButton.size.y - 5), new(80, 30), true);
            nameLabel.label.anchorY = 1f;
            selectingStatusLabel = new(menu, slugcatButton, "Selecting\nSlugcat", Vector2.zero, slugcatButton.size, false);
            InitButtons(canKick);
            this.SafeAddSubobjects(slugcatButton, nameLabel, selectingStatusLabel, colorInfoButton, infoKickButton);
            subObjects.AddRange(lines);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprites.Do(x => x.RemoveFromContainer());
            pingLabel.RemoveFromContainer();
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "KICKPLAYER")
            {
                BanHammer.BanUser(profileIdentifier);
                menu.PlaySound(SoundID.MENU_Remove_Level);
            }
            if (message == "MUTEPLAYER")
            {
                AddOrRemoveFromMute(profileIdentifier);
                (sender as SymbolButton)?.UpdateSymbol(GetMuteSymbol(OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(profileIdentifier.id.name) == true));
                menu.PlaySound(OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(profileIdentifier.id.name) == true ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
            }

        }
        public override void Update()
        {
            base.Update();
            rainbowColor.hue = GetLerpedRainbowHue();
            slugcatButton.portraitSecondaryLerpFactor = GetLerpedRainbowHue(0.75f);
            realPing = Math.Max(1, profileIdentifier.ping - 16);
            lastSelectingStatusLabelFade = selectingStatusLabelFade;
            selectingStatusLabelFade = isSelectingSlugcat ? Custom.LerpAndTick(selectingStatusLabelFade, 1f, 0.02f, 1f / 60f) : Custom.LerpAndTick(selectingStatusLabelFade, 0f, 0.12f, 0.1f);
            slugcatButton.isBlackPortrait = isSelectingSlugcat;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 size = DrawSize(timeStacker), pos = DrawPos(timeStacker);
            Color pingColor = GetPingColor(realPing);
            pingLabel.text = profileIdentifier.isMe ? "ME" : $"{realPing}ms";
            pingLabel.x = pos.x + size.x;
            pingLabel.y = pos.y + 7;
            pingLabel.color = pingColor;
            for (int i = 0; i < 3; i++)
            {
                if (i == 2)
                {
                    sprites[i].x = pingLabel.x - pingLabel.textRect.width;
                    sprites[i].y = pingLabel.y - 2.5f;
                    sprites[i].color = pingColor;
                    continue;
                }
                sprites[i].scaleX = size.x;
                sprites[i].x = pos.x;
                sprites[i].y = pos.y + (size.y * i); //first sprite is bottomLine, second sprite is topLine
                sprites[i].color = MenuColorEffect.rgbVeryDarkGrey;
            }
            lines.Do(x => x.lineConnector.alpha = 0.5f);
            selectingStatusLabel.label.alpha = Custom.SCurve(Mathf.Lerp(lastSelectingStatusLabelFade, selectingStatusLabelFade, timeStacker), 0.3f);

            lines.Do(x => x.lineConnector.color = MenuColorEffect.rgbDarkGrey);
            Color rainbow = MyRainbowColor(rainbowColor, showRainbow);
            HSLColor basecolor = MyBaseColor();
            nameLabel.label.color = Color.Lerp(basecolor.rgb, rainbow, rainbow.a);
            slugcatButton.secondaryColor = showRainbow ? rainbow : null;
        }
        public void InitButtons(bool canKick)
        {
            Vector2 basePos = new(nameLabel.pos.x + 10, slugcatButton.pos.y + 10);
            if (profileIdentifier.isMe)
            {
                colorInfoButton = new(menu, this, "Meadow_Menu_BigColorBucket", "Color_Slugcat", basePos, new(45, 45));
                infoKickButton = new(menu, this, "Menu_InfoI", "Info_Player", new(colorInfoButton.pos.x + colorInfoButton.size.x + 30, basePos.y + 21));
                infoKickButton.OnClick += (_) =>
                {
                    profileIdentifier.id.OpenProfileLink();
                };
                UiLineConnector connector = new(menu, colorInfoButton, infoKickButton, false)
                {
                    posOffset = new(0, (45 - infoKickButton.size.y) / 2)
                };
                connector.MoveLineSpriteBeforeNode(colorInfoButton.roundedRect.sprites[0]);
                lines.Add(connector);
            }
            else
            {
                colorInfoButton = new(menu, this, "Menu_InfoI", "Info_Player", new(basePos.x, basePos.y + 21));
                colorInfoButton.OnClick += (_) =>
                {
                    profileIdentifier.id.OpenProfileLink();
                };
                infoKickButton = new(menu, this, canKick? "Menu_Symbol_Clear_All" : GetMuteSymbol(OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(profileIdentifier.id.name) == true), canKick ? "KICKPLAYER" : "MUTEPLAYER", new(colorInfoButton.pos.x + colorInfoButton.size.x + 30, colorInfoButton.pos.y));
                UiLineConnector connector = new(menu, colorInfoButton, infoKickButton, false);
                connector.MoveLineSpriteBeforeNode(colorInfoButton.roundedRect.sprites[0]);
                lines.Add(connector);

            }
        }
        public HSLColor MyBaseColor()
        {
            return baseColor.GetValueOrDefault(Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey));
        }
        public Color GetPingColor(int ping)
        {
            if (profileIdentifier.isMe)
            {
                return MenuColorEffect.rgbDarkGrey;
            }
            return Color.Lerp((ping > 200 ? Color.red : ping > 100 ? Color.yellow : Color.green), MenuColorEffect.rgbVeryDarkGrey, 0.65f);
        }

        public float selectingStatusLabelFade = 0, lastSelectingStatusLabelFade = 0;
        public int realPing;
        public bool showRainbow, isSelectingSlugcat;
        public HSLColor? baseColor;
        public HSLColor rainbowColor;
        public FSprite[] sprites;
        public FLabel pingLabel;
        public MenuLabel selectingStatusLabel;
        public List<UiLineConnector> lines;
        public ScrollSymbolButton? infoKickButton;
        public ScrollSymbolButton colorInfoButton;
        public ProperlyAlignedMenuLabel nameLabel;
        public SlugcatColorableButton slugcatButton;
        public OnlinePlayer profileIdentifier;
    }
}
