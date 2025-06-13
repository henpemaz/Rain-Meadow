using System;
using System.Collections.Generic;
using Menu;
using RWCustom;
using UnityEngine;
using MoreSlugcats;
using System.Linq;
using System.Globalization;

namespace RainMeadow
{
    public class ColorSlugcatDialog : DialogNotify, CheckBox.IOwnCheckBox //recommend remix on as it has translation and slider enums
    {
       
        public static Slider.SliderID[] MMFHSLSLiderIDs => [MMFEnums.SliderID.Hue, MMFEnums.SliderID.Saturation, MMFEnums.SliderID.Lightness];
        public float SizeXOfDefaultCol => CurrLang == InGameTranslator.LanguageID.Japanese || CurrLang == InGameTranslator.LanguageID.French ? 110 : 
            CurrLang == InGameTranslator.LanguageID.Italian || CurrLang == InGameTranslator.LanguageID.Spanish ? 180 : 110;
        public ColorSlugcatDialog(ProcessManager manager, SlugcatStats.Name name, Action onOK) : base("", Utils.Translate("Custom colors"), new(500f, 400f), manager, onOK)
        {
            SetUpSafeColorChoices();
            id = name;
            colorChooser = -1;
            colorCheckbox = new(this, pages[0], this, new(size.x + 40, size.y + -40 * 5), 0, "", COLORCHECKBOXID);
            pages[0].subObjects.Add(colorCheckbox);
            MutualHorizontalButtonBind(colorCheckbox, okButton);
            GetSaveColorEnabled();
        }
        public override void ShutDownProcess()
        {
            base.ShutDownProcess();
            RemoveColorButtons();
        }
        public override void Update()
        {
            base.Update();

        }
        public override void Singal(MenuObject sender, string message)
        {
            base.Singal(sender, message);
            if (message == "DEFAULTCOL")
            {
                PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
                manager.rainWorld.progression.miscProgressionData.colorChoices[bodyInterface.slugcatID.value][colorChooser] = bodyInterface.defaultColors[colorChooser];
            }
            string openInterface = ColorSlugcatBodyButtons.OPENINTERFACESINGAL;
            if (message.StartsWith(openInterface) && int.TryParse(message.Substring(openInterface.Length), NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
            {
                AddOrRemoveColorInterface(result);
            }
        }
        public override void SliderSetValue(Slider slider, float f)
        {
            if (slider?.ID != null && MMFHSLSLiderIDs.Contains(slider.ID))
            {
                Vector3 hsl = GetHSL();
                hsl[MMFHSLSLiderIDs.IndexOf(slider.ID)] = f;
                ApplyHSL(ColorHelpers.RWHSLRange(hsl));
            }
        }
        public override float ValueOfSlider(Slider slider)
        {
            if (slider?.ID != null && MMFHSLSLiderIDs.Contains(slider.ID))
            {
                return GetHSL()[MMFHSLSLiderIDs.IndexOf(slider.ID)];

            }
            return 0;
        }
        public bool GetChecked(CheckBox box)
        {
            return box?.IDString == COLORCHECKBOXID && colorChecked;
        }
        public void SetChecked(CheckBox box, bool c)
        {
            if (box?.IDString == COLORCHECKBOXID)
            {
                SaveColorChoicesEnabled(c);
            }
        }
        public void GetSaveColorEnabled()
        {
            SetUpSafeColorChoices();
            if (!manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value))
            {
                manager.rainWorld.progression.miscProgressionData.colorsEnabled.Add(id.value, false);
            }
            colorCheckbox.Checked = manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value];

        }
        public void SaveColorChoicesEnabled(bool colorChecked)
        {
            if (!manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(id.value))
            {
                manager.rainWorld.progression.miscProgressionData.colorsEnabled.Add(id.value, colorChecked);
            }
            else
            {
                manager.rainWorld.progression.miscProgressionData.colorsEnabled[id.value] = colorChecked;
            }
            this.colorChecked = colorChecked;
            ((Action)(colorChecked ? AddColorButtons : RemoveColorButtons)).Invoke();
        }
        public void SetUpSafeColorChoices()
        {
            manager.rainWorld.progression.miscProgressionData.colorChoices ??= [];
            manager.rainWorld.progression.miscProgressionData.colorsEnabled ??= [];
        }
        public Vector3 GetHSL()
        {
            return this.manager.rainWorld.progression.GetCustomColorHSL(id, colorChooser);
        }
        public void ApplyHSL(Vector3 hsl)
        {
            this.manager.rainWorld.progression.SaveCustomColorHSL(id, colorChooser, hsl);
        }
        public void AddOrRemoveColorInterface(int num)
        {
            if (num == colorChooser)
            {
                RemoveColorInterface();
                PlaySound(SoundID.MENU_Remove_Level);
                return;
            }
            colorChooser = num;
            AddColorInterface();
            PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
        }
        public void AddColorButtons()
        {
            if (bodyInterface == null)
            {
                bodyInterface = GetColorInterface(id, new Vector2(size.x, size.y + 90f));
                pages[0].subObjects.Add(bodyInterface);
            }
        }
        public void AddColorInterface()
        {
            if (hueSlider == null)
            {
                hueSlider = new(this, pages[0], Translate("HUE"), new(size.x, size.y - 10), new(200, 30), MMFEnums.SliderID.Hue, false);
                pages[0].subObjects.Add(hueSlider);
            }
            if (satSlider == null)
            {
                satSlider = new(this, pages[0], Translate("SAT"), hueSlider.pos + new Vector2(0, -40), hueSlider.size, MMFEnums.SliderID.Saturation, false);
                pages[0].subObjects.Add(satSlider);
            }
            if (litSlider == null)
            {
                litSlider = new(this, pages[0], Translate("LIT"), satSlider.pos + new Vector2(0, -40), satSlider.size, MMFEnums.SliderID.Lightness, false);
                pages[0].subObjects.Add(litSlider);
            }
            if (defaultColor == null)
            {
                defaultColor = new(this, pages[0], Translate("Restore Default"), "DEFAULTCOL", litSlider.pos + new Vector2(0, -40), new(SizeXOfDefaultCol, 30));
                pages[0].subObjects.Add(defaultColor);
            }
            this.TryMutualBind(hueSlider, bodyInterface?.bodyButtons?.FirstOrDefault(), bottomTop: true);
            MutualVerticalButtonBind(satSlider, hueSlider);
            MutualVerticalButtonBind(litSlider, satSlider);
            MutualVerticalButtonBind(defaultColor, litSlider);
            MutualVerticalButtonBind(colorCheckbox, defaultColor);

        }
        public void RemoveColorButtons()
        {
            RemoveColorInterface();
            pages[0].ClearMenuObject(ref bodyInterface);
        }
        public void RemoveColorInterface()
        {
            for (int i = 0; i < bodyInterface?.bodyButtons?.Length; i++)
            {
                if (i == 0)
                {
                    MutualVerticalButtonBind(colorCheckbox, bodyInterface.bodyButtons[i]);
                    continue;
                }
                bodyInterface.bodyButtons[i].TryBind(colorCheckbox, bottom: true);
            }
            pages[0].ClearMenuObject(ref hueSlider);
            pages[0].ClearMenuObject(ref satSlider);
            pages[0].ClearMenuObject(ref litSlider);
            pages[0].ClearMenuObject(ref defaultColor);
            colorChooser = -1;
        }
        public ColorSlugcatBodyButtons GetColorInterface(SlugcatStats.Name slugcatID, Vector2 pos)
        {
             return new ColorSlugcatBodyButtons(this, pages[0], pos, slugcatID, PlayerGraphics.ColoredBodyPartList(slugcatID), [.. PlayerGraphics.DefaultBodyPartColorHex(slugcatID).Select(Custom.hexToColor).Select(Custom.RGB2HSL).Select(ColorHelpers.SetHSLString)]);
        }

        public const string COLORCHECKBOXID = "COLORCHECKED";
        public int colorChooser;
        public bool colorChecked;
        public CheckBox colorCheckbox;
        public SlugcatStats.Name id;
        public SimpleButton? defaultColor;
        public ColorSlugcatBodyButtons? bodyInterface;
        public HorizontalSlider? hueSlider, satSlider, litSlider;
    }
}
