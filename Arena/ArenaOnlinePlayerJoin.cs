using RainMeadow;
using Rewired;
using RWCustom;
using System;
using UnityEngine;

namespace Menu
{
    public class ArenaOnlinePlayerJoinButton : ButtonTemplate
    {
        public MenuLabel menuLabel;
        public MenuIllustration? portrait;
        public MenuIllustration joinButtonImage;
        public RoundedRect roundedRect, selectRect;
        public Joystick joystickPressed;
        public bool joystickAvailable;
        public int assignedJoystick, labelFadeCounter, colorIndex;
        public float labelFade, lastLabelFade, portraitBlack = 1, lastPortraitBlack = 1f;
        public bool lastInput;
        public bool readyForCombat;
        public SimplerButton usernameButton;
        public SimplerSymbolButton? kickButton;
        public OnlinePlayer profileIdentifier;
        public SlugcatStats.Name? slugcat;

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
        public ArenaOnlinePlayerJoinButton(Menu menu, MenuObject owner, Vector2 pos, int index, OnlinePlayer player, bool canKick)
            : base(menu, owner, pos, new Vector2(100f, 100f))
        {
            slugcat = SlugcatStats.Name.White;
            colorIndex = index;
            profileIdentifier = player;
            roundedRect = new(menu, this, new Vector2(0f, 0f), size, filled: true);
            selectRect = new(menu, this, new Vector2(0f, 0f), size, filled: false);
            portrait = new(menu, this, "", "MultiplayerPortrait" + index + "1", size / 2f, crispPixels: true, anchorCenter: true);
            readyForCombat = false;
            string text = "";
            float num = 0f;
            menuLabel = new(menu, this, menu.Translate("PLAYER") + (InGameTranslator.LanguageID.UsesSpaces(menu.CurrLang) ? " " : "") + (index + 1) + "\r\n" + text, new Vector2(0.01f, 0.1f + num), size, bigText: false);
            usernameButton = new(menu, this, profileIdentifier.id.name, new(0, -40), new(100, 30));
            usernameButton.OnClick += (_) =>
            {
                profileIdentifier.id.OpenProfileLink();
            };
            subObjects.AddRange([roundedRect, selectRect, portrait, menuLabel, usernameButton]);
            menu.MutualVerticalButtonBind(usernameButton, this);
            if (canKick)
            {
                kickButton = new(menu, this, "Menu_Symbol_Clear_All", "KICKPLAYER", new(40, 110));
                kickButton.OnClick += (_) =>
                {
                    RainMeadow.RainMeadow.Debug(string.Format("Kicked User: {0}", profileIdentifier), "/Arena/ArenaLobbyMenu.cs", "AddClassButtons");
                    BanHammer.BanUser(profileIdentifier);
                };
                subObjects.Add(kickButton);
                menu.MutualVerticalButtonBind(this, kickButton);
            }
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
            var champBorderColor = Color.yellow;
            if (RainMeadow.RainMeadow.isArenaMode(out var arena) && profileIdentifier != OnlineManager.mePlayer)
            {
                if (arena.playersReadiedUp != null &&
                      arena.playersReadiedUp.list != null &&
                      arena.playersReadiedUp.list.Contains(profileIdentifier.id))
                {
                    readyForCombat = true;
                }
                else
                {
                    readyForCombat = false;
                }
            }

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

            menuLabel.label.color = Color.Lerp(PlayerGraphics.DefaultSlugcatColor(SlugcatStats.Name.White), MyColor(timeStacker), num);
            var ogColor = Color.Lerp(Color.white, Color.black, Custom.SCurve(Mathf.Lerp(lastPortraitBlack, portraitBlack, timeStacker), 0.5f) * 0.75f);


            float alternationSpeed = 0.75f; // Adjust for alternation speed.
            float lerpFactor = Mathf.PingPong(Time.time * alternationSpeed, 1f);


            if (arena != null && arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(profileIdentifier.id))
            {
                roundedRect.borderColor = HSLColor.Lerp(ogColor.ToHSL(), champBorderColor.ToHSL(), lerpFactor);
                portrait.sprite.color = Color.Lerp(ogColor, champBorderColor, lerpFactor);
            }
            else
            {
                portrait.sprite.color = ogColor;
                roundedRect.borderColor = ogColor.ToHSL();
            }
        }
        public void SetNewSlugcat(SlugcatStats.Name? slugcat, int currentColorIndex, Func<SlugcatStats.Name, int, string> arenaImage)
        {
            if (this.slugcat != slugcat || colorIndex != currentColorIndex)
            {
                this.slugcat = slugcat;
                colorIndex = currentColorIndex;
                SetNewPortrait(arenaImage.Invoke(slugcat!, currentColorIndex));
            }
        } //func for now ig
        public void SetNewPortrait(string newFile)
        {
            if (portrait!.fileName != newFile)
            {
                RainMeadow.RainMeadow.Debug(newFile);
                portrait.fileName = newFile;
                portrait.LoadFile();
                portrait.sprite.SetElementByName(portrait.fileName);
            }
        }

    }
}