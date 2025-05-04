using System;
using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using HarmonyLib;
using System.Diagnostics;

namespace RainMeadow.UI.Components
{
    public class ArenaPlayerBox : RectangularMenuObject, ButtonScroller.IPartOfButtonScroller
    {
        public static Vector2 DefaultSize => new(290, 120);
        public float Alpha { get => alpha; set => alpha = value; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public ArenaPlayerBox(Menu.Menu menu, MenuObject owner, OnlinePlayer player, bool canKick, Vector2 pos, Vector2 size = default) : base(menu, owner, pos, size == default? DefaultSize : size)
        {
            profileIdentifier = player;
            sprites = [new("pixel"), new("pixel"), new("Meadow_Menu_Ping")];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].anchorX = i < 2? 0 : 1;
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
            slugcatButton = new(menu, this, new(10, 10), "", "MultiplayerPortrait01");
            slugcatButton.size += new Vector2(16, 16);
            nameLabel = new(menu, this, player.id.name, new(slugcatButton.pos.x + slugcatButton.size.x + 10, slugcatButton.pos.y + slugcatButton.size.y - 5), new(80, 30), true);
            nameLabel.label.anchorY = 1f;
            InitButtons(canKick);
            this.SafeAddSubobjects(slugcatButton, nameLabel, colorInfoButton, infoKickButton);
            subObjects.AddRange(lines);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprites.Do(x => x.RemoveFromContainer());
            pingLabel.RemoveFromContainer();
        }
        public override void Update()
        {
            base.Update();
            realPing = Math.Max(1, profileIdentifier.ping - 16);
          
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            HSLColor basecolor = MyBaseColor();
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
            nameLabel.label.color = basecolor.rgb;
            slugcatButton.rectColor = basecolor;
            lines.Do(x => x.lineConnector.color = MenuColorEffect.rgbDarkGrey);
        }
        public void UpdateAlpha(float alpha)
        {
            nameLabel.label.alpha = alpha;
            slugcatButton.Alpha = alpha;
            colorInfoButton.Alpha = alpha;
            if (infoKickButton != null)
            {
                infoKickButton.Alpha = alpha;
            }
            lines.Do(x => x.lineConnector.alpha = alpha / 2);
            sprites.Do(x => x.alpha = alpha);
            pingLabel.alpha = alpha;
        }
        public void InitButtons(bool canKick)
        {
            Vector2 basePos = new(nameLabel.pos.x + 10, slugcatButton.pos.y + 10);
            if (profileIdentifier.isMe)
            {
                colorInfoButton = new(menu, this, "Kill_Slugcat", "Color_Slugcat", basePos, new(45, 45));
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
                if (canKick)
                {
                    infoKickButton = new(menu, this, "Menu_Symbol_Clear_All", "KICKPLAYER", new(colorInfoButton.pos.x + colorInfoButton.size.x + 30, colorInfoButton.pos.y));
                    infoKickButton.OnClick += (_) =>
                    {
                        BanHammer.BanUser(profileIdentifier);
                    };
                    UiLineConnector connector = new(menu, colorInfoButton, infoKickButton, false);
                    connector.MoveLineSpriteBeforeNode(colorInfoButton.roundedRect.sprites[0]);
                    lines.Add(connector);
                }
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
        public void SaveNewSlugcat(SlugcatStats.Name slugcat, int colorIndex)
        {
            if (this.slugcat == slugcat && this.colorIndex == colorIndex)
            {
                return;
            }
            this.slugcat = slugcat;
            this.colorIndex = colorIndex;

        }
        public float alpha;
        public int colorIndex, realPing;
        public HSLColor? baseColor;
        public FSprite[] sprites;
        public FLabel pingLabel;
        public List<UiLineConnector> lines;
        public ScrollSymbolButton? infoKickButton;
        public ScrollSymbolButton colorInfoButton;
        public ProperlyAlignedMenuLabel nameLabel;
        public IllustrationButton slugcatButton;
        public SlugcatStats.Name? slugcat;
        public OnlinePlayer profileIdentifier;
    }
}
