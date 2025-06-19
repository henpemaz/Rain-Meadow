using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Menu;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    //supposed to follow saves
    public class ColorSlugcatBodyButtons : PositionedMenuObject
    {
        public int PerPage { get => perPage; set => perPage = Mathf.Max(1, value); }
        public int CurrentOffset { get => currentOffset; set => currentOffset = Mathf.Clamp(value, 0, (bodyNames?.Count > 0) ? ((bodyNames.Count - 1) / PerPage) : 0); }
        public bool PagesOn => bodyNames?.Count > PerPage;
        public ColorSlugcatBodyButtons(Menu.Menu menu, MenuObject owner, Vector2 pos, SlugcatStats.Name slugcatID, List<string> names, List<string> defaultColors) : base(menu, owner, pos)
        {
            PerPage = 2;
            CurrentOffset = 0;
            bodyNames = names;
            this.slugcatID = slugcatID;
            this.defaultColors = defaultColors;
            SafeSaveColor();
            PopulatePage(CurrentOffset);
            if (PagesOn)
            {
                ActivateButtons();
            }
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            bodyColors?.Do((illuIndex) =>
            {
                illuIndex.Key.color = ColorHelpers.HSL2RGB(this.menu.manager.rainWorld.progression.GetCustomColorHSL(slugcatID, illuIndex.Value));
            });
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            DeactivateButtons();
            ClearInterface();
        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == PREVSINGAL)
            {
                PrevPage();
            }
            if (message == NEXTSINGAL)
            {
                NextPage();
            }
        }
        public void PrevPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage((CurrentOffset - 1 < 0) ? ((bodyNames != null && bodyNames.Count > 0) ? ((bodyNames.Count - 1) / PerPage) : 0) : (CurrentOffset - 1));
        }
        public void NextPage()
        {
            menu.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            PopulatePage(bodyNames == null || bodyNames.Count == 0 || CurrentOffset + 1 > (bodyNames.Count - 1) / PerPage ? 0 : (CurrentOffset + 1));
        }
        public void PopulatePage(int offset)
        {
            ClearInterface(true);
            List<SimpleButton> buttons = [];
            List<RoundedRect> borders = [];
            CurrentOffset = offset;
            int num = CurrentOffset * PerPage;
            while (num < bodyNames?.Count && num < (CurrentOffset + 1) * PerPage)
            {
                SimpleButton bodyButton = new(menu, this, menu.Translate(bodyNames[num]), OPENINTERFACESINGAL + num.ToString(CultureInfo.InvariantCulture), new Vector2(num % PerPage * 90, -50), new(80, 30));
                RoundedRect bodyColorBorder = new(menu, this, new(bodyButton.pos.x + (bodyButton.size.x / 4), bodyButton.pos.y + 50), new(40, 40), false);
                MenuIllustration bodyColor = new(menu, this, "", "square", bodyColorBorder.pos + new Vector2(2, 2), false, false);
                subObjects.AddRange([bodyButton, bodyColorBorder, bodyColor]);
                menu.TryMutualBind(buttons.GetValueOrDefault((num - 1) % PerPage), bodyButton, true);
                buttons.Add(bodyButton);
                borders.Add(bodyColorBorder);
                bodyColors.Add(bodyColor, num);
                num++;
            }
            bodyButtons = [.. buttons];
            bodyColorBorders = [.. borders];
            menu.TryMutualBind(prevButton, bodyButtons.FirstOrDefault(), true);
            menu.TryMutualBind(bodyButtons.LastOrDefault(), nextButton, true);
            (menu as ColorSlugcatDialog)?.RemoveColorInterface();
        }
        public void SafeSaveColor()
        {
            if (!menu.manager.rainWorld.progression.miscProgressionData.colorChoices.ContainsKey(slugcatID.value))
            {
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices.Add(slugcatID.value, []);
            }
            menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value] ??= [];
            while (menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Count < bodyNames?.Count)
            {
                menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Add(defaultColors[menu.manager.rainWorld.progression.miscProgressionData.colorChoices[slugcatID.value].Count]);
            }
        }
        public void ActivateButtons()
        {
            if (prevButton == null)
            {
                prevButton = new(menu, this, "Menu_Symbol_Arrow", PREVSINGAL, new Vector2(-34f, -50f));
                subObjects.Add(prevButton);
                prevButton.symbolSprite.rotation = 270;
            }
            if (nextButton == null)
            {
                nextButton = new(menu, this, "Menu_Symbol_Arrow", NEXTSINGAL, new Vector2(prevButton.pos.x + 54 + (perPage * 80), prevButton.pos.y));
                subObjects.Add(nextButton);
                nextButton.symbolSprite.rotation = 90;
            }
            menu.MutualHorizontalButtonBind(prevButton, nextButton);
            menu.TryMutualBind(prevButton, bodyButtons.FirstOrDefault(), true);
            menu.TryMutualBind(bodyButtons.LastOrDefault(), nextButton, true);
        }
        public void DeactivateButtons()
        {
            this.ClearMenuObject(ref prevButton);
            this.ClearMenuObject(ref nextButton);
        }
        public void ClearInterface(bool refresh = false)
        {
            this.ClearMenuObjectIList(bodyButtons);
            this.ClearMenuObjectIList(bodyColorBorders);
            this.ClearMenuObjectIList(bodyColors?.Keys);
            bodyButtons = refresh ? [] : default;
            bodyColorBorders = refresh ? [] : default;
            bodyColors = refresh ? [] : default;
        }

        protected int currentOffset, perPage;
        public const string OPENINTERFACESINGAL = "COLORBODYBUTTONS_CUSTOMCOLOR", PREVSINGAL = "PrevPageColors_COLORBODYBUTTONS", NEXTSINGAL = "NextPageColors_COLORBODYBUTTONS";
        public List<string> bodyNames;
        public List<string> defaultColors;
        public SlugcatStats.Name slugcatID;
        public SimpleButton[]? bodyButtons;
        public RoundedRect[]? bodyColorBorders;
        public Dictionary<MenuIllustration, int>? bodyColors;
        public SymbolButton? prevButton, nextButton;
    }
}
