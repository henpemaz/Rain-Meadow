using System;
using System.Linq;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components.Patched;
using UnityEngine;

namespace RainMeadow.UI.Menus.Panels;

class LobbySelectNetworkPanel : PositionedMenuObject
{
    public event Action? OnDirectConnectButtonClick;
    public event Action? OnDomainChanged;

    public SimplerButton directConnectButton;

    public LobbySelectNetworkPanel(Menu.Menu menu, MenuObject owner, Vector2 pos)
        : base(menu, owner, pos)
    {
        MenuTabWrapper tabWrapper = new(menu, this);

        directConnectButton = new SimplerButton(
            menu,
            this,
            menu.Translate("Direct Connect"),
            new Vector2(20, 445),
            new Vector2(160f, 30f),
            menu.Translate("Directly connect to a local IP")
        );
        directConnectButton.buttonBehav.greyedOut =
            MatchmakingManager.currentDomain == MatchmakingManager.MatchMakingDomain.LAN;
        directConnectButton.OnClick += (btn) => OnDirectConnectButtonClick?.Invoke();

        ProperlyAlignedMenuLabel domainLabel = new(
            menu,
            this,
            menu.Translate("Lobby Domain"),
            new Vector2(20, 415),
            new Vector2(200f, 20f),
            false
        );

        OpComboBox2 domainComboBox = new(
            new Configurable<MatchmakingManager.MatchMakingDomain>(
                MatchmakingManager.currentDomain
            ),
            new Vector2(20, 385),
            125f,
            [
                .. MatchmakingManager.supported_matchmakers.Select(x => new ListItem(
                    x.value,
                    menu.Translate(x.value)
                )),
            ]
        );
        domainComboBox.OnChange += () =>
        {
            MatchmakingManager.currentDomain = new MatchmakingManager.MatchMakingDomain(
                domainComboBox.value,
                false
            );
            OnDomainChanged?.Invoke();
        };

        new PatchedUIelementWrapper(tabWrapper, domainComboBox);
        subObjects.AddRange([tabWrapper, directConnectButton, domainLabel]);
    }

    public override void Update()
    {
        base.Update();
        directConnectButton.buttonBehav.greyedOut =
            MatchmakingManager.currentDomain != MatchmakingManager.MatchMakingDomain.LAN;
    }
}
