// literally 90% of this class is ripped out from Menu.LevelSelector, just tweaked to work with lobby cards

using System;
using System.Net;
using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace RainMeadow;

public class LobbyCardsSelector : PositionedMenuObject
{
    public class LobbyCard : SimplerButton
    {
        public LobbyInfo lobbyInfo;
        /// <summary>
        /// A float between 1 and 0, 1 representing the card not faded and 0 representing fully faded
        /// </summary>
        public float fade;
        private List<MenuLabel> elements;
        public override bool CurrentlySelectableMouse
        {
            get
            {
                if (fade != 1) return false;
                return base.CurrentlySelectableMouse;
            }
        }

        public override bool CurrentlySelectableNonMouse
        {
            get
            {
                if (fade != 1) return false;
                return base.CurrentlySelectableNonMouse;
            }
        }

        public LobbyCard(Menu.Menu menu, MenuObject owner, LobbyInfo lobbyInfo) : base(menu, owner, "", new Vector2(0, 0), new Vector2(300f, 60f), $"Click to join {lobbyInfo.name}")
        {
            this.fade = 1f;
            this.lobbyInfo = lobbyInfo;
            this.elements = new List<MenuLabel>();

            this.menuLabel.RemoveSprites();
            this.RemoveSubObject(menuLabel);
            this.menuLabel = new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.name, new Vector2(5f, 30f), new(10f, 50f), true);
            this.elements.Add(menuLabel);

            if (lobbyInfo.hasPassword) this.elements.Add(new ProperlyAlignedMenuLabel(menu, this, "Private", new(256, 20), new(10, 50), false));
            this.elements.Add(new ProperlyAlignedMenuLabel(menu, this, $"{lobbyInfo.maxPlayerCount} max", new(256, 5), new(10, 50), false));
            this.elements.Add(new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.mode, new(5, 20), new(10, 50), false));
            this.elements.Add(new ProperlyAlignedMenuLabel(menu, this, lobbyInfo.playerCount + " player" + (lobbyInfo.playerCount == 1 ? "" : "s"), new(5, 5), new(10, 50), false));

