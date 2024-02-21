using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace RainMeadow
{
    public class OnlineStoryHud : HudPart
    {
        private List<OnlinePlayerIndicator> indicators;

        public OnlineStoryHud(HUD.HUD hud) : base(hud)
        {
            UpdatePlayers();
        }

        public void UpdatePlayers()
        {
            List<StoryClientSettings> clientSettings = OnlineManager.lobby.entities.OfType<StoryClientSettings>().ToList();
            var currentSettings = indicators.Select(i => i.clientSettings);
            clientSettings.Except(currentSettings).Do(PlayerAdded);
            currentSettings.Except(clientSettings).Do(PlayerRemoved);
        }

        public void PlayerAdded(StoryClientSettings clientSettings)
        {
            OnlinePlayerIndicator indicator = new OnlinePlayerIndicator(hud, hud.fContainers[1], clientSettings);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
        }

        public void PlayerRemoved(StoryClientSettings clientSettings)
        {
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
