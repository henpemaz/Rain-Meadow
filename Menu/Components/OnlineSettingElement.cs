using Menu;
using Menu.Remix;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;

namespace RainMeadow.UI.Components;
public abstract class OnlineSettingElement : PositionedMenuObject
{
    public const float tabMargin = 30;
    public const float elementHeight = 30;
    public const float posTween = 0.25f;
    public const float alphaTween = 0.15f;

    public static bool debug = false;
    public FSprite? spacingRect;
    public abstract MenuObject selectable {get;}

    public int position = 0;
    public Vector2 WantedPosition => Vector2.up * settingsBoxSize.y
        - Vector2.up * elementSize.y/2
        - Vector2.up * position * (spacing + elementHeight)
        + Vector2.right * (margin + (tab is null ? 0 : tabMargin));
    public Vector2? forcePos;
    public Vector2 targetPos;
    public bool grayedOut = false;
    public bool tabIndependant = false;
    public bool isClient = false;
    public bool visible = true;
    public float alpha = 1;
    protected float currentAlpha = 0;

    public readonly OnlineSettingTab? tab;
    public OnlineSlugcatSettingsBase? slugcatSettingPage => owner as OnlineSlugcatSettingsBase;
    public SettingsPage? settingsPage => owner as SettingsPage;

    public Vector2 settingsBoxSize = new(390, 430);
    public float spacing = 5f;
    public float margin = 30f;
    public float textSpacing = 300f;
    public Vector2 elementSize;

    public OnlineSettingElement(Menu.Menu menu, MenuObject owner, OnlineSettingTab? tab = null)
         : base(menu, owner, Vector2.zero)
    {
        this.tab = tab;
        if (debug)
        {
            spacingRect = new("pixel", false){
                anchorX = 0f,
                alpha = 0.25f,
                color = Color.red,
            };
            Container.AddChild(spacingRect);
        }
    }
    public OnlineSettingElement(Menu.Menu menu, OnlineSlugcatSettingsBase owner, OnlineSettingTab? tab = null)
         : this(menu, (MenuObject)owner, tab)
    {
        settingsBoxSize = owner.settingsBoxSize;
        spacing = owner.spacing;
        margin = owner.margin;
        textSpacing = owner.textSpacing;
    }
    public void HardSetAlpha(float alpha)
    {
        this.alpha = alpha;
        currentAlpha = alpha;
    }
    public void HardSetPosition(Vector2 position, bool setForcePos = false)
    {
        targetPos = position;
        pos = position;
        if (setForcePos) forcePos = position;
    }

    public override void Update()
    {
        base.Update();

        targetPos = forcePos ?? WantedPosition;

        pos = Vector2.Lerp(pos, targetPos, posTween);
        currentAlpha = Mathf.Lerp(currentAlpha, alpha, alphaTween);
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);

        elementSize.x = settingsBoxSize.x - (tab is null ? 0 : tabMargin);
        elementSize.y = elementHeight;
        if (spacingRect is not null)
        {
            spacingRect.scaleX = elementSize.x;
            spacingRect.scaleY = elementSize.y;
            spacingRect.alpha = 0.25f * currentAlpha;
            spacingRect.x = DrawX(timeStacker);
            spacingRect.y = DrawY(timeStacker) + elementSize.y/2;
        }
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        spacingRect?.RemoveFromContainer();
    }
}