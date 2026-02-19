using System;
using System.Linq;
using Menu;
using UnityEngine;

namespace RainMeadow
{
    public static partial class SpecialEvents
    {
        public class Event
        {
            public string Name;
            public int StartMonth;
            public int StartDay;
            public int EndDay;

            public bool IsActive =>
                DateTime.UtcNow.Month == StartMonth
                && DateTime.UtcNow.Day >= StartDay
                && DateTime.UtcNow.Day <= EndDay;

            public virtual void UpdateLoginMessage(Menu.Menu self)
            {
                int daysLeft = EndDay - DateTime.UtcNow.Day;
                string message = self.Translate(
                    $"Special event: {Name}. Days remaining: {daysLeft}"
                );

                self.manager.ShowDialog(CreateDialogNotify(self, message));
            }

            public DialogNotify CreateDialogNotify(Menu.Menu self, string message)
            {
                DialogNotify dialog = new DialogNotify(message, self.manager, null);
                dialog.okButton.size = new Vector2(100f, 30f);
                dialog.pos = new Vector2(dialog.size.x * 0.5f, 0);
                return dialog;
            }
        }

        private static readonly Event[] AllEvents = { AprilFoolsEvent, AnniversaryEvent };
        public static bool IsSpecialEvent => AllEvents.Any(e => e.IsActive);

        public static Event? GetActiveEvent()
        {
            return AllEvents.FirstOrDefault(e => e.IsActive);
        }

        public static void LoadElement(string elementName)
        {
            if (!SpecialEvents.IsSpecialEvent)
            {
                return;
            }
            if (Futile.atlasManager.GetAtlasWithName(elementName) != null)
            {
                return;
            }
            string text = AssetManager.ResolveFilePath(
                "Illustrations"
                    + System.IO.Path.DirectorySeparatorChar.ToString()
                    + elementName
                    + ".png"
            );
            Texture2D texture2D = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            AssetManager.SafeWWWLoadTexture(ref texture2D, "file:///" + text, false, true);
            Futile.atlasManager.LoadAtlasFromTexture(elementName, texture2D, false);
        }

        /// <summary>
        /// Override DataPearls with Meadow Coins!
        /// </summary>
        public static void DataPearl_InitiateSprites(
            On.DataPearl.orig_InitiateSprites orig,
            DataPearl self,
            RoomCamera.SpriteLeaser sLeaser,
            RoomCamera rCam
        )
        {
            if (!IsSpecialEvent)
            {
                orig(self, sLeaser, rCam);
                return;
            }
            SpecialEvents.LoadElement("meadowcoin");
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite("meadowcoin");
            sLeaser.sprites[0].scale = 0.05f;
            sLeaser.sprites[1] = new FSprite("tinyStar");
            sLeaser.sprites[2] = new FSprite("Futile_White");
            sLeaser.sprites[2].shader = rCam.game.rainWorld.Shaders["FlatLightBehindTerrain"];
            self.AddToContainer(sLeaser, rCam, null);
        }

        public static void GainedMeadowCoin(int coinsEarned)
        {
            RainMeadow.rainMeadowOptions.MeadowCoins.Value += coinsEarned;
            RainMeadow.rainMeadowOptions.config.Save();
        }

        public static void SpentMeadowCoin(int coinsSpent)
        {
            RainMeadow.rainMeadowOptions.MeadowCoins.Value -= coinsSpent;
            RainMeadow.rainMeadowOptions.config.Save();
        }
    }
}
