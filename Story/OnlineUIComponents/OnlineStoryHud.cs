using HUD;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using HarmonyLib;

namespace RainMeadow
{
    public class OnlineHud : HudPart
    {
        private List<PlayerSpecificOnlineHud> indicators = new();

        private RoomCamera camera;
        private readonly OnlineGameMode onlineGameMode;

        public OnlineHud(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            UpdatePlayers();
        }

        public void UpdatePlayers()
        {
            List<ClientSettings> clientSettings = OnlineManager.lobby.clientSettings.Values.OfType<ClientSettings>().ToList();
            var currentSettings = indicators.Select(i => i.clientSettings);

            clientSettings.Except(currentSettings).Do(PlayerAdded); 
            currentSettings.Except(clientSettings).Do(PlayerRemoved);

        }

        public void PlayerAdded(ClientSettings clientSettings)
        {
            RainMeadow.DebugMe();
            PlayerSpecificOnlineHud indicator = new PlayerSpecificOnlineHud(hud, camera, onlineGameMode, clientSettings);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
        }

        public void PlayerRemoved(ClientSettings clientSettings)
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
