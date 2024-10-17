using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class SlugcatCustomizationSelector : RectangularMenuObject
    {
        public SlugcatCustomization customization;
        public OpTinyColorPicker eyeColorSelector;
        public OpTinyColorPicker bodyColorSelector;
        public OpComboBox2 slugcatSelector;
        private OpTextBox nicknameBox;

        public SlugcatCustomizationSelector(SmartMenu menu, MenuObject owner, Vector2 pos, SlugcatCustomization customization) : base(menu, owner, pos, new Vector2(40, 120))
        {
            this.customization = customization;

            this.subObjects.Add(new MenuLabel(menu, this, "Eye color", Vector2.zero, new Vector2(100, 18), false));
            this.eyeColorSelector = new OpTinyColorPicker(menu, pos + new Vector2(100, 0), customization.eyeColor);
            this.subObjects.Add(new MenuLabel(menu, this, "Body color", new Vector2(0, 30), new Vector2(100, 18), false));
            this.bodyColorSelector = new OpTinyColorPicker(menu, pos + new Vector2(100, 30), customization.bodyColor);
            new UIelementWrapper(menu.tabWrapper, bodyColorSelector);
            new UIelementWrapper(menu.tabWrapper, eyeColorSelector);
            this.slugcatSelector = new OpComboBox2(
                new Configurable<SlugcatStats.Name>(customization.playingAs)
                , pos + new Vector2(140, 0), 160,
                // wasn't there a helper for common types?
                Menu.Remix.MixedUI.OpResourceSelector.GetEnumNames(null, typeof(SlugcatStats.Name))
                    .Select(li => {
                        li.displayName = menu.Translate(li.displayName);
                        return li;
                    }).ToList())
            { colorEdge = MenuColorEffect.rgbWhite };
            new UIelementWrapper(menu.tabWrapper, slugcatSelector);
            this.nicknameBox = new OpTextBox(new Configurable<string>(customization.nickname), pos + new Vector2(140, 32), 160);
            new UIelementWrapper(menu.tabWrapper, nicknameBox);

            bodyColorSelector.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;
            eyeColorSelector.OnValueChangedEvent += ColorSelector_OnValueChangedEvent;

            slugcatSelector.OnValueChanged += SlugcatSelector_OnValueChanged;
            nicknameBox.OnValueChanged += NicknameBox_OnValueChanged;
            nicknameBox.OnValueUpdate += NicknameBox_OnValueUpdate;
        }

        private void NicknameBox_OnValueUpdate(UIconfig config, string value, string oldValue)
        {
            RainMeadow.DebugMe();
            customization.nickname = value;
        }

        private void NicknameBox_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            RainMeadow.DebugMe();
            customization.nickname = value;
        }

        private void SlugcatSelector_OnValueChanged(UIconfig config, string value, string oldValue)
        {
            customization.playingAs = new SlugcatStats.Name(value);
        }

        private void ColorSelector_OnValueChangedEvent()
        {
            customization.eyeColor = eyeColorSelector.valuecolor;
            customization.bodyColor = bodyColorSelector.valuecolor;
        }
    }
}
