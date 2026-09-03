using Menu;
using UnityEngine;
using HarmonyLib;

namespace RainMeadow.UI.Components;
public readonly struct SettingsTabData
{
    public readonly SlugcatStats.Name? slugcatIcon;
    public readonly string? name;
    public readonly string? icon;
    public readonly Color? color;
    public readonly bool isClient;
    public SettingsTabData(SlugcatStats.Name slugcat, bool isClient = false)
    {
        slugcatIcon = slugcat;
        this.isClient = isClient;
    }
    public SettingsTabData(string name, SlugcatStats.Name slugcatIcon, Color color, bool isClient = false)
    {
        this.slugcatIcon = slugcatIcon;
        this.name = name;
        this.color = color;
        this.isClient = isClient;
    }
    public SettingsTabData(string name, string icon, Color color, bool isClient = false)
    {
        this.icon = icon;
        this.name = name;
        this.color = color;
        this.isClient = isClient;
    }

    public static bool operator== (SettingsTabData left, SettingsTabData right)
    {
        return left.name == right.name && left.slugcatIcon == right.slugcatIcon;
    }
    public static bool operator!= (SettingsTabData left, SettingsTabData right)
    {
        return !(left == right);
    }
    public override bool Equals(object obj)
    {
        return obj is SettingsTabData tab && tab == this;
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
public class OnlineSettingTab : OnlineSettingElement
{
    private const float iconSize = 24;
    private const float elementSpacing = 10;
    private const float selectedTweening = 0.3f;
    public readonly SettingsTabData data;
    public PositionedMenuObject icon;
    public MenuLabel label;
    public FSprite divider;
    public TabButton tabButton;
    public Color tabColor = Color.gray;
    public string name = "";
    public float selectedTween = 0f;
    public bool folded = false;

    public override MenuObject selectable => tabButton;

    public OnlineSettingTab(Menu.Menu menu, MenuObject owner, SettingsTabData data)
         : base(menu, owner, null)
    {
        this.data = data;
        isClient = data.isClient;

        if (data.slugcatIcon is not null)
        {
            icon = new PositionedSlugIcon(menu, this, Vector2.zero, data.slugcatIcon.value);
        }
        else
        {
            icon = new MenuIllustration(menu, this, "", data.icon, Vector2.zero, true, true);
        }

        if (data.name is not null && data.color is not null)
        {
            name = data.name;
            tabColor = (Color)data.color;
        }
        else if (data.slugcatIcon is not null)
        {
            name = SlugcatStats.getSlugcatName(data.slugcatIcon);
            tabColor = Color.HSVToRGB(
                PlayerGraphics.DefaultSlugcatColor(data.slugcatIcon).ToHSL().hue,
                PlayerGraphics.DefaultSlugcatColor(data.slugcatIcon).ToHSL().saturation < 0.1f ? 0 : 0.75f,
                1f
            );
        }

        label = new(
            menu,
            this,
            menu.Translate(name),
            Vector2.zero,
            new(textSpacing, elementHeight),
        true);

        divider = new("pixel", false){
            anchorX = 0f,
            scaleX = 200f,
            scaleY = 2f,
            color = tabColor,
        };

        tabButton = new(menu, this, elementHeight);

        this.SafeAddSubobjects(icon, label, tabButton);
        Container.AddChild(divider);
    }

    public override void Update()
    {
        base.Update();

        tabButton.pos = Vector2.left * Mathf.Min(margin - 5, tabButton.size.x);
        icon.pos = tabButton.pos + Vector2.right * tabButton.size.x;
        label.pos = icon.pos + Vector2.right * (iconSize + elementSpacing);
        label.size.x = label.label.textRect.width + elementSpacing;

        if (selectable.Selected)
        {
            selectedTween = Mathf.Lerp(selectedTween, 1, selectedTweening);
        }
        else
        {
            selectedTween = Mathf.Lerp(selectedTween, 0, selectedTweening * 2);
        }
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        if (icon is MenuIllustration illustration)
        {
            illustration.sprite.isVisible = visible;
            illustration.sprite.alpha = currentAlpha;
        }
        else if (icon is PositionedSlugIcon slugicon)
        {
            slugicon.slugIcon.sprites.Do(x =>
            {
                x.isVisible = visible;
                x.alpha = currentAlpha;
            });
        }

        label.label.alpha = (grayedOut ? 0.5f : 1f) * currentAlpha;

        tabButton.arrowSprite.alpha = (grayedOut ? 0.25f : folded ? 0.5f : 0.75f) * currentAlpha;

        float spacing = label.pos.x + label.size.x + elementSpacing;
        divider.isVisible = visible;
        divider.alpha = (grayedOut ? 0.5f : 1f) * currentAlpha;
        divider.x = DrawX(timeStacker) + spacing;
        divider.y = DrawY(timeStacker) + elementSize.y/2f;
        divider.scaleX = Mathf.Max(0, settingsBoxSize.x - spacing);
        divider.scaleY = 2f + 2f * selectedTween;
        divider.color = Color.Lerp(tabColor, Color.white, selectedTween * 0.35f);
    }
    public override void Singal(MenuObject sender, string message)
    {
        if (sender == tabButton)
        {
            folded = !folded;
            menu.PlaySound(folded ? SoundID.MENU_Checkbox_Uncheck : SoundID.MENU_Checkbox_Check);
        }
    }
    public override void RemoveSprites()
    {
        base.RemoveSprites();
        divider?.RemoveFromContainer();
    }

    public class TabButton : BigSimpleButton
    {
        public FSprite arrowSprite;
        public OnlineSettingTab settingTab => (owner as OnlineSettingTab)!;
        public TabButton(Menu.Menu menu, OnlineSettingTab owner, float size)
             : base(menu, owner, "", "", Vector2.zero, new Vector2(size, size), FLabelAlignment.Left, false)
        {
            roundedRect.RemoveSprites();
            selectRect.RemoveSprites();
            arrowSprite = new("Menu_Symbol_Arrow")
            {
                rotation = 180,
                anchorX = 0.5f,
                anchorY = 0.5f,
                scale = 0.5f
            };
            Container.AddChild(arrowSprite);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            arrowSprite.x = DrawX(timeStacker) + size.y/2f;
            arrowSprite.y = DrawY(timeStacker) + size.y/2f;
            arrowSprite.rotation = !settingTab.folded ? 180 : 90;
            arrowSprite.color = MyColor(timeStacker);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            arrowSprite.RemoveFromContainer();
        }
    }
}