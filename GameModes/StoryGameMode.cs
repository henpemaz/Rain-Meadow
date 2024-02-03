using System.Collections.Generic;

namespace RainMeadow
{
    public class StoryGameMode : OnlineGameMode
    {
        public string myDenPos = "SU_C04";
        public string? saveStateProgressString = null;
        public List<ushort> readyForWinPlayers = new List<ushort>();
        public bool didStartGame = false;
        public StoryGameMode(Lobby lobby) : base(lobby)
        {
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
            return SlugcatStats.Name.White;
        }

        public override bool ShouldSpawnFly(FliesWorldAI self, int spawnRoom)
        {
            return true;
        }

        public override bool PlayerCanOwnResource(OnlinePlayer from, OnlineResource onlineResource) {
            if (onlineResource is WorldSession) {
                return lobby.owner == from;
            }
            return true;
        }

        public override void LobbyReadyCheck()
        {
            if (OnlineManager.lobby.isOwner && !(OnlineManager.lobby.gameMode as StoryGameMode).didStartGame) {
                (OnlineManager.lobby.gameMode as StoryGameMode).didStartGame = true;
            }
        }
    }
}
