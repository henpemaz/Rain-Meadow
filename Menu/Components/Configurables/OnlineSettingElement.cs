using Menu;
using Menu.Remix;
using UnityEngine;
using static RainMeadow.UI.Components.OnlineSlugcatAbilitiesInterface;

namespace RainMeadow.UI.Components.Configurables;
public abstract class OnlineSettingElement : RectangularMenuObject
{
    public const float tabMargin = 30;
    public const float elementHeight = 30;
    public const float posTween = 0.25f;
    public const float alphaTween = 0.15f;

    public static bool debug = false;
    public FSprite? spacingRect;
    public abstract MenuObject selectable {get;}

    public int position = 0;
    public Vector2 offsetPos = Vector2.zero;
    public Vector2 WantedPosition => Vector2.up * ownerBoxSize.y
        - Vector2.up * size.y/2
        - Vector2.up * position * (spacing + elementHeight)
        + Vector2.right * (margin + (tab is null ? 0 : tabMargin))
        + offsetPos;
    public Vector2? forcePos;
    public Vector2 targetPos;
    public bool grayedOut = false;
    public bool tabIndependant = false;
    public bool isClient = false;
    public bool visible = true;
    public float alpha = 1;
    protected float currentAlpha = 0;
    public virtual int additionalPositionsTaken => 0;

    public readonly OnlineSettingTab? tab;
    public OnlineSlugcatSettingsBase? slugcatSettingPage => settingsPage as OnlineSlugcatSettingsBase;
    public SettingsPage? settingsPage => owner as SettingsPage ?? owner?.owner?.owner as SettingsPage;

    public Vector2 ownerBoxSize = new(390, 430);
    public float spacing = 5f;
    public float margin = 30f;
    public float textSpacing = 300f;

    public OnlineSettingElement(Menu.Menu menu, MenuObject owner, OnlineSettingTab? tab = null)
         : base(menu, owner, Vector2.zero, new Vector2(390, elementHeight))
    {
        this.tab = tab;

        if (slugcatSettingPage is OnlineSlugcatSettingsBase settings)
        {
            ownerBoxSize = settings.settingsBoxSize;
            spacing = settings.spacing;
            margin = settings.margin;
            textSpacing = settings.textSpacing;
        }

        if (debug)
        {
            spacingRect = new("pixel", false){
                anchorX = 0f,
                alpha = 0.5f,
                color = Color.red,
            };
            Container.AddChild(spacingRect);
        }
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

        size.x = ownerBoxSize.x - (tab is null ? 0 : tabMargin);
        size.y = elementHeight;

        if (spacingRect is not null)
        {
            spacingRect.scaleX = size.x;
            spacingRect.scaleY = size.y;
            spacingRect.alpha = 0.25f * currentAlpha;
            spacingRect.x = DrawX(timeStacker);
            spacingRect.y = DrawY(timeStacker) + size.y/2;
        }
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        spacingRect?.RemoveFromContainer();
    }
}