using Menu;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowPauseMenu : PauseMenu
    {
        private readonly MeadowGameMode mgm;
        private Creature avatarCreature;
        public MeadowPauseMenu(ProcessManager manager, RainWorldGame game, MeadowGameMode mgm) : base(manager, game)
        {
            this.continueButton.RemoveSprites();
            this.pages[0].RemoveSubObject(this.continueButton);
            this.continueButton = null;
            this.exitButton.RemoveSprites();
            this.pages[0].RemoveSubObject(this.exitButton);
            this.exitButton = null;

            this.mgm = mgm;
            this.avatarCreature = mgm.avatar.realizedCreature;

            this.pauseWarningActive = false;
            game.cameras[0].hud.textPrompt.pausedWarningText = false;
            game.cameras[0].hud.textPrompt.pausedMode = false;

            int buttonCount = 0;
            SimplerButton AddButton(string localizedText, string localizedDescription, Action<SimplerButton> onClick)
            {
                Vector2 pos = new Vector2(
                    this.ContinueAndExitButtonsXPos - 250.2f - this.moveLeft - this.manager.rainWorld.options.SafeScreenOffset.x, 
                    Mathf.Max(manager.rainWorld.options.SafeScreenOffset.y, 15f) + 540.2f
                );
                pos.y -= (buttonCount) * 40f;
                SimplerButton button = new SimplerButton(this, this.pages[0], localizedText, pos, new Vector2(110f, 30f), localizedDescription);
                button.OnClick += onClick;
                button.nextSelectable[0] = button;
                button.nextSelectable[2] = button;
                this.pages[0].subObjects.Add(button);
                buttonCount += 1;
                return button;
            }
            this.pages[this.currentPage].lastSelectedObject = AddButton(this.Translate("CONTINUE"), this.Translate("Close this menu"), this.Continue);
            AddButton(this.Translate("TO HUB"), this.Translate("Teleport to the closest hub"), this.ToHub);
            AddButton(this.Translate("TO OUTSKIRTS"), this.Translate("Teleport to outskirts"), this.ToOutskirts);
            AddButton(this.Translate("PASSAGE"), this.Translate("Passage to another shelter"), this.Passage);
            AddButton(this.Translate("UNSTUCK"), this.Translate("Teleport to a nearby pipe"), this.Unstuck);
            AddButton(this.Translate("LOBBY"), this.Translate("Go back to the charater selection screen"), this.ToLobby);
            AddButton(this.Translate("QUIT"), this.Translate("Exit back to the main menu"), this.ToMainMenu);

            //this.blackSprite.scaleX = manager.rainWorld.options.ScreenSize.x / 4f;
        }

        private void Continue(SimplerButton button)
        {
            this.wantToContinue = true;
            base.PlaySound(SoundID.HUD_Unpause_Game);
        }

        private void ToMainMenu(SimplerButton button)
        {
            base.PlaySound(SoundID.HUD_Exit_Game);
            this.game.ExitToMenu();
            this.ShutDownProcess();
        }

        private void ToLobby(SimplerButton button)
        {
            base.PlaySound(SoundID.HUD_Exit_Game);
            manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowMenu);
            this.ShutDownProcess();
        }

        private void Passage(SimplerButton button)
        {
            // untested
            this.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.FastTravelScreen);
            base.PlaySound(SoundID.MENU_Passage_Button);
        }

        private void ToOutskirts(SimplerButton button)
        {
            throw new NotImplementedException();
        }

        private void Unstuck(SimplerButton button)
        {
            var creature = mgm.avatar.realizedCreature;
            if (creature.room != null)
            {
                var room = creature.room;
                creature.RemoveFromRoom();
                room.CleanOutObjectNotInThisRoom(creature); // we need it this frame
                var node = creature.coord.abstractNode;
                if (node > room.abstractRoom.exits) node = UnityEngine.Random.Range(0, room.abstractRoom.exits);
                creature.SpitOutOfShortCut(room.ShortcutLeadingToNode(node).startCoord.Tile, room, true);
            }
        }

        private void ToHub(SimplerButton button)
        {
            var creature = mgm.avatar.realizedCreature;
            if (creature.room != null)
            {
                var room = creature.room;
                creature.RemoveFromRoom();
                room.CleanOutObjectNotInThisRoom(creature); // we need it this frame
            } else if (!creature.RemoveFromShortcuts())
            {
                return; // not found?
            }

            // todo

        }

        public override string UpdateInfoText()
        {
            if (this.selectedObject is IHaveADescription ihad)
            {
                return ihad.Description;
            }
            return base.UpdateInfoText();
        }
    }
}