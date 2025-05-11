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

            base.Draw(timeStacker);
        }

        public void UpdatePlayers()
        {
            var playerAvatars = OnlineManager.lobby.playerAvatars.Select(x => x.Value).ToList();
            var currentAvatars = indicators.Select(i => i.playerId).ToList();

            playerAvatars.Except(currentAvatars).Do(AvatarAdded);
            currentAvatars.Except(playerAvatars).Do(AvatarRemoved);
        }

        public void AvatarAdded(OnlineEntity.EntityId avatar)
        {
            RainMeadow.DebugMe();
            if (avatar.FindEntity() is not OnlineEntity entity) {
                RainMeadow.Error("Couldn't find online entity");
                return;
            }

            if (entity.owner is null) {
                RainMeadow.Error("Online Entity has no owner");
                return;
            }


            
            PlayerSpecificOnlineHud indicator = new(this, camera, onlineGameMode, OnlineManager.lobby.clientSettings[entity.owner], avatar);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
        }

        public void AvatarRemoved(OnlineEntity.EntityId avatar)
        {
            RainMeadow.DebugMe();
            var indicator = this.indicators.First(i => i.playerId == avatar);
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
