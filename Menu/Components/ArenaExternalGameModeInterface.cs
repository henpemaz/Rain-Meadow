using ArenaMode = RainMeadow.ArenaOnlineGameMode;
using System.Collections.Generic;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;
using System.Linq;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;

namespace RainMeadow.UI.Components
{
    public class OnlineArenaExternalGameModeSettingsInterface : PositionedMenuObject, CheckBox.IOwnCheckBox, MultipleChoiceArray.IOwnMultipleChoiceArray
    {
        public ArenaSetup GetArenaSetup => menu.manager.arenaSetup;
        public ArenaSetup.GameTypeSetup GetGameTypeSetup => GetArenaSetup.GetOrInitiateGameTypeSetup(GetArenaSetup.currentGameType);
        public OnlineArenaExternalGameModeSettingsInterface(ArenaOnlineGameMode arena, Menu.Menu menu, MenuObject owner, Vector2 pos, List<ListItem> listItems, float settingsWidth = 300) : base(menu, owner, pos)
        {
            tabWrapper = new(menu, this);
            if (GetGameTypeSetup.gameType != ArenaSetup.GameTypeID.Competitive)
            {
                RainMeadow.Error("THIS IS NOT COMPETITIVE MODE!");
            }

            arena.onlineArenaGameMode.ArenaExternalGameModeSettingsInterface_ctor(arena, this, menu, owner, tabWrapper, pos);

        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 pos = DrawPos(timeStacker);

        }
        public override void Update()
        {
            if (tabWrapper.IsAllRemixUINotHeld() && tabWrapper.holdElement) tabWrapper.holdElement = false;
            base.Update();
            if (RainMeadow.isArenaMode(out ArenaMode arena))
            {
                arena.onlineArenaGameMode.ArenaExternalGameModeSettingsInterface_Update(arena, this, menu, owner, tabWrapper, pos);

            }
        }
        public bool GetChecked(CheckBox box)
        {

            return false;
        }
        public void SetChecked(CheckBox box, bool c)
        {

        }
        public int GetSelected(MultipleChoiceArray array)
        {

            return 0;
        }
        public void SetSelected(MultipleChoiceArray array, int i)
        {
        }
        public void CallForSync() //call this after ctor if needed for sync at start
        {
        }

        public bool gameModeComboBoxLastHeld;
        public bool teamComboBoxLastHeld;

        public Vector2[] divSpritePos;
        public FSprite[] divSprites;

        public UIelementWrapper externalModeWrapper;
        public MenuTabWrapper tabWrapper;

    }
}
