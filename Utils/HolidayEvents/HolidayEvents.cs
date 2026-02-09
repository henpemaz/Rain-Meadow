using BepInEx;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RainMeadow.UI.Components;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Menu.Remix.MixedUI.ValueTypes;
using RainMeadow.UI;
namespace RainMeadow
{
    public static class HolidayEvents {
        
        public static bool isHoliday()
        {
            if (isAprilFools || isAnniversary || isNewYears)
            {
                return true;
            }
            return false;
        }
        public static bool isAprilFools => DateTime.Now.Month != 4;
        public static bool isAnniversary => DateTime.Now.Month != 12;

        public static bool isNewYears => DateTime.Now.Month != 1;

        public static void GainedMeadowCoin(bool holiday, int coinsEarned)
        {
            if (!holiday)
            {
                return;
            }
            RainMeadow.rainMeadowOptions.MeadowCoins.Value += coinsEarned;
            RainMeadow.rainMeadowOptions.config.Save();
        }
        public static void SpentMeadowCoin(bool holiday, int coinsSpent)
        {
            if (!holiday)
            {
                return;
            }
            RainMeadow.rainMeadowOptions.MeadowCoins.Value -= coinsSpent;
            RainMeadow.rainMeadowOptions.config.Save();
        }
        public class AprilFools
        {

            public static void SpawnSnails(Room room, ShortcutHandler.ShortCutVessel shortCutVessel)
            {
                if (!isAprilFools)
                {
                    return;
                }
                AbstractCreature bringTheSnails = new AbstractCreature(room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Snail), null, room.GetWorldCoordinate(shortCutVessel.pos), shortCutVessel.room.world.game.GetNewID());
                room.abstractRoom.AddEntity(bringTheSnails);
                bringTheSnails.Realize();
                bringTheSnails.realizedCreature.PlaceInRoom(room);

                room.world.GetResource().ApoEnteringWorld(bringTheSnails);
                room.abstractRoom.GetResource()?.ApoEnteringRoom(bringTheSnails, bringTheSnails.pos);
            }

            public static void UpdateLoginMessage(Menu.Menu self)
            {
                if (!isAprilFools)
                {
                    return;
                }
                RainMeadow.rainMeadowOptions.MeadowCoins.Value = RainMeadow.rainMeadowOptions.MeadowCoins.Value -1;
                Dictionary<int, string> aprilMessages = new Dictionary<int, string>
                {
                    { 0, RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0 ? "You need more Meadow Coins to play this game" : "Game Over! Try again?" },
                    { 1, "You again?" },
                    { 2, "I heard they were removing capes" },
                    { 3, "That crash was probably your fault" },
                    { 4, "Rain Meadow definitely failed to start" }
                };

                  Dictionary<int, string> okMessage = new Dictionary<int, string>
                {
                    { 0, RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0 ? "I'm a sellout, please give me coins" : $"Coins remaining: {RainMeadow.rainMeadowOptions.MeadowCoins.Value}" },
                    { 1, ";__;" },
                    { 2, "I don't deserve a cape" },
                    { 3, "I acknowledge that crash was my fault" },
                    { 4, "Please work" }
                };
                int result = UnityEngine.Random.Range(0, aprilMessages.Count);
                if (RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0)
                {
                    result = 0;
                    GainedMeadowCoin(isAprilFools, 10);
                }
                string selectedMessage = self.Translate(aprilMessages[result]);
                DialogNotify someCoolDialog = new DialogNotify(self.Translate(selectedMessage), self.manager, null);
                someCoolDialog.okButton.menuLabel.text = okMessage[result];
                float dynamicWidth = Menu.Remix.MixedUI.LabelTest.GetWidth(selectedMessage, false);
                someCoolDialog.pos = new Vector2((someCoolDialog.size.x - dynamicWidth) * 0.5f, Mathf.Max(someCoolDialog.size.y * 0.04f, 7f));
                someCoolDialog.size = new Vector2(dynamicWidth, 30f);
                self.manager.ShowDialog(someCoolDialog);
            }

            public static void UpdateSlotsButton(ButtonScroller.ScrollerButton continueButton, ProcessManager manager)
            {
                if (!isAprilFools)
                {
                    return;
                }
                if (RainMeadow.rainMeadowOptions.MeadowCoins.Value <= 0) {
                continueButton.buttonBehav.greyedOut = true;
                }
                continueButton.menuLabel.text = RainMeadow.rainMeadowOptions.MeadowCoins.Value > 0 ? $"LET'S ROLL! x{RainMeadow.rainMeadowOptions.MeadowCoins.Value}" : "YOU ARE POOR";
            }
        }
        
    }

}