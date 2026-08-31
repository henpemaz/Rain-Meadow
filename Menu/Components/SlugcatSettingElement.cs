using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components;
public abstract class SlugcatSettingElement : PositionedMenuObject
{
    public const float tabMargin = 30;
    public const float elementHeight = 30;
    public static bool debug = false;
    public int position = 0;
    public bool grayedOut = false;
    public bool tabIndependant = false;
    public bool visible = true;
    public readonly SlugcatSettingTab? tab;
    public OnlineSlugcatSettingsBase settingPage => (owner as OnlineSlugcatSettingsBase)!;
    public FSprite? spacingRect;
    public Vector2 elementSize;

    public SlugcatSettingElement(Menu.Menu menu, OnlineSlugcatSettingsBase owner, SlugcatSettingTab? tab = null)
         : base(menu, owner, Vector2.zero)
    {
        this.tab = tab;
        if (debug)
        {
            this.spacingRect = new("pixel", false){
                anchorX = 0f,
                alpha = 0.25f,
                color = Color.red,
            };
            this.Container.AddChild(spacingRect);
        }
    }

    public override void Update()
    {
        base.Update();
        this.pos = Vector2.up * settingPage.settingsBoxSize.y
            - Vector2.up * elementSize.y/2
            - Vector2.up * position * (settingPage.spacing + elementHeight)
            + Vector2.right * (settingPage.margin + (tab is null ? 0 : tabMargin));
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        elementSize.x = settingPage.settingsBoxSize.x - (tab is null ? 0 : tabMargin);
        elementSize.y = elementHeight;
        if (spacingRect is not null)
        {
            spacingRect.scaleX = elementSize.x;
            spacingRect.scaleY = elementSize.y;
            spacingRect.x = DrawX(timeStacker);
            spacingRect.y = DrawY(timeStacker) + elementSize.y/2;
        }
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        this.spacingRect?.RemoveFromContainer();
    }
}