using Menu;
using Menu.Remix;
using RainMeadow.UI.Components;
using RainMeadow.UI.Interfaces;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static RainMeadow.UI.Dialogs.ModerationDialog.RecentPlayerDisplayer;

namespace RainMeadow.UI.Dialogs;

public class ModerationDialog : Dialog
{
    bool isClosed, queuedToClose;
    float desiredSelectPagePosY;
    public Page selectPage;
    public FLabel title, recentLabel, blockedLabel;

    public RecentPlayerDisplayer recentsDisplay, blockedDisplay;

    public ButtonScroller.ScrollerButton continueButton;

    public ModerationDialog(ProcessManager manager) : base(manager)
    {
        Vector2 center = UIUtils.ScreenCenter(manager);
        darkSprite.alpha = 0.85f;

        pages.Add(selectPage = new(this, null, "Moderation", 1));
        selectPage.pos.y = 1500;
        title = new(Custom.GetDisplayFont(), Translate("Moderation"))
        {
            scale = 1.7f,
            shader = manager.rainWorld.Shaders["MenuTextCustom"],
        };
        recentLabel = new(Custom.GetFont(), Translate("Recently played with"))
        {
            scale = 1.2f,
            alignment = FLabelAlignment.Center,
        };
        blockedLabel = new(Custom.GetFont(), Translate("Blocked"))
        {
            scale = 1.2f,
            alignment = FLabelAlignment.Center,
        };

        continueButton = new(this, selectPage, Translate("Close"),
            new(center.x - 60, center.y / 2 - 100), new(120, 30))
        { signalText = "CLOSE" };
        continueButton.GetScrollObject().LocalAlpha = 0;

        recentsDisplay = new(this, selectPage, new(center.x + 180, center.y - 190), false);
        blockedDisplay = new(this, selectPage, new(center.x - 430, center.y - 190), true);

        selectPage.Container.AddChild(title);
        selectPage.Container.AddChild(recentLabel);
        selectPage.Container.AddChild(blockedLabel);

        selectPage.SafeAddSubobjects(continueButton, recentsDisplay, blockedDisplay);
        selectPage.selectables.Add(continueButton);
    }

    public override void Update()
    {
        base.Update();
        if (selectPage.pos.y != desiredSelectPagePosY)
        {
            selectPage.pos.y = Custom.LerpAndTick(
                selectPage.pos.y,
                desiredSelectPagePosY,
                0.35f,
                0.1f
            );
            return;
        }
        else if (infoLabel != null)
        {
            infoLabel.text = Translate("Blocking a player will prevent them from joining any lobbies you host.");
            infoLabelFade = 1f;
        }
        var continueBtnScrollObj = continueButton.GetScrollObject();
        if (continueBtnScrollObj.LocalAlpha < 1)
        {
            continueBtnScrollObj.LocalAlpha += 0.05f;
            if (continueBtnScrollObj.LocalAlpha >= 1)
            {
                currentPage = 1;
                if (!manager.menuesMouseMode)
                    selectedObject = continueButton;
            }
            return;
        }
        if (queuedToClose)
        {
            manager.StopSideProcess(this);
            queuedToClose = false;
            isClosed = true;
        }
    }

    public override void GrafUpdate(float timeStacker)
    {
        base.GrafUpdate(timeStacker);
        Vector2 selectPagepos = selectPage.DrawPos(timeStacker);
        Vector2 center = UIUtils.ScreenCenter(manager);
        title.x = selectPagepos.x + manager.rainWorld.options.ScreenSize.x / 2;
        title.y = selectPagepos.y + manager.rainWorld.options.ScreenSize.y - 80;

        recentLabel.x = selectPagepos.x + center.x + 305;
        recentLabel.y = selectPagepos.y + center.y + 190;
        blockedLabel.x = selectPagepos.x + center.x - 305;
        blockedLabel.y = selectPagepos.y + center.y + 190;
    }

    public override void Singal(MenuObject sender, string message)
    {
        base.Singal(sender, message);
        if (message == "CLOSE") Close(SoundID.MENU_Remove_Level, true);
    }

    public void RefreshAll()
    {
        recentsDisplay.Refresh();
        blockedDisplay.Refresh();
    }

    public void Close(SoundID id, bool recieveSlugcat)
    {
        if (isClosed || queuedToClose)
            return;
        queuedToClose = true;
        PlaySound(id);
    }

    public class RecentPlayerDisplayer : ButtonScroller
    {
        public MenuTabWrapper tabWrapper;
        public List<RecentCard> recentCards;
        public bool isBlockList;

        public RecentPlayerDisplayer(Menu.Menu menu, MenuObject owner, Vector2 pos, bool isBlockList)
            : base(
            menu,
            owner,
            pos,
            6,
            250,
            (20, 20),
            sliderPosOffset: new(0, 10),
            sliderSizeAxisOffset: -40,
            startEndWithSpacing: true
        )
        {
            recentCards = new List<RecentCard>();

            greyOutWhenNoScroll = true;
            tabWrapper = new MenuTabWrapper(menu, this);
            AddScrollUpDownButtons(100, 34);
            CreateSideButtonLines();
            this.SafeAddSubobjects(tabWrapper);
            this.isBlockList = isBlockList;

            Refresh();
        }

        public void Refresh()
        {
            RemoveAllButtons();
            recentCards.Clear();
            var recents = isBlockList ? Moderation.blocked : Moderation.recents;
            foreach(var recent in recents)
            {
                recentCards.Add(new(menu, this, recent, isBlockList));
            }
            AddButtons(recentCards.ToArray());
        }

        public class RecentCard : ButtonScroller.ScrollerButton, IOwnMenuScrollObject {
            public PositionedSlugIcon slugIcon;
            public RecentPlayer recent;
            public bool isBlockList;
            public ModerationDialog parentDialog => (ModerationDialog)menu;

            public RecentCard(Menu.Menu menu, MenuObject owner, RecentPlayer recent, bool isBlockList = false)
                :
                base(
                    menu,
                    owner,
                    recent.name,
                    Vector2.zero,
                    new Vector2(250, 40),
                    description: menu.Translate($"Click to " + (isBlockList ? "unblock" : "block") + " <PLAYER>")
                    .Replace("<LOBBY>", recent.name))
            {
                this.isBlockList = isBlockList;
                OnClick += (btn) =>
                {
                    if (isBlockList)
                    {
                        Moderation.Unblock(recent);
                    }
                    else
                    {
                        if (Moderation.IsBlocked(recent)) return;
                        Moderation.Block(recent);
                    }
                    (owner.menu as ModerationDialog)?.RefreshAll();
                };

                slugIcon = new(menu, this, new(5, 7), recent.customization.slugcatClass, 
                    [recent.customization.Primary(), recent.customization.Secondary(), recent.customization.Special()], Moderation.IsBlocked(recent));

                this.SafeAddSubobjects(slugIcon);
            }

            public override void RemoveSprites()
            {
                slugIcon.ClearSprites();
                base.RemoveSprites();
            }
        }
    }
}
