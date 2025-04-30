using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using HarmonyLib;

namespace RainMeadow.UI.Components
{
    public class ArenaPlayerBox : RectangularMenuObject, ButtonScroller.IPartOfButtonScroller
    {
        public static Vector2 DefaultSize => new(300, 120);
        public float Alpha { get => alpha; set => alpha = value; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public ArenaPlayerBox(Menu.Menu menu, MenuObject owner, string player, Vector2 pos, Vector2 size = default) : base(menu, owner, pos, size == default? DefaultSize : size)
        {
            baseSprites = [];
            lines = [];
            InitBaseSprites();
            slugcatButton = new(menu, this, new(10, 10), "", "MultiplayerPortrait01");
            slugcatButton.size += new Vector2(16, 16);
            nameLabel = new(menu, this, player, new(slugcatButton.pos.x + slugcatButton.size.x + 10, slugcatButton.pos.y + slugcatButton.size.y - 5), new(80, 30), true);
            nameLabel.label.anchorY = 1f;
            Vector2 basePos = new(nameLabel.pos.x + 10, slugcatButton.pos.y + 10);
            if (player == "test")
            {
                colorButton = new(menu, this, "Kill_Slugcat", "Color_Slugcat", basePos, new(45, 45));
                subObjects.Add(colorButton); //will be seperated
            }
            infoButton = new(menu, this, "Menu_InfoI", "Info_Player", new(colorButton != null ? colorButton.pos.x + colorButton.size.x + 30 : basePos.x, basePos.y));
            infoButton.pos += new Vector2(0, 45 - infoButton.size.y);
            subObjects.AddRange([slugcatButton, nameLabel, infoButton]);
            kickButton = new(menu, this, "Menu_Symbol_Clear_All", "KICKPLAYER", infoButton.pos + new Vector2(infoButton.size.x + 30, 0));
            subObjects.Add(kickButton);
            InitLines();
            subObjects.AddRange(lines);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            baseSprites.Do(x => x.RemoveFromContainer());
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            HSLColor basecolor = MyBaseColor();
            Vector2 size = DrawSize(timeStacker), pos = DrawPos(timeStacker);
            for (int i = 0; i < 2; i++)
            {
                baseSprites[i].scaleX = size.x;
                baseSprites[i].x = pos.x;
                baseSprites[i].y = pos.y + (size.y * i); //first sprite is bottomLine, second sprite is topLine
                baseSprites[i].color = MenuColorEffect.rgbVeryDarkGrey;
            }
            nameLabel.label.color = basecolor.rgb;
            slugcatButton.rectColor = basecolor;
            lines.Do(x => x.lineConnector.color = MenuColorEffect.rgbDarkGrey);
        }
        public void UpdateAlpha(float alpha)
        {
            nameLabel.label.alpha = alpha;
            slugcatButton.Alpha = alpha;
            infoButton.Alpha = alpha;
            if (colorButton != null)
            {
                colorButton.Alpha = alpha;
            }
           if (kickButton != null)
            {
                kickButton.Alpha = alpha;
            }
            lines.Do(x => x.lineConnector.alpha = alpha / 2);
            baseSprites.Do(x => x.alpha = alpha);
        }
        public void InitBaseSprites()
        {
            for (int i = 0; i < 2; i++)
            {
                FSprite sprite = new("pixel")
                {
                    anchorX = 0,
                    anchorY = 0,
                };
                sprite.scaleY = 2;
                baseSprites.Add(sprite);
                Container.AddChild(sprite);
            }
        }
        public void InitLines()
        {
            if (colorButton != null)
            {
                UiLineConnector connector = new(menu, colorButton, infoButton, false)
                {
                    posOffset = new(0, (45 - infoButton.size.y) / 2)
                };
                connector.MoveLineSpriteBeforeNode(colorButton.roundedRect.sprites[0]);
                lines.Add(connector);
            }
            if (kickButton != null)
            {
                UiLineConnector connector = new(menu, infoButton, kickButton, false);
                connector.MoveLineSpriteBeforeNode(infoButton.roundedRect.sprites[0]);
                lines.Add(connector);
            }
        }
        public HSLColor MyBaseColor()
        {
            return baseColor.GetValueOrDefault(Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey));
        }
        public void SaveNewSlugcat(SlugcatStats.Name slugcat, int colorIndex)
        {
            if (this.slugcat == slugcat && this.colorIndex != colorIndex)
            {
                return;
            }
            this.slugcat = slugcat;
            this.colorIndex = colorIndex;

        }

        public float alpha;
        public int colorIndex;
        public HSLColor? baseColor;
        public SlugcatStats.Name slugcat;
        public List<UiLineConnector> lines;
        public List<FSprite> baseSprites;
        public ScrollSymbolButton infoButton;
        public ScrollSymbolButton? colorButton, kickButton;
        public ProperlyAlignedMenuLabel nameLabel;
        public IllustrationButton slugcatButton;
        //public OnlinePlayer profileIdentifier;
    }
}