            for (int i = 0; i < elements.Count; i++) this.subObjects.Add(elements[i]);
        }

        public override void Update()
        {
            base.Update();

            if (owner is not LobbyCardsSelector selector) return;

            fade = selector.list.PercentageOverYBound(pos.y);

            for (int i = 0; i < elements.Count; i++) this.elements[i].label.alpha = fade;
            for (int i = 0; i < roundedRect.sprites.Length; i++)
            {
                this.roundedRect.sprites[i].alpha = fade;
                this.roundedRect.fillAlpha = fade / 2;
            }
        }

        // public override void GrafUpdate(float timeStacker)
        // {
        //     if (sleep)
        //     {
        //         return;
        //     }

        //     // imageSprite.isVisible = true;
        //     // label.label.isVisible = true;

        //     base.GrafUpdate(timeStacker);
        //     float num = Custom.SCurve(Mathf.Lerp(lastFade, fade, timeStacker), 0.3f);
        //     float num3 = Mathf.InverseLerp(0f, 0.8f, num);
        //     // imageSprite.x = 0.01f + DrawX(timeStacker) + size.x / 2f;
        //     // imageSprite.y = 0.01f + DrawY(timeStacker) + 20f + (float)ThumbHeight * 1.01f * num3 / 2f;
        //     // imageSprite.alpha = num * (0.85f + 0.15f * Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker)) * Mathf.Pow(num2, 1.5f) * (1f - Mathf.Lerp(lastThumbChangeFade, thumbChangeFade, timeStacker));
        //     // imageSprite.scaleX = (float)ThumbWidth * (0.5f + 0.5f * Mathf.Pow(num3, 0.3f)) / imageSprite.element.sourcePixelSize.x;
        //     // imageSprite.scaleY = (float)ThumbHeight * num3 / imageSprite.element.sourcePixelSize.y;

        //     Color color;
        //     float num4 = Mathf.Lerp(1f, 0.5f + 0.5f * Mathf.Sin(Mathf.Lerp(buttonBehav.lastSin, buttonBehav.sin, timeStacker) / 30f * (float)Math.PI * 2f), Mathf.Lerp(buttonBehav.lastExtraSizeBump, buttonBehav.extraSizeBump, timeStacker) * num * Mathf.Lerp(1f, 0.5f, 1f));
        //     // label.label.color = Color.Lerp(Menu.MenuRGB(Menu.MenuColors.Black), MyColor(timeStacker), Mathf.Lerp(num * num4, UnityEngine.Random.value, Mathf.Lerp(lastSelectedBlink, selectedBlink, timeStacker)));
        //     float a = Mathf.Lerp(buttonBehav.lastCol, buttonBehav.col, timeStacker);
        //     a = Mathf.Max(a, Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
        //     color = HSLColor.Lerp(Menu.Menu.MenuColor(Menu.Menu.MenuColors.VeryDarkGrey), HSLColor.Lerp(Menu.Menu.MenuColor(Menu.Menu.MenuColors.DarkGrey), Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey), num4), a).rgb;


        //     // label.label.alpha = Mathf.Pow(num, 2f);
        //     if (num > 0f)
        //     // if (num2 * num > 0f)
        //     {
        //         Color color2 = Color.Lerp(Menu.Menu.MenuRGB(Menu.Menu.MenuColors.Black), Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey), Mathf.Lerp(buttonBehav.lastFlash, buttonBehav.flash, timeStacker));
        //         for (int i = 0; i < 9; i++)
        //         {
        //             roundedRect.sprites[i].color = color2;
        //             // roundedRect.sprites[i].alpha = num2 * num * 0.5f;
        //             roundedRect.sprites[i].alpha = num * 0.5f;
        //             roundedRect.sprites[i].isVisible = true;
        //         }

        //         for (int j = 9; j < 17; j++)
        //         {
        //             roundedRect.sprites[j].color = color;
        //             // roundedRect.sprites[j].alpha = num2 * num;
        //             roundedRect.sprites[j].alpha = num;
        //             roundedRect.sprites[j].isVisible = true;
        //         }
        //     }
        //     else
        //     {
        //         for (int k = 0; k < 9; k++)
        //         {
        //             roundedRect.sprites[k].isVisible = false;
        //         }

        //         for (int l = 9; l < 17; l++)
        //         {
        //             roundedRect.sprites[l].isVisible = false;
        //         }
        //     }
        // }
    }

    public class LobbyCardsList : RectangularMenuObject, Slider.ISliderOwner
    {
        public List<LobbyCard> lobbyCards;
        public FContainer cardsContainer;
        public LobbyCardsFilter filter;
        public LevelSelector.ScrollButton scrollUpButton;
        public LevelSelector.ScrollButton scrollDownButton;
        public SymbolButton[] sideButtons;
        public MenuLabel[] labels;
        public float[,] labelsFade;
        public FSprite[] rightLines;
        public float floatScrollPos;
        public float floatScrollVel;
        public VerticalSlider scrollSlider;
        public float sliderValue;
        public float sliderValueCap;
        public bool sliderPulled;
        public int scrollPos;
        public int TotalItems => lobbyCards.Count;
        public int MaxVisibleItems => (int)(size.y / CardHeight);
        public int LastPossibleScroll => Math.Max(0, TotalItems - MaxVisibleItems);
        public float CardHeight => 70;
        public SymbolButton RefreshButton => sideButtons[0];
        public SymbolButton FilterButton => sideButtons[1];
        private float LowerBound => pos.y;
        private float UpperBound => pos.y + size.y;

        public float ValueOfSlider(Slider slider)
        {
            return 1f - sliderValue;
        }

        public void SliderSetValue(Slider slider, float setValue)
        {
            sliderValue = 1f - setValue;
            sliderPulled = true;
        }

        public float StepsDownOfItem(int itemIndex)
        {
            float num = 0f;
            for (int i = 0; i <= Math.Min(itemIndex, lobbyCards.Count - 1); i++)
            {
                num += (i > 0) ? Mathf.Pow(Custom.SCurve(1f, 0.3f), 0.5f) : 1f;
            }

            return num;
        }

        public bool IsWithinYBounds(float y)
        {
            return y > LowerBound && y < UpperBound;
        }

        /// <summary>
        /// Returns a float between 0 and 1 depending on how far the given y pos is from the y boundaries of the list within a distance of CardHeight
        /// </summary>
        public float PercentageOverYBound(float y)
        {
            if (y < LowerBound) return 1 - Math.Min(1, (LowerBound - y) / CardHeight);
            float cardUpperBound = y + CardHeight;
            if (cardUpperBound > UpperBound) return 1 - Math.Min(1, (cardUpperBound - UpperBound) / CardHeight);
            return 1;
        }

        public float IdealYPosForItem(int itemIndex)
        {
            float num = StepsDownOfItem(itemIndex);
            num -= floatScrollPos;
            return pos.y + size.y - num * CardHeight;
        }

        public void ConstrainScroll()
        {
            if (scrollPos > LastPossibleScroll) scrollPos = LastPossibleScroll;
            if (scrollPos < 0) scrollPos = 0;
        }

        public void AddCard(LobbyCard card)
        {
            card.pos.x = pos.x + 15f;
            card.pos.y = IdealYPosForItem(lobbyCards.Count);
            lobbyCards.Add(card);
            subObjects.Add(card);
        }

        public void RemoveCard(LobbyCard card)
        {
            card.RemoveSprites();
            RemoveSubObject(card);
            lobbyCards.Remove(card);
            ConstrainScroll();
        }

        public void AddScroll(int scrollDir)
        {
            scrollPos += scrollDir;
            ConstrainScroll();
        }

        public LobbyCardsList(Menu.Menu menu, LobbyCardsSelector owner, Vector2 pos, Vector2 size, List<LobbyCard> cards) : base(menu, owner, pos, size)
        {
            if (!Futile.atlasManager.DoesContainAtlas("uielements"))
            {
                HeavyTexturesCache.futileAtlasListings.Add(Futile.atlasManager.LoadAtlas("illustrations/uielements").name);
            }
            filter = new LobbyCardsFilter();

            myContainer = new FContainer();
            owner.Container.AddChild(myContainer);

            lobbyCards = new List<LobbyCard>();

            cardsContainer = new FContainer();
            Container.AddChild(cardsContainer);

            scrollUpButton = new LevelSelector.ScrollButton(menu, this, "UP", new Vector2(0.01f + size.x / 2f - 50f, 0.01f + size.y), 0);
            scrollUpButton.size.x = 100f;
            scrollUpButton.roundedRect.size.x = 100f;
            subObjects.Add(scrollUpButton);
            scrollDownButton = new LevelSelector.ScrollButton(menu, this, "DOWN", new Vector2(0.01f + size.x / 2f - 50f, -25.99f), 2);
            scrollDownButton.size.x = 100f;
            scrollDownButton.roundedRect.size.x = 100f;
            subObjects.Add(scrollDownButton);

            rightLines = new FSprite[3];
            for (int i = 0; i < rightLines.Length; i++)
            {
                rightLines[i] = new FSprite("pixel");
                rightLines[i].anchorX = 0f;
                rightLines[i].anchorY = 0f;
                rightLines[i].scaleX = 2f;
                Container.AddChild(rightLines[i]);
            }

            sideButtons = new SymbolButton[2];
            sideButtons[0] = new SimplerSymbolButton(menu, this, "Menu_Symbol_Repeats", "REFRESH", new Vector2(size.x - 8f + 0.01f, 14.01f));
            subObjects.Add(sideButtons[0]);
            sideButtons[1] = new SimplerSymbolButton(menu, this, "filter", "FILTER", sideButtons[0].pos + new Vector2(0, 30f));
            subObjects.Add(sideButtons[1]);

            labels = new MenuLabel[2];
            labelsFade = new float[labels.Length, 2];
            for (int j = 0; j < 2; j++)
            {
                labels[j] = new MenuLabel(menu, this, "", sideButtons[j].pos + new Vector2(10f, -3f), new Vector2(50f, 30f), bigText: false);
                labels[j].label.alignment = FLabelAlignment.Left;
                subObjects.Add(labels[j]);
            }
            labels[0].text = menu.Translate("Refresh list");

            scrollSlider = new VerticalSlider(menu, this, "Slider", new Vector2(-16f, 9f), new Vector2(30f, size.y - 40f), Slider.SliderID.LevelsListScroll, subtleSlider: true);
            subObjects.Add(scrollSlider);
            floatScrollPos = scrollPos;

            for (int i = 0; i < cards.Count; i++)
            {
                AddCard(cards[i]);
            }
        }

        public override void Update()
        {
            base.Update();

            if (MouseOver && menu.manager.menuesMouseMode && menu.mouseScrollWheelMovement != 0)
            {
                AddScroll(menu.mouseScrollWheelMovement);
            }

            for (int i = 0; i < lobbyCards.Count; i++)
            {
                lobbyCards[i].pos.y = IdealYPosForItem(i);
            }

            scrollDownButton.buttonBehav.greyedOut = scrollPos == LastPossibleScroll;
            scrollUpButton.buttonBehav.greyedOut = scrollPos == 0;
            float num = scrollPos;

            floatScrollPos = Custom.LerpAndTick(floatScrollPos, num, 0.01f, 0.01f);
            floatScrollVel *= Custom.LerpMap(Math.Abs(num - floatScrollPos), 0.25f, 1.5f, 0.45f, 0.99f);
            floatScrollVel += Mathf.Clamp(num - floatScrollPos, -2.5f, 2.5f) / 2.5f * 0.15f;
            floatScrollVel = Mathf.Clamp(floatScrollVel, -1.2f, 1.2f);
            floatScrollPos += floatScrollVel;
            sliderValueCap = Custom.LerpAndTick(sliderValueCap, LastPossibleScroll, 0.02f, (float)lobbyCards.Count / 40f);
            if (LastPossibleScroll == 0)
            {
                sliderValue = Custom.LerpAndTick(sliderValue, 0.5f, 0.02f, 0.05f);
                scrollSlider.buttonBehav.greyedOut = true;
            }
            else
            {
                scrollSlider.buttonBehav.greyedOut = false;

                if (sliderPulled)
                {
                    floatScrollPos = Mathf.Lerp(0f, sliderValueCap, sliderValue);
                    scrollPos = Custom.IntClamp(Mathf.RoundToInt(floatScrollPos), 0, LastPossibleScroll);
                    sliderPulled = false;
                }
                else
                {
                    sliderValue = Custom.LerpAndTick(sliderValue, Mathf.InverseLerp(0f, sliderValueCap, floatScrollPos), 0.02f, 0.05f);
                }
            }

            FilterButton.buttonBehav.greyedOut = lobbyCards.Count == 0;

            for (int i = 0; i < labels.Length; i++)
            {
                labelsFade[i, 1] = labelsFade[i, 0];
                if (sideButtons[i].Selected)
                {
                    labelsFade[i, 0] = Custom.LerpAndTick(labelsFade[i, 0], 0.33f, 0.04f, 1f / 60f);

                    if (i == 1) labels[i].text = filter.enabled ? "Edit filter" : "Filter lobbies";
                }
                else
                {
                    labelsFade[i, 0] = Custom.LerpAndTick(labelsFade[i, 0], 0f, 0.04f, 1f / 60f);
                }
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);

            for (int i = 0; i < rightLines.Length; i++)
            {
                rightLines[i].x = DrawX(timeStacker) + size.x + 0.01f;
                float num = (i != 0) ? (sideButtons[i - 1].DrawY(timeStacker) + sideButtons[i - 1].DrawSize(timeStacker).y + 0.01f) : (DrawY(timeStacker) + 9.01f);
                float num2 = (i != rightLines.Length - 1) ? (sideButtons[i].DrawY(timeStacker) + 0.01f) : (DrawY(timeStacker) + DrawSize(timeStacker).y - 10.99f);
                rightLines[i].y = num;
                rightLines[i].scaleY = num2 - num;
                rightLines[i].color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
            }
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].label.alpha = Mathf.Lerp(labelsFade[i, 1], labelsFade[i, 0], timeStacker);
            }
        }

        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);

            switch (message)
            {
                case "UP":
                    AddScroll(-1);
                    break;
                case "DOWN":
                    AddScroll(1);
                    break;
            }
        }
    }

    public class LobbyCardsFilter
    {
        public bool enabled;

        public LobbyCardsFilter()
        {
            enabled = false;
        }
    }

    public LobbyCardsList list;

    public LobbyCardsSelector(Menu.Menu menu, MenuObject owner) : base(menu, owner, new Vector2(0, 0))
    {
        var fakeEndpoint = new IPEndPoint(IPAddress.Loopback, UdpPeer.STARTING_PORT);
        list = new LobbyCardsList(menu, this, new Vector2(500f, 100f), new Vector2(330f, 490f), new List<LobbyCard>{
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "Test", "Meadow", 3, true, 24)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local", "Meadow", 1, false, 4)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local:HasPassword", "Meadow", 4, true, 4)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test1", "ArenaCompetitive", 2, true, 7)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local TEST", "Story", 17, false, 32)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test \\w symbol", "Meadow", 3, false, 4)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test2", "Story", 5, false, 15)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3%", "Story", 2, true, 6)),
            new LobbyCard(menu, this, new LobbyInfo(fakeEndpoint, "local test3% no i didnt copy and paste this cause im lazy you did", "Story", 2, true, 6)),
        });

        subObjects.Add(list);
    }
}