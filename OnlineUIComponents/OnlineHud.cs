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

        public int hudCounter;

        public OnlineHUD(HUD.HUD hud, RoomCamera camera, OnlineGameMode onlineGameMode) : base(hud)
        {
            this.camera = camera;
            this.onlineGameMode = onlineGameMode;
            UpdatePlayers();
        }

        public override void Draw(float timeStacker)
        {

            if (!RainMeadow.rainMeadowOptions.FriendViewClickToActivate.Value)
                RainMeadow.rainMeadowOptions.ShowFriends.Value = Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value);
            else if (Input.GetKeyDown(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
                RainMeadow.rainMeadowOptions.ShowFriends.Value ^= true;

                if (Input.GetKeyDown(KeyCode.P))
                {
                    RainMeadow.rainMeadowOptions.ShowPingLocation.Value += 1;
                }
                if (RainMeadow.rainMeadowOptions.ShowPingLocation.Value > 2)
                {
                    RainMeadow.rainMeadowOptions.ShowPingLocation.Value = 0;
                }

            base.Draw(timeStacker);
        }

        public void UpdatePlayers()
        {
            var activeAvatars = OnlineManager.lobby.playerAvatars.Select(kv => kv.Value.FindEntity(true) as OnlineCreature).Where(e => e != null);
            var currentAvatars = indicators.Select(i => i.onlinePlayer).ToList(); //needs duplication
            activeAvatars.Except(currentAvatars).Do(AvatarAdded);
            currentAvatars.Except(activeAvatars).Do(AvatarRemoved);
        }

        public void AvatarAdded(OnlineCreature avatar)
        {
            RainMeadow.DebugMe();
            if (avatar.owner is null) {
                RainMeadow.Error("Online Entity has no owner");
                return;
            }

            PlayerSpecificOnlineHud indicator = new(this, camera, onlineGameMode, OnlineManager.lobby.clientSettings[avatar.owner], avatar);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
        }

        public void AvatarRemoved(OnlineCreature avatar)
        {
            RainMeadow.DebugMe();
            var indicator = this.indicators.First(x => x.onlinePlayer == avatar);
            this.indicators.Remove(indicator);
            indicator.slatedForDeletion = true;
        }

        public override void Update()
        {
            base.Update();
            if (OnlineManager.lobby == null) return;
            UpdatePlayers();
            hudCounter++;
        }
    }
}
