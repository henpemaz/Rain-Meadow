using System;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using RainMeadow.UI.Interfaces;
using UnityEngine;

namespace RainMeadow.UI.Components.Configurables;

public class OnlineSettingButtons : OnlineSettingElement, IPLEASEUPDATEME
{
    public const float boxMargin = 5f;
    public const float buttonGap = 6f;
    public const float buttonWidth = 90f;

    public readonly struct ButtonDef(string text, string description, bool ownerOnly, Action<OnlineSettingButtons> onClick)
    {
        public readonly string text = text;
        public readonly string description = description;
        public readonly bool ownerOnly = ownerOnly;
        public readonly Action<OnlineSettingButtons> onClick = onClick;
    }

    public readonly MenuLabel label;
    public readonly MenuTabWrapper tabWrapper;
    public readonly OpSimpleButton[] buttons;
    public readonly bool[] ownerOnly;

    public string defaultText;
    public int messageTimer;
    public int messageDuration = 120;
    public bool IsHidden {get; set;} // avoid click through
    public bool active = true;

    public override MenuObject selectable => buttons[0].wrapper;

    public OnlineSettingButtons(Menu.Menu menu, OnlineSlugcatSettingsBase owner, OnlineSettingTab? tab, string labelText, params ButtonDef[] defs)
         : base(menu, owner.scroller, tab)
    {
        tabWrapper = owner.tabWrapper;
        defaultText = labelText;
        size = new Vector2(ownerBoxSize.x - (tab is null ? 0 : tabMargin), elementHeight);

        label = new(menu, this, menu.Translate(labelText), Vector2.zero, new(textSpacing, elementHeight), false);
        label.label.alignment = FLabelAlignment.Left;
        this.SafeAddSubobjects(label);

        buttons = new OpSimpleButton[defs.Length];
        ownerOnly = new bool[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            ButtonDef def = defs[i];
            ownerOnly[i] = def.ownerOnly;

            OpSimpleButton button = new(Vector2.zero, new(buttonWidth, elementHeight), menu.Translate(def.text));
            if (!string.IsNullOrWhiteSpace(def.description))
                button.description = menu.Translate(def.description);

            Action<OnlineSettingButtons> onClick = def.onClick;
            button.OnClick += _ => {if (active && !IsHidden) onClick(this);};
            new PatchedUIelementWrapper(tabWrapper, button);
            buttons[i] = button;
        }

        for (int i = 1; i < buttons.Length; i++)
            menu.MutualHorizontalButtonBind(buttons[i - 1].wrapper, buttons[i].wrapper);
    }

    public void ShowMessage(string translatedText, Color color)
    {
        label.text = translatedText;
        label.label.color = color;
        messageTimer = messageDuration;
    }

    public override void Update()
    {
        base.Update();

        label.pos = Vector2.left * textSpacing / 2f;

        float x = size.x - boxMargin;
        for (int i = buttons.Length - 1; i >= 0; i--)
        {
            x -= buttons[i].size.x;
            buttons[i].pos = pos
                + Vector2.right * x
                + Vector2.up * (size.y - buttons[i].size.y) / 2f;
            x -= buttonGap;
            buttons[i].greyedOut = ownerOnly[i] && grayedOut;
        }

        if (messageTimer > 0)
        {
            messageTimer--;
            if (messageTimer == 0)
            {
                label.text = menu.Translate(defaultText);
                label.label.color = Color.white;
            }
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        label.label.isVisible = visible;
        label.label.alpha = currentAlpha * (grayedOut && messageTimer <= 0 ? 0.5f : 1f);

        foreach (OpSimpleButton button in buttons)
        {
            if (!visible && button.held) button.held = false;
            button.Hidden = !visible;
            button.myContainer.isVisible = visible;
            button.myContainer.alpha = currentAlpha;
        }
    }
}
