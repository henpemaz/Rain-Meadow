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
            public int DaysRemaining => EndDay - DateTime.UtcNow.Day;

            public bool IsActive =>
                DateTime.UtcNow.Month == StartMonth
                && DateTime.UtcNow.Day >= StartDay
                && DateTime.UtcNow.Day <= EndDay;

            public virtual void UpdateLoginMessage(Menu.Menu self)
            {
                int chanceToShowMessage = UnityEngine.Random.Range(0, 11);
                if (chanceToShowMessage > 5 && RainMeadow.rainMeadowOptions.MeadowCoins.Value > 0)
                {
                    return;
                }
                string m1 = self.Translate("Special event");
                string m2 = self.Translate("Days remaining");

                string message = $"{m1} {Name} {m2} {DaysRemaining}";

                self.manager.ShowDialog(CreateDialogNotify(self, message));

            }

            public virtual DialogNotify CreateDialogNotify(
                Menu.Menu self,
                string message,
                string okText = null
            )
            {
                DialogNotify dialog = new DialogNotify(message, self.manager, null);
                dialog.okButton.size = new Vector2(100f, 30f);
                dialog.pos = new Vector2(dialog.size.x * 0.5f, 0);
                return dialog;
            }
        }

        private static readonly Event[] AllEvents = { AprilFoolsEvent, AnniversaryEvent };
        public static bool IsSpecialEvent => AllEvents.Any(e => e.IsActive);
        public static bool IsSpecialEventInLobby => OnlineManager.lobby != null && OnlineManager.lobby.eventGags && AllEvents.Any(e => e.IsActive);

        public static Event? GetActiveEvent()
        {
            return AllEvents.FirstOrDefault(e => e.IsActive);
        }

        public static void LoadElement(string elementName)
        {
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
            if (self != null && self.AbstractPearl != null && self.AbstractPearl.dataPearlType != DataPearl.AbstractDataPearl.DataPearlType.Misc && self.AbstractPearl.dataPearlType != DataPearl.AbstractDataPearl.DataPearlType.Misc2)
            {
                orig(self, sLeaser, rCam);
                return;
            }

            SpecialEvents.LoadElement("meadowcoin");
            sLeaser.sprites = new FSprite[3];
            sLeaser.sprites[0] = new FSprite("meadowcoin");
            sLeaser.sprites[0].color = Color.yellow;
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

        public static bool CanSpendMeadowCoin(int coinsSpent)
        {
            return RainMeadow.rainMeadowOptions.MeadowCoins.Value >= coinsSpent;
        }

        public static bool SpendMeadowCoin(int coinsSpent)
        {
            if (CanSpendMeadowCoin(coinsSpent))
            {
                RainMeadow.rainMeadowOptions.MeadowCoins.Value -= coinsSpent;
                RainMeadow.rainMeadowOptions.config.Save();
                return true;
            }

            return false;
        }

        public static void PlayMeadowCoinSound(Menu.Menu menu)
        {
            menu.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, pan: 0f, vol: 2.0f, pitch: 2.0f);
            menu.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, pan: 0f, vol: 2.0f, pitch: 1.5f);
        }

        public static void PlayMeadowCoinSound(Room room)
        {
            room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, pan: 0f, vol: 2.0f, pitch: 2.0f);
            room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, pan: 0f, vol: 2.0f, pitch: 1.5f);
        }

        public static void PlayMeadowCoinSound(Room room, MeadowCollectToken token)
        {
            room.PlaySound(SoundID.HUD_Food_Meter_Fill_Plop_A, pos: token.pos, vol: 2.0f, pitch: 2.0f);
            room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, pos: token.pos, vol: 2.0f, pitch: 1.5f);
        }
    }
}
