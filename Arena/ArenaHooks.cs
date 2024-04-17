using RainMeadow.GameModes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public static bool isArenaMode(out ArenaCompetitiveGameMode gameMode)
        {
            gameMode = null;
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode arena)
            {
                gameMode = arena;
                return true;
            }
            return false;
        }

        private void ArenaHooks()
        {

            On.ArenaGameSession.Update += ArenaGameSession_Update;
            On.ArenaGameSession.SpawnPlayers += ArenaGameSession_SpawnPlayers;



        }



        private void ArenaGameSession_SpawnPlayers(On.ArenaGameSession.orig_SpawnPlayers orig, ArenaGameSession self, Room room, List<int> suggestedDens) // player 2 is not spawning, is spectating the correct room but no data from player 1 is sent
        {
            List<ArenaSitting.ArenaPlayer> list = new List<ArenaSitting.ArenaPlayer>();


            List<ArenaSitting.ArenaPlayer> list2 = new List<ArenaSitting.ArenaPlayer>();
            for (int j = 0; j < self.arenaSitting.players.Count; j++)
            {
                list2.Add(self.arenaSitting.players[j]);
            }

            while (list2.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, list2.Count);
                list.Add(list2[index]);
                list2.RemoveAt(index);
            }


            int exits = self.game.world.GetAbstractRoom(0).exits;
            int[] array = new int[exits];
            if (suggestedDens != null)
            {
                for (int k = 0; k < suggestedDens.Count; k++)
                {
                    if (suggestedDens[k] >= 0 && suggestedDens[k] < array.Length)
                    {
                        array[suggestedDens[k]] -= 1000;
                    }
                }
            }

            for (int l = 0; l < list.Count; l++)
            {
                int num = UnityEngine.Random.Range(0, exits);
                float num2 = float.MinValue;
                for (int m = 0; m < exits; m++)
                {
                    float num3 = UnityEngine.Random.value - (float)array[m] * 1000f;
                    RWCustom.IntVector2 startTile = room.ShortcutLeadingToNode(m).StartTile;
                    for (int n = 0; n < exits; n++)
                    {
                        if (n != m && array[n] > 0)
                        {
                            num3 += Mathf.Clamp(startTile.FloatDist(room.ShortcutLeadingToNode(n).StartTile), 8f, 17f) * UnityEngine.Random.value;
                        }
                    }

                    if (num3 > num2)
                    {
                        num = m;
                        num2 = num3;
                    }
                }

                array[num]++;

                AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, list[l].playerNumber));

                AbstractRoom_Arena_MoveEntityToDen(self.game.world, abstractCreature.Room, abstractCreature); // Arena adds abstract creature then realizes it later
                SetOnlineCreature(abstractCreature);
