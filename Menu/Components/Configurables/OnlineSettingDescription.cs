using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components.Configurables;

public class OnlineSettingDescription : OnlineSettingElement, IUpdateWithSlugcatSetting
{
    public const string LINEBREAK = "<LINE>";
    public const float textMargin = -7.5f;
    public const float descButtonMargin = 5f;
    public static readonly Color defaultColor = Color.Lerp(MenuColorEffect.rgbMediumGrey, MenuColorEffect.rgbDarkGrey, 0.5f);

    public override MenuObject selectable => descButton as MenuObject ?? descriptionLabel;
    public override int additionalPositionsTaken 
        => Mathf.Max(0, ((int)descriptionLabel.label.textRect.height / (int)size.y) - 1);

    public MenuLabel descriptionLabel;
    public Color color = defaultColor;

    public DescButton? descButton;
    public bool folded = false;

    public OnlineSettingDescription(Menu.Menu menu, OnlineSlugcatSettingsBase owner, string description, OnlineSettingElement? parent, OnlineSettingTab? tab = null)
         : this(menu, owner.scroller, description, parent, tab) {}
    public OnlineSettingDescription(Menu.Menu menu, MenuObject owner, string description, OnlineSettingElement? parent, OnlineSettingTab? tab = null)
         : base(menu, owner, parent?.tab ?? tab)
    {
        descriptionLabel = new(
            menu,
            this,
            menu.Translate(description).Replace(LINEBREAK, "\n"),
            Vector2.zero,
            size,
            false
        );
        descriptionLabel.label.alignment = FLabelAlignment.Left;

        if (parent is not null)
        {
            folded = true;
            descButton = new(menu, this, parent, elementHeight);
            parent.SafeAddSubobjects(descButton);
            descButton.OnClick += (button) => ToggleDescription();
        }

        this.SafeAddSubobjects(descriptionLabel);
    }

    public void ToggleDescription()
    {
        if (descButton is not null)
        {
            folded = !folded;
            menu.PlaySound(folded ? SoundID.MENU_Checkbox_Uncheck : SoundID.MENU_Checkbox_Check);
        }
    }
    public override void Update()
    {
        base.Update();
        
        descButton?.pos = Vector2.left * (descButton.size.x + descButtonMargin);
        descriptionLabel.pos = new Vector2(-size.x/2 + textMargin, -descriptionLabel.label.textRect.height/2 + descriptionLabel.label.FontLineHeight/2);
    }
    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        descriptionLabel.label.color = color;
        descriptionLabel.label.alpha = currentAlpha * (grayedOut ? 0.75f : 1);
        descriptionLabel.label.isVisible = visible;
    }

    public void OnUpdateVisibility()
    {
        if (folded) visible = false;
    }
    public void OnUpdatePosition() {}

    public class DescButton : SimplerButton
    {
        private const float rotTween = 0.2f;
        private const float symbolSize = 11f;
        public FSprite openSprite;
        public FSprite closeSprite;
        private float rotation;
        private float lastRotation;
        public OnlineSettingDescription settingDesc;
        public OnlineSettingElement parent => (owner as OnlineSettingElement)!;
        public DescButton(Menu.Menu menu, OnlineSettingDescription settingDesc, OnlineSettingElement owner, float size)
             : base(menu, owner, "", Vector2.zero, new Vector2(size, size))
        {
            roundedRect.RemoveSprites();
            selectRect.RemoveSprites();
            this.settingDesc = settingDesc;
            openSprite = new("pixel", false)
            {
                rotation = 0,
                anchorX = 0.5f,
                anchorY = 0.5f,
                scaleX = symbolSize,
                scaleY = 1f,
            };
            closeSprite = new("pixel", false)
            {
                rotation = 0,
                anchorX = 0.5f,
                anchorY = 0.5f,
                scaleX = 1f,
                scaleY = symbolSize,
            };
            Container.AddChild(openSprite);
            Container.AddChild(closeSprite);
        }
        public override void Update()
        {
            base.Update();
            lastRotation = rotation;

            if (settingDesc.folded) rotation = Mathf.Lerp(rotation, 90, rotTween);
            else rotation = Mathf.Lerp(rotation, 0, rotTween);
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            
            openSprite.x = DrawX(timeStacker) + size.y/2f;
            openSprite.y = DrawY(timeStacker) + size.y/2f;
            openSprite.scaleY = settingDesc.folded ? 2f : 1f;
            openSprite.color = MyColor(timeStacker);
            openSprite.rotation = Mathf.Lerp(lastRotation, rotation, timeStacker);
            openSprite.isVisible = parent.visible && !settingDesc.folded;

            closeSprite.x = DrawX(timeStacker) + size.y/2f;
            closeSprite.y = DrawY(timeStacker) + size.y/2f;
            closeSprite.scaleX = settingDesc.folded ? 2f : 1f;
            closeSprite.color = MyColor(timeStacker);
            closeSprite.rotation = Mathf.Lerp(lastRotation, rotation, timeStacker);
            closeSprite.isVisible = parent.visible;
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            openSprite.RemoveFromContainer();
            closeSprite.RemoveFromContainer();
        }
    }

}