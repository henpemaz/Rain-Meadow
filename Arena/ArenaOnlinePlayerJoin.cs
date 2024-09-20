using System;
using RainMeadow;
using RainMeadow.GameModes;
using Rewired;
using RWCustom;
using UnityEngine;

namespace Menu
{
    public class ArenaOnlinePlayerJoinButton : ButtonTemplate
    {
        public MenuLabel menuLabel;

        public RoundedRect roundedRect;

        public RoundedRect selectRect;

        public bool joystickAvailable;

        public Joystick joystickPressed;

        public int assignedJoystick;

        public int index;

        public MenuIllustration portrait;

        public float labelFade;

        public float lastLabelFade;

        public int labelFadeCounter;

        public float portraitBlack = 1f;

        public float lastPortraitBlack = 1f;

        public bool lastInput;

        public bool readyForCombat;

        public ArenaCompetitiveGameMode arena;

        private ArenaLobbyMenu arenaMenu;

        public MenuIllustration joinButtonImage;


        public event Action<ArenaOnlinePlayerJoinButton> OnClick;
        public override void Clicked() { base.Clicked(); OnClick?.Invoke(this); }
        public override Color MyColor(float timeStacker)
        {
            if (buttonBehav.greyedOut)
            {
                return HSLColor.Lerp(Menu.MenuColor(Menu.MenuColors.DarkGrey), Menu.MenuColor(Menu.MenuColors.Black), black).rgb;
            }

            float a = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
            a = Mathf.Max(a, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
            HSLColor from = HSLColor.Lerp(HSLColor.Lerp(Menu.MenuColor(Menu.MenuColors.MediumGrey), Menu.MenuColor(Menu.MenuColors.DarkGrey), Mathf.Lerp(lastPortraitBlack, portraitBlack, timeStacker)), Menu.MenuColor(Menu.MenuColors.White), a);
            return HSLColor.Lerp(from, Menu.MenuColor(Menu.MenuColors.Black), black).rgb;
        }

        public ArenaOnlinePlayerJoinButton(Menu menu, MenuObject owner, Vector2 pos, int index)
            : base(menu, owner, pos, new Vector2(100f, 100f))
        {
            this.index = index;
            roundedRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, filled: true);
            subObjects.Add(roundedRect);
            selectRect = new RoundedRect(menu, this, new Vector2(0f, 0f), size, filled: false);
            subObjects.Add(selectRect);
            portrait = new MenuIllustration(menu, this, "", "MultiplayerPortrait" + index + "1", size / 2f, crispPixels: true, anchorCenter: true);
            subObjects.Add(portrait);
            string text = menu.Translate("");
            readyForCombat = false;

            float num = 0f;
            menuLabel = new MenuLabel(menu, this, menu.Translate("PLAYER") + (InGameTranslator.LanguageID.UsesSpaces(menu.CurrLang) ? " " : "") + (index + 1) + "\r\n" + text, new Vector2(0.01f, 0.1f + num), size, bigText: false);
            subObjects.Add(menuLabel);

        }

        public override void Update()
        {
            base.Update();
            buttonBehav.Update();
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
            roundedRect.addSize = new Vector2(10f, 6f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI)) * (buttonBehav.clicked ? 0f : 1f);
            selectRect.addSize = new Vector2(2f, -2f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * (float)Math.PI)) * (buttonBehav.clicked ? 0f : 1f);
            lastLabelFade = labelFade;
            labelFade = Custom.LerpAndTick(labelFade, 0f, 0.12f, 0.1f);
            labelFadeCounter = ((labelFade == 0f) ? 40 : 0);
            lastPortraitBlack = portraitBlack;
            portraitBlack = Custom.LerpAndTick(portraitBlack, readyForCombat ? 0 : 1, 0.06f, 0.05f); // Set to 1 to grey out

        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            menuLabel.label.alpha = Custom.SCurve(Mathf.Lerp(lastLabelFade, labelFade, timeStacker), 0.3f);
            Color color = Color.Lerp(Menu.MenuRGB(Menu.MenuColors.Black), Menu.MenuRGB(Menu.MenuColors.White), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
            for (int i = 0; i < 9; i++)
            {
                roundedRect.sprites[i].color = color;
            }

            float num = 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * (float)Math.PI * 2f);
            num *= buttonBehav.sizeBump;
            for (int j = 0; j < 8; j++)
            {
                selectRect.sprites[j].color = MyColor(timeStacker);
                selectRect.sprites[j].alpha = num;
            }

            menuLabel.label.color = Color.Lerp(PlayerGraphics.DefaultSlugcatColor(((OnlineManager.lobby.clientSettings[OnlineManager.mePlayer] as ArenaClientSettings).playingAs)), MyColor(timeStacker), num);


            portrait.sprite.color = Color.Lerp(Color.white, Color.black, Custom.SCurve(Mathf.Lerp(lastPortraitBlack, portraitBlack, timeStacker), 0.5f) * 0.75f);
        }
    }
}