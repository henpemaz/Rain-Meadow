using System.Collections.Generic;

namespace RainMeadow
{
    public class Onslaught : ArenaOnlineGameMode
    {

        public static readonly string OnslaughtMode = "Onslaught";
        public Onslaught(ArenaOnlineGameMode arena) : base(arena.lobby)
        {
            avatarSettings = new SlugcatCustomization() { nickname = OnlineManager.mePlayer.id.name };
            arenaClientSettings = new ArenaClientSettings();
            arenaClientSettings.playingAs = SlugcatStats.Name.White;
        }


        public void ResetGameTimer()
        {
            setupTime = 0;
        }

        public void ResetViolence()
        {
            countdownInitiatedHoldFire = true;
            playerEnteredGame = 0;
        }

        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return false;
        }

        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.ArenaLobbyMenu;
        }


        public override bool ShouldSyncAPOInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return true;
        }
        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is WorldSession || onlineResource is RoomSession)
            {
                return lobby.owner == from;
            }
            return true;
        }

        public override bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {

            return true;
        }

        public override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            if (player == lobby.owner)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        public override bool AllowedInMode(PlacedObject item)
        {
            return base.AllowedInMode(item) || OnlineGameModeHelpers.PlayerGrabbableItems.Contains(item.type);
        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return roomSession.owner == null || roomSession.isOwner;
        }

        public override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);

            if (onlineResource is Lobby lobby)
            {
                lobby.AddData(new ArenaLobbyData());
            }
        }

        public override void AddClientData()
        {
            clientSettings.AddData(arenaClientSettings);
        }

        public override void ConfigureAvatar(OnlineCreature onlineCreature)
        {
            onlineCreature.AddData(avatarSettings);
        }

        public override void Customize(Creature creature, OnlineCreature oc)
        {
            if (oc.TryGetData<SlugcatCustomization>(out var data))
            {
                RainMeadow.Debug(oc);
                RainMeadow.creatureCustomizations.GetValue(creature, (c) => data);
            }
        }

       /* public override bool IsExitOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            foreach (var player in self.gameSession.arenaSitting.players)
            {
                if (player != null && (player.score > 0))
                {

                    return true;
                }
            }
            return false;

        }*/
    }
}
