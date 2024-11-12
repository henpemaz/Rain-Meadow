using HarmonyLib;
using HUD;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class OnlineHUD : HudPart
    {
        private List<PlayerSpecificOnlineHud> indicators = new();

        private RoomCamera camera;
        private readonly OnlineGameMode onlineGameMode;

        public OnlineHUD(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            UpdatePlayers();
        }

        public override void Draw(float timeStacker)
        {
            if (!ChatHud.chatButtonActive)
            {
                if (!RainMeadow.rainMeadowOptions.FriendViewClickToActivate.Value)
                    RainMeadow.rainMeadowOptions.ShowFriends.Value = Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value);
                else if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
                    RainMeadow.rainMeadowOptions.ShowFriends.Value ^= true;
            }

            base.Draw(timeStacker);
        }

        public void UpdatePlayers()
        {
            var clientSettings = OnlineManager.lobby.clientSettings.Values.OfType<ClientSettings>();
            var currentSettings = indicators.Select(i => i.clientSettings).ToList();

            clientSettings.Except(currentSettings).Do(PlayerAdded);
            currentSettings.Except(clientSettings).Do(PlayerRemoved);
        }

        public void PlayerAdded(ClientSettings clientSettings)
        {
            RainMeadow.DebugMe();
            PlayerSpecificOnlineHud indicator = new PlayerSpecificOnlineHud(this, camera, onlineGameMode, clientSettings);
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
