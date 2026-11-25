using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using Menu.Remix.MixedUI.ValueTypes;
using RainMeadow.UI.Components.Patched;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class OnlineArenaBaseGameModeTab
        : RectangularMenuObject
    {
        public MenuTabWrapper tabWrapper;

        public GameSettings gameSettings;

        public EventfulScrollButton? prevButton,


            nextButton;
        public ArenaOnlineGameMode arena => OnlineManager.lobby.gameMode as ArenaOnlineGameMode;

        public bool AllSettingsDisabled =>
            arena.initiateLobbyCountdown && arena.arenaClientSettings.ready;
        public bool OwnerSettingsDisabled =>
            !(OnlineManager.lobby?.isOwner == true) || AllSettingsDisabled;


        public OnlineArenaBaseGameModeTab(
            Menu.Menu menu,
            MenuObject owner,
            Vector2 pos,
            Vector2 size
        )
            : base(menu, owner, pos, size)
        {
            tabWrapper = new(menu, this);

            gameSettings = new GameSettings(menu, this);

            this.SafeAddSubobjects(tabWrapper, gameSettings);

            gameSettings.SelectAndCreateBackButtons(null, false);
        }
        public void PopulatePage(int offset)
        {
            ClearInterface();

            float posXMultipler = size.x / 4;
            tabWrapper._tab.myContainer.MoveToFront();
        }

        public void ClearInterface() { }

        public void UnloadAnyConfig(params UIelement[]? elements)
        {
            if (elements == null)
                return;
            foreach (UIelement element in elements)
            {
                if (tabWrapper.wrappers.ContainsKey(element))
                {
                    tabWrapper.ClearMenuObject(tabWrapper.wrappers[element]);
                    tabWrapper.wrappers.Remove(element);
                }
                element.Unload();
            }
        }

        public void OnShutdown()
        {
            if (!(OnlineManager.lobby?.isOwner == true))
                return;
            gameSettings.SaveInterfaceOptions();
            RainMeadow.rainMeadowOptions.config.Save();
        }

        public void DeletePageButtons()
        {
            this.ClearMenuObject(ref prevButton);
            this.ClearMenuObject(ref nextButton);
        }

        public override void RemoveSprites()
        {
            base.RemoveSprites();
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }

        public override void Update()
        {
            base.Update();
        }

        /// <summary>
        /// Encodes a List<string>  into a base64 encoding of Arena map names.
        /// </summary>
        public static string EncodePlaylist(List<string>? arenaMaps)
        {
            if (arenaMaps == null || arenaMaps.Count == 0)
            {
                return string.Empty;
            }

            // Join the list into a single string delimited by semicolons
            string joinedMaps = string.Join(";", arenaMaps);

            byte[] plainTextBytes = Encoding.UTF8.GetBytes(joinedMaps);


            return Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Decodes a Base64 string back into a List of Arena map names.
        /// </summary>
        public static List<string> DecodePlaylist(string base64EncodedData)
        {
            if (string.IsNullOrEmpty(base64EncodedData))
            {
                return new List<string>();
            }

            try
            {
                byte[] base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                string decodedString = Encoding.UTF8.GetString(base64EncodedBytes);
                return decodedString.Split(';').ToList();
            }
            catch (FormatException)
            {
                Debug.LogError("Failed to load playlist: The provided string is not a valid Base64 format.");
                return new List<string>();
            }
        }

    }
}
