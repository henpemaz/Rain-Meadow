using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Menu;
using UnityEngine;
using static RainMeadow.ButtonScroller;

namespace RainMeadow.UI.Components
{
    public class ArenaPlayerSmallBox : RectangularMenuObject, IPartOfButtonScroller
    {
        public static Vector2 DefaultSize => new(ArenaPlayerBox.DefaultSize.x, 40);
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public float Alpha { get; set; } = 1;
        public ArenaPlayerSmallBox(Menu.Menu menu, MenuObject owner, OnlinePlayer player, bool canKick, Vector2 pos, Vector2 size = default) : base(menu, owner, pos, size == default? DefaultSize : size)
        {
            profileIdentifier = player;
            rainbowColor = new(0, 1, 0.5f);
            baseSprites = [new FSprite("pixel"), new FSprite("pixel")];
            for (int i = 0; i < baseSprites.Length; i++)
            {
                baseSprites[i].anchorX = 0;
                baseSprites[i].anchorY = 0;
                baseSprites[i].scaleY = 2;
                Container.AddChild(baseSprites[i]);
            }
            float yPos = MiddleOfY(30);
            playerButton = new(menu, this, player.id.name, new(0, yPos), new(100, 30));
            playerButton.OnClick += (_) =>
            {
                profileIdentifier.id.OpenProfileLink();
            };
            slugcatButton = new(menu, this, null, null, new(120, 30), new(playerButton.pos.x + playerButton.size.x + 15, yPos));
            InitButtons(canKick);
            this.SafeAddSubobjects(playerButton, slugcatButton, colorKickButton);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            baseSprites.Do(x => x.RemoveFromContainer());
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
                ArenaPlayerBox.AddOrRemoveFromMute(profileIdentifier);
                (sender as SymbolButton)?.UpdateSymbol(ArenaPlayerBox.GetMuteSymbol(OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(profileIdentifier.id.name) == true));
                menu.PlaySound(OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(profileIdentifier.id.name) == true ? SoundID.MENU_Checkbox_Check : SoundID.MENU_Checkbox_Uncheck);
            }

        }
        public override void Update()
        {
            base.Update();
            rainbowColor.hue = ArenaPlayerBox.GetLerpedRainbowHue();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 pos = DrawPos(timeStacker), size = DrawSize(timeStacker);
            for (int i = 0; i < 2 && i < baseSprites.Length; i++)
            {
                baseSprites[i].scaleX = size.x;
                baseSprites[i].x = pos.x;
                baseSprites[i].y = pos.y + (size.y * i);
                baseSprites[i].color = MenuColorEffect.rgbVeryDarkGrey;
            }
            Color rainbowCol = ArenaPlayerBox.MyRainbowColor(rainbowColor, showRainbow);
            HSLColor baseCol = MyBaseColor(), lerpedBaseCol = Color.Lerp(baseCol.rgb, rainbowCol, rainbowCol.a).ToHSL();
            playerButton.rectColor = lerpedBaseCol;
            playerButton.labelColor = lerpedBaseCol;
            slugcatButton.rectColor = lerpedBaseCol;
            slugcatButton.labelColor = lerpedBaseCol;
        }
        public void InitButtons(bool canKick)
        {
            float yPosDefaultSymbol = MiddleOfY(24);
            string sprite = profileIdentifier.isMe ? "Meadow_Menu_ColorBucket" : canKick? "Menu_Symbol_Clear_All" : ArenaPlayerBox.GetMuteSymbol(OnlineManager.lobby?.gameMode?.mutedPlayers?.Contains(profileIdentifier.id.name) == true), 
                signal = profileIdentifier.isMe ? "Color_Slugcat" : canKick ? "KICKPLAYER" : "MUTEPLAYER";
            colorKickButton = new(menu, this, sprite, signal, new(slugcatButton.pos.x + slugcatButton.size.x + 15, yPosDefaultSymbol));
        }
        public HSLColor MyBaseColor()
        {
            return baseColor.GetValueOrDefault(Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey));
        }
        public float MiddleOfY(float sizeY)
        {
            return (size.y - sizeY) / 2;
        }
        public void SaveNewSlugcat(SlugcatStats.Name slugcat)
        {
            slugcatButton.slug = slugcat;
        }
        public bool showRainbow;
        public HSLColor? baseColor;
        public HSLColor rainbowColor;
        public FSprite[] baseSprites;
        public ScrollerButton playerButton;
        public StoryMenuSlugcatButton slugcatButton;
        public ScrollSymbolButton colorKickButton; //no kicking yourself pls
        public OnlinePlayer profileIdentifier;
    }
}