/*                if (OnlineManager.lobby.isActive)
                {
                    OnlineManager.lobby.Tick(OnlineManager.mePlayer.tick); // I think the Lobby data is being destroyed and that's why it only works here?
                }*/


                if (ModManager.MSC && l == 0)
                {
                    self.game.cameras[0].followAbstractCreature = abstractCreature;
                }

                if (self.chMeta != null)
                {
                    abstractCreature.state = new PlayerState(abstractCreature, list[l].playerNumber, self.characterStats_Mplayer[0].name, isGhost: false);
                }
                else
                {
                    abstractCreature.state = new PlayerState(abstractCreature, list[l].playerNumber, new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.GetEntry(list[l].playerNumber)), isGhost: false);
                }



                abstractCreature.Realize();
                ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(new RWCustom.IntVector2(-1, -1), abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);
                shortCutVessel.entranceNode = num;
                shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);
                abstractCreature.pos.room = self.game.world.offScreenDen.index;
                self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
                self.AddPlayer(abstractCreature);
                if (ModManager.MSC)
                {
                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, l, -0.75f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, l, 0.5f);
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, l, 0.75f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, l, 0.3f);
                    }


                }
            }

            self.playersSpawned = true;

        }

        private void SetOnlineCreature(AbstractCreature abstractCreature)
        {
            if (OnlineCreature.map.TryGetValue(abstractCreature, out var onlineCreature))
            {
                RainMeadow.Debug("Found OnlineCreature");
                OnlineManager.lobby.gameMode.SetAvatar(onlineCreature as OnlineCreature);
            }
            else
            {
                throw new InvalidProgrammerException($"Can't find OnlineCreature for {abstractCreature}");
            }
        }

        private void AbstractRoom_Arena_MoveEntityToDen(World world, AbstractRoom asbtRoom, AbstractWorldEntity entity)
        {
            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo0 && OnlinePhysicalObject.map.TryGetValue(apo0, out var oe))
            {
                if (!oe.isMine && !oe.beingMoved)
                {
                    Error($"Remote entity trying to move: {oe} at {oe.roomSession} {Environment.StackTrace}");
                    return;
                }
            }

            if (OnlineManager.lobby != null && entity is AbstractPhysicalObject apo)
            {
                if (WorldSession.map.TryGetValue(world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncObjectInWorld(ws, apo)) ws.ApoEnteringWorld(apo);
                if (RoomSession.map.TryGetValue(asbtRoom, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncObjectInRoom(rs, apo)) rs.ApoLeavingRoom(apo);
            }
        }


        private void ArenaGameSession_Update(On.ArenaGameSession.orig_Update orig, ArenaGameSession self)
        {

            if (self.arenaSitting.attempLoadInGame && self.arenaSitting.gameTypeSetup.savingAndLoadingSession)
            {
                self.arenaSitting.attempLoadInGame = false;
                self.arenaSitting.LoadFromFile(self, self.game.world, self.game.rainWorld);
            }

            if (self.initiated)
            {
                self.counter++;
            }
            else if (self.room != null && self.room.shortCutsReady)
            {
                self.Initiate();
            }

            if (self.room != null && self.chMeta != null && self.chMeta.deferred)
            {
                self.room.deferred = true;
            }

            self.thisFrameActivePlayers = self.PlayersStillActive(addToAliveTime: true, dontCountSandboxLosers: false);
            RainMeadow.Debug("GAME COLOR" + (OnlineManager.lobby.gameMode.clientSettings as ArenaClientSettings).bodyColor);

            // stop game over by not adding the rest of orig code
        }


        // TODO: Index out of range issue
        public static Region[] LoadAllArenaLevels(SlugcatStats.Name storyIndex)
        {

            List<Region> regions = new List<Region>();

            /*            string[] files = Directory.GetFiles(AssetManager.ResolveFilePath("Levels"), "*.txt");

                        if (files.Length == 0)
                        {
                            RainMeadow.Error("No arena files found");
                        }

                        List<string> modifiedFileNames = new List<string>();
                        List<Region> regions = new List<Region>();
                        int num = 0;
                        for (int i = 0; i < files.Length; i++)
                        {
                            // Extract just the filename without the extension from the full path
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);

                            // Check if the filename contains "_arena" or "_settings"
                            if (fileNameWithoutExtension.Contains("_arena") || fileNameWithoutExtension.Contains("_settings"))
                            {
                                continue; // Skip this file
                            }

                            // uncomment this when you're done
                            string modifiedFileName = "arena" + fileNameWithoutExtension;

                            // Add the modified filename to the list
                            modifiedFileNames.Add(fileNameWithoutExtension);

                            // Use the modified filename when creating the Region object
                            Region region = new Region(fileNameWithoutExtension, 0, 0, storyIndex);
                            regions.Add(region);
                            num += region.numberOfRooms;
                        }*/
            Region arenaRegion = new Region("arena", 0, 0, storyIndex);
            Region arenaRegionsm = new Region("arenasmallroom", 0, 0, storyIndex);

            regions.Add(arenaRegion);
            regions.Add(arenaRegionsm);
            return regions.ToArray();
        }


    }


}