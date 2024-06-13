using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace RainMeadow
{
    public class OnlineStoryHud : HudPart
    {
        private List<PlayerSpecificOnlineHud> indicators = new();

        private RoomCamera camera;
        private readonly StoryGameMode storyGameMode;

        public OnlineStoryHud(HUD.HUD hud, RoomCamera camera, StoryGameMode storyGameMode) : base(hud)
        {
            this.camera = camera;
            this.storyGameMode = storyGameMode;
            UpdatePlayers();
        }

        public void UpdatePlayers()
        {
            var clientSettings = OnlineManager.lobby.clientSettings.Values.OfType<StoryClientSettings>();
            var currentSettings = indicators.Select(i => i.clientSettings).ToList();

            clientSettings.Except(currentSettings).Do(PlayerAdded); 
            currentSettings.Except(clientSettings).Do(PlayerRemoved);

        }

        public void PlayerAdded(StoryClientSettings clientSettings)
        {
            RainMeadow.DebugMe();
            PlayerSpecificOnlineHud indicator = new PlayerSpecificOnlineHud(hud, camera, storyGameMode, clientSettings);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
        }

        public void PlayerRemoved(StoryClientSettings clientSettings)
        {
            RainMeadow.DebugMe();
            var indicator = this.indicators.First(i => i.clientSettings == clientSettings);
            this.indicators.Remove(indicator);
            indicator.slatedForDeletion = true;
        }

        public override void Update()
        {
            base.Update();
            UpdatePlayers();
        }
    }
}
