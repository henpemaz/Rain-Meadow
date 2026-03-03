using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public static partial class SpecialEvents
    {
        public static AprilFools AprilFoolsEvent = new AprilFools()
        {
            Name = Utils.Translate("April Fool's"),
            StartMonth = 4,
            StartDay = 1,
            EndDay = 2,
        };

        public class AprilFools : Event
        {
            public override DialogNotify CreateDialogNotify(
                Menu.Menu self,
                string message,
                string okText
            )
            {
                DialogNotify dialog = new DialogNotify(message, self.manager, null);
                dialog.okButton.size = new Vector2(100f, 30f);
                dialog.okButton.menuLabel.text = okText;
                dialog.pos = new Vector2(dialog.size.x * 0.5f, 0);
                return dialog;
            }

            public override void UpdateLoginMessage(Menu.Menu self)
            {
                Dictionary<int, string> aprilMessages = new Dictionary<int, string>
                {
                    {
                        0,
                        RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0
                            ? "You need more Meadow Coins to play this game"
                            : "Game Over! Try again?"
                    },
                    { 1, "What's the secret password?" },
                    { 2, "I heard they were removing capes" },
                    { 3, "That crash was probably your fault" },
                    { 4, "Rain Meadow definitely failed to start" },
                };

                Dictionary<int, string> okMessage = new Dictionary<int, string>
                {
                    {
                        0,
                        RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0
                            ? "Take ¤10 Meadow Coins"
                            : $"Coins remaining: {RainMeadow.rainMeadowOptions.MeadowCoins.Value - 1}"
                    },
                    { 1, "wawa" },
                    { 2, "Good." },
                    { 3, "It was." },
                    { 4, "Please work" },
                };
                int result = UnityEngine.Random.Range(0, aprilMessages.Count);
                if (result == 0)
                {
                    RainMeadow.rainMeadowOptions.MeadowCoins.Value--;
                }
                if (RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0)
                {
                    result = 0;
                    GainedMeadowCoin(10);
                }
                string selectedMessage = self.Translate(aprilMessages[result]);
                self.manager.ShowDialog(
                    CreateDialogNotify(self, selectedMessage, okMessage[result])
                );
            }

            public void SpawnSnails(Room room, ShortcutHandler.ShortCutVessel shortCutVessel)
            {
                AbstractCreature bringTheSnails = new AbstractCreature(
                    room.world,
                    StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Snail),
                    null,
                    room.GetWorldCoordinate(shortCutVessel.pos),
                    shortCutVessel.room.world.game.GetNewID()
                );
                room.abstractRoom.AddEntity(bringTheSnails);
                bringTheSnails.Realize();
                bringTheSnails.realizedCreature.PlaceInRoom(room);

                room.world.GetResource().ApoEnteringWorld(bringTheSnails);
                room.abstractRoom.GetResource()
                    ?.ApoEnteringRoom(bringTheSnails, bringTheSnails.pos);
            }

            public void UpdateSlotsButton(
                ButtonScroller.ScrollerButton continueButton,
                ProcessManager manager
            )
            {
                if (RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0)
                {
                    continueButton.buttonBehav.greyedOut = true;
                }
                continueButton.menuLabel.text =
                    RainMeadow.rainMeadowOptions.MeadowCoins.Value > 0
                        ? continueButton.menu.Translate(
                            $"COINS: ¤{RainMeadow.rainMeadowOptions.MeadowCoins.Value}"
                        )
                        : continueButton.menu.Translate("YOU ARE POOR");
            }
        }
    }
}
