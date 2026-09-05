using Menu;
using Menu.Remix;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;
using HarmonyLib;
using Menu.Remix.MixedUI;

namespace RainMeadow.UI.Components;
public class OnlineSettingKeycode : OnlineSettingUIconfig
{
    public const int tickTillNextDot = 15;
    private static string[] dots = [".", "..", "..."];
    public KeyCode valueKeyCode => OpKeyBinder.StringToKeyCode(keyBinder.value);
    public OpKeyBinder keyBinder => (OpKeyBinder)uiConfig;
    public bool lastHeld = false; // ok the feedback was so bad i'm making my own;
    public int dotCycle;
    public OnlineSettingKeycode(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SettingsConfigData config, OnlineSettingTab? tab = null)
         : this(menu, owner, owner.tabWrapper, config, tab) {}
    public OnlineSettingKeycode(Menu.Menu menu, MenuObject owner, MenuTabWrapper tabWrapper, SettingsConfigData config, OnlineSettingTab? tab = null)
         : base(menu,
            owner,
            tabWrapper,
            config,
            new OpKeyBinder(new Configurable<KeyCode>((KeyCode)config.configurable.BoxedValue) {OI = RainMeadow.rainMeadowOptions}, Vector2.zero, new Vector2(150f, elementHeight)),
            tab)
    {
        if (OpKeyBinder._BoundKey is null) OpKeyBinder._BoundKey = [];
    }

    public override void Update()
    {
        base.Update();
        if (keyBinder.held != lastHeld)
        {
            if (keyBinder.held)
            {
				keyBinder.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
				keyBinder._label.text = dots[0];
            }
            else
            {
                keyBinder._label.text = keyBinder.value;
            }
            dotCycle = 0;
            lastHeld = keyBinder.held;
        }
        else
        {
            if (keyBinder.held)
            {
                dotCycle++;
                if (dotCycle >= tickTillNextDot * dots.Length) dotCycle = 0;
                keyBinder._label.text = dots[Mathf.Clamp(dotCycle/tickTillNextDot, 0, dots.Length - 1)];
            }
        }
    }

    protected override void ShowSyncInUIConfig(bool grayedOut, object value)
        => ShowSyncInGenericUIConfig(keyBinder, grayedOut, value);
    public override void GrafUpdate(float timeStacker)
    {
        if (color is not null) keyBinder.colorEdge = (Color)color;
        base.GrafUpdate(timeStacker);
        label.label.color = keyBinder.rect.colorEdge;

        keyBinder._sprite.isVisible = false;
        keyBinder._label.isVisible = visible;
        keyBinder._label.alpha = currentAlpha * (keyBinder.held ? 0.5f : 1f);
        HandleRectAlpha(keyBinder.rect);
    }
}
