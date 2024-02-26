using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class StoryGameMode : OnlineGameMode
    {
        public List<ushort> readyForWinPlayers = new List<ushort>();

        // these are synced by StoryLobbyData
        public bool didStartGame = false;
        public bool didStartCycle = false;
        public string defaultDenPos;
        public SlugcatStats.Name currentCampaign;

        public StoryClientSettings storyClientSettings => clientSettings as StoryClientSettings;

        public StoryGameMode(Lobby lobby) : base(lobby)
        {

        }
        public override ProcessManager.ProcessID MenuProcessId()
        {
            return RainMeadow.Ext_ProcessID.StoryMenu;
        }
        public override bool AllowedInMode(PlacedObject item)
        {
            return base.AllowedInMode(item) || OnlineGameModeHelpers.PlayerGrablableItems.Contains(item.type);
        }
        public override bool ShouldLoadCreatures(RainWorldGame game, WorldSession worldSession)
        {
            return worldSession.owner == null || worldSession.isOwner;
        }

        public override bool ShouldSpawnRoomItems(RainWorldGame game, RoomSession roomSession)
        {
            return roomSession.owner == null || roomSession.isOwner;
            // todo if two join at once, this first check is faulty
        }

        public override bool ShouldSyncObjectInWorld(WorldSession ws, AbstractPhysicalObject apo)
        {
            return true;
        }

        public override SlugcatStats.Name LoadWorldAs(RainWorldGame game)
        {
            if (currentCampaign == RainMeadow.Ext_SlugcatStatsName.OnlineStoryYellow) 
            {
                return SlugcatStats.Name.Yellow;
            }
            else if (currentCampaign == RainMeadow.Ext_SlugcatStatsName.OnlineStoryWhite) 
            {
                return SlugcatStats.Name.White;
            }
            else if (currentCampaign == RainMeadow.Ext_SlugcatStatsName.OnlineStoryRed) 
            {
                return SlugcatStats.Name.Red;
            }
            else
            {
                return SlugcatStats.Name.White;
            }
        }

        public override bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            return true;
        }

        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource)
        {
            if (onlineResource is WorldSession)
            {
                return lobby.owner == from;
            }
            return true;
        }

        public override void LobbyReadyCheck()
        {
            if (lobby.isOwner && !didStartGame)
            {
                didStartGame = true;
                currentCampaign = storyClientSettings.playingAs;
            }
        }

        internal override void PlayerLeftLobby(OnlinePlayer player)
        {
            base.PlayerLeftLobby(player);
            if (player == lobby.owner)
            {
                OnlineManager.instance.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MainMenu);
            }
        }

        internal override void ResourceAvailable(OnlineResource onlineResource)
        {
            base.ResourceAvailable(onlineResource);
            if(onlineResource is Lobby lobby)
            {
                lobby.AddData<StoryLobbyData>(true);
            }
        }

        internal override void LobbyTick(uint tick)
        {
            base.LobbyTick(tick);
            readyForWinPlayers = lobby.entities.Values.Where(e => e.entity is StoryClientSettings sas && sas.readyForWin).Select(e => e.entity.owner.inLobbyId).ToList();
        }
    }
}
