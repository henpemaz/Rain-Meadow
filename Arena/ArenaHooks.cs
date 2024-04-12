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

            On.ArenaGameSession.SpawnPlayers += ArenaGameSession_SpawnPlayers;
            On.ArenaBehaviors.ExitManager.ExitsOpen += ExitManager_ExitsOpen;


        }

        private bool ExitManager_ExitsOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            if (OnlineManager.lobby == null)
            {
                return orig(self);
            }

            var deadCount = 0;
            foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
            {
                if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                {
                    if (ac.state.dead
                    || ((ac.realizedCreature as Player)?.dangerGrasp != null && (ac.realizedCreature as Player)?.dangerGraspTime > 200))
                    {
                        deadCount++;
                    }
                }

            }

            if (deadCount == OnlineManager.players.Count -1)
            {
                return true;
            }
            return false;
        }


        private void ArenaGameSession_SpawnPlayers(On.ArenaGameSession.orig_SpawnPlayers orig, ArenaGameSession self, Room room, List<int> suggestedDens)
        {

            if (OnlineManager.lobby == null)
            {
                orig(self, room, suggestedDens);
                return;
            }

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
                if (OnlineManager.lobby.isActive)
                {
                    OnlineManager.instance.Update(); // Subresources are active, gamemode is online, ticks are happening. Not sure why we'd need this here
                }


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

    }


}