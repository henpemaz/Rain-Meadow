using HUD;
using RainMeadow.GameModes;
using System;
using System.Collections.Generic;
using System.Linq;
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

            On.Spear.Update += Spear_Update;

            On.ArenaGameSession.SpawnPlayers += ArenaGameSession_SpawnPlayers;
            On.ArenaGameSession.Update += ArenaGameSession_Update;
            On.ArenaGameSession.ctor += ArenaGameSession_ctor;
            On.ArenaGameSession.AddHUD += ArenaGameSession_AddHUD;
            On.ArenaGameSession.SpawnCreatures += ArenaGameSession_SpawnCreatures;

            On.ArenaBehaviors.ExitManager.ExitsOpen += ExitManager_ExitsOpen;
            On.ArenaBehaviors.ExitManager.Update += ExitManager_Update;
            On.ArenaBehaviors.ExitManager.PlayerTryingToEnterDen += ExitManager_PlayerTryingToEnterDen;
            On.ArenaBehaviors.Evilifier.Update += Evilifier_Update;
            On.ArenaBehaviors.RespawnFlies.Update += RespawnFlies_Update;
            On.ArenaBehaviors.ArenaGameBehavior.Update += ArenaGameBehavior_Update;
            On.ArenaCreatureSpawner.SpawnArenaCreatures += ArenaCreatureSpawner_SpawnArenaCreatures;

            On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;
            On.Menu.ArenaOverlay.Update += ArenaOverlay_Update;
            On.Menu.ArenaOverlay.PlayerPressedContinue += ArenaOverlay_PlayerPressedContinue;

            On.Menu.MultiplayerResults.ctor += MultiplayerResults_ctor;
            On.Menu.MultiplayerResults.Singal += MultiplayerResults_Singal;
            On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass1;

        }

        private void Player_GetInitialSlugcatClass1(On.Player.orig_GetInitialSlugcatClass orig, Player self)
        {
            orig(self);
            if (isArenaMode(out var arena))
            {
                self.SlugCatClass = (arena.clientSettings as ArenaClientSettings).playingAs;

            }
        }
        private void Spear_Update(On.Spear.orig_Update orig, Spear self, bool eu)
        {

            if (RainMeadow.isArenaMode(out var _))
            {
                if (self == null)
                {
                    RainMeadow.Debug("Spear is null");
                    return;
                }

                if (self.mode == Weapon.Mode.StuckInCreature && self.stuckInObject == null)
                {
                    RainMeadow.Debug("Creature fell off map with spear in them");
                    return;
                }

                orig(self, eu);



            }
            else
            {
                orig(self, eu);
            }

        }

        private void MultiplayerResults_ctor(On.Menu.MultiplayerResults.orig_ctor orig, Menu.MultiplayerResults self, ProcessManager manager)
        {
            orig(self, manager);
            if (isArenaMode(out var _))
            {
                var exitButton = new Menu.SimpleButton(self, self.pages[0], self.Translate("EXIT"), "EXIT", new Vector2(856f, 50f), new Vector2(110f, 30f));
                self.pages[0].subObjects.Add(exitButton);
            }
        }

        private void MultiplayerResults_Singal(On.Menu.MultiplayerResults.orig_Singal orig, Menu.MultiplayerResults self, Menu.MenuObject sender, string message)
        {
            if (isArenaMode(out var _))
            {

                if (message != null && message == "CONTINUE")
                {
                    self.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.ArenaLobbyMenu);
                    self.manager.rainWorld.options.DeleteArenaSitting();
                    self.PlaySound(SoundID.MENU_Switch_Page_In);
                }

                if (message != null && message == "EXIT")
                {

                    self.manager.rainWorld.options.DeleteArenaSitting();
                    OnlineManager.LeaveLobby();
                    self.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                    self.PlaySound(SoundID.MENU_Switch_Page_In);
                }
            }
            else
            {
                orig(self, sender, message);
            }
        }

        private void ArenaCreatureSpawner_SpawnArenaCreatures(On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
        {
            if (isArenaMode(out var _))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    RainMeadow.Debug("Spawning creature");
                    orig(game, wildLifeSetting, ref availableCreatures, ref unlocks);
                }
                else
                {
                    RainMeadow.Debug("Prevented client from spawning excess creatures");
                }
            }
            else
            {
                orig(game, wildLifeSetting, ref availableCreatures, ref unlocks);
            }
        }

        private void ArenaGameSession_SpawnCreatures(On.ArenaGameSession.orig_SpawnCreatures orig, ArenaGameSession self)
        {
            if (isArenaMode(out var _))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    RainMeadow.Debug("Spawning creature");

                    orig(self);
                }
                else
                {
                    RainMeadow.Debug("Prevented client from spawning excess creatures");
                }


            }
            else
            {
                orig(self);
            }
        }

        private void HUD_InitMultiplayerHud(On.HUD.HUD.orig_InitMultiplayerHud orig, HUD.HUD self, ArenaGameSession session)
        {


            if (isArenaMode(out var _))
            {
                self.AddPart(new TextPrompt(self));
            }
            else
            {
                orig(self, session);
            }

        }



        private void ArenaGameSession_AddHUD(On.ArenaGameSession.orig_AddHUD orig, ArenaGameSession self)
        {
            orig(self);


            if (isArenaMode(out var gameMode))
            {
                self.game.cameras[0].hud.AddPart(new OnlineHUD(self.game.cameras[0].hud, self.game.cameras[0], gameMode));
            }
        }

        private bool ExitManager_PlayerTryingToEnterDen(On.ArenaBehaviors.ExitManager.orig_PlayerTryingToEnterDen orig, ArenaBehaviors.ExitManager self, ShortcutHandler.ShortCutVessel shortcutVessel)
        {

            if (isArenaMode(out var _))
            {

                if (!(shortcutVessel.creature is Player))
                {
                    return false;
                }

                if (ModManager.MSC && shortcutVessel.creature.abstractCreature.creatureTemplate.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.SlugNPC)
                {
                    return false;
                }

                if (self.gameSession.GameTypeSetup.denEntryRule == ArenaSetup.GameTypeSetup.DenEntryRule.Score && self.gameSession.ScoreOfPlayer(shortcutVessel.creature as Player, inHands: true) < self.gameSession.GameTypeSetup.ScoreToEnterDen)
                {
                    return false;
                }

                int num = -1;
                for (int i = 0; i < shortcutVessel.room.realizedRoom.exitAndDenIndex.Length; i++)
                {
                    if (shortcutVessel.pos == shortcutVessel.room.realizedRoom.exitAndDenIndex[i])
                    {
                        num = i;
                        break;
                    }
                }

                if (self.ExitsOpen() && !self.ExitOccupied(num))
                {
                    shortcutVessel.entranceNode = num;
                    if (!OnlinePhysicalObject.map.TryGetValue(shortcutVessel.creature.abstractPhysicalObject, out var onlineVessel))
                    {
                        Error("Error getting online vessel");
                    }

                    if (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var roomSession))
                    {
                        Error("Error getting exit manager room");
                    }

                    if (!roomSession.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.AddShortCutVessel, new RWCustom.IntVector2(-1, -1), onlineVessel, roomSession, 0)))
                    {
                        foreach (OnlinePlayer player in OnlineManager.players)
                        {
                            if (roomSession.isOwner)
                            {

                                RPCs.AddShortCutVessel(new RWCustom.IntVector2(-1, -1), onlineVessel, roomSession, 0);
                            }
                            else
                            {
                                player.InvokeRPC(RPCs.AddShortCutVessel, new RWCustom.IntVector2(-1, -1), onlineVessel, roomSession, 0);

                            }
                        }

                    }
                    return true;
                }

                return false;
            }
            else
            {
                return orig(self, shortcutVessel);

            }

        }

        private void ArenaGameSession_ctor(On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            orig(self, game);
            if (isArenaMode(out var _))
            {
                self.thisFrameActivePlayers = OnlineManager.players.Count;


                On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
            }
        }

        private void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (ID == ProcessManager.ProcessID.MultiplayerMenu && self.currentMainLoop.ID == ProcessManager.ProcessID.Game && isArenaMode(out var _))
            {
                ID = Ext_ProcessID.ArenaLobbyMenu;
            }

            orig(self, ID);
        }

        private void ArenaGameBehavior_Update(On.ArenaBehaviors.ArenaGameBehavior.orig_Update orig, ArenaBehaviors.ArenaGameBehavior self)
        {

            orig(self);
            if (isArenaMode(out var _))
            {

                if (self.gameSession.Players.Count < OnlineManager.players.Count)
                {
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !self.gameSession.Players.Contains(ac))
                        {
                            self.gameSession.Players.Add(ac);
                        }


                    }
                }
            }

        }

        private void ArenaOverlay_PlayerPressedContinue(On.Menu.ArenaOverlay.orig_PlayerPressedContinue orig, Menu.ArenaOverlay self)
        {
            if (isArenaMode(out var _))
            {

                if (!OnlineManager.lobby.isOwner) // clients cannot initiate next level
                {
                    self.playersContinueButtons = null;
                    self.PlaySound(SoundID.UI_Multiplayer_Player_Result_Box_Player_Ready);

                }
                else
                {
                    orig(self);
                }
            }
            else
            {
                orig(self);
            }
        }

        private void ArenaOverlay_Update(On.Menu.ArenaOverlay.orig_Update orig, Menu.ArenaOverlay self)
        {

            if (isArenaMode(out var arena))
            {

                if (self.countdownToNextRound == 0 && !self.nextLevelCall)
                {

                    ArenaGameSession getArenaGameSession = (self.manager.currentMainLoop as RainWorldGame).GetArenaGameSession;
                    AbstractRoom absRoom = getArenaGameSession.game.world.abstractRooms[0];

                    if (RoomSession.map.TryGetValue(absRoom, out var roomSession))
                    {
                        foreach (OnlinePlayer player in OnlineManager.players)
                        {
                            if (player.id == OnlineManager.lobby.owner.id)
                            {
                                RPCs.Arena_NextLevelCall();
                            }

                            else
                            {
                                player.InvokeRPC(RPCs.Arena_NextLevelCall);

                            }

                        }
                    }


                }

                if (self.nextLevelCall)
                {
                    return;
                }

                orig(self);
            }
            else
            {
                orig(self);
            }


        }

        private void ArenaGameSession_Update(On.ArenaGameSession.orig_Update orig, ArenaGameSession self)
        {
            if (isArenaMode(out var arena))
            {

                if (self.Players.Count < OnlineManager.players.Count)
                {
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !self.Players.Contains(ac))
                        {
                            self.Players.Add(ac);
                        }

                    }
                }
                orig(self);
            }
            else
            {
                orig(self);
            }

        }


        private void RespawnFlies_Update(On.ArenaBehaviors.RespawnFlies.orig_Update orig, ArenaBehaviors.RespawnFlies self)
        {
            if (isArenaMode(out var arena))
            {

                if (self.room == null)
                {
                    return;
                }

                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void Evilifier_Update(On.ArenaBehaviors.Evilifier.orig_Update orig, ArenaBehaviors.Evilifier self)
        {
            if (isArenaMode(out var arena))
            {
                if (self.room == null)
                {
                    return;
                }
                orig(self);
            }
            else
            {
                orig(self);
            }
        }

        private void ExitManager_Update(On.ArenaBehaviors.ExitManager.orig_Update orig, ArenaBehaviors.ExitManager self)
        {
            if (isArenaMode(out var arena))
            {

                if (self == null)
                {
                    return;
                }
                if (self.room == null)
                {
                    return;
                }
                if (self.room.shortcuts == null)
                {
                    return;
                }
                orig(self);
            }
            else
            {
                orig(self);
            }

        }
        private bool ExitManager_ExitsOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {
            if (isArenaMode(out var arena))
            {

                var deadCount = 0;

                foreach (var player in self.gameSession.Players)
                {
                    if (player.realizedCreature != null && player.realizedCreature.State.dead)
                    {
                        deadCount++;
                    }
                }

                if (deadCount != 0 && deadCount == self.gameSession.Players.Count - 1)
                {
                    return true;
                }

                return orig(self);
            }
            else
            {
                return orig(self);
            }
        }


        private void ArenaGameSession_SpawnPlayers(On.ArenaGameSession.orig_SpawnPlayers orig, ArenaGameSession self, Room room, List<int> suggestedDens)
        {

            if (RainMeadow.isArenaMode(out var arena))
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
                int[] denArray = new int[exits];

                if (suggestedDens != null)
                {
                    for (int k = 0; k < suggestedDens.Count; k++)
                    {
                        if (suggestedDens[k] >= 0 && suggestedDens[k] < denArray.Length)
                        {
                            denArray[suggestedDens[k]] -= 1000;
                        }
                    }
                }

                arena.denChoice = arena.denChoice + 1 % exits;


                //float num2 = float.MinValue;
                //// must be weighting the den choice?
                //for (int m = 0; m < exits; m++)
                //{
                //    float num3 = UnityEngine.Random.value - (float)denArray[m] * 1000f;
                //    RWCustom.IntVector2 startTile = room.ShortcutLeadingToNode(m).StartTile;
                //    for (int n = 0; n < exits; n++)
                //    {
                //        if (n != m && denArray[n] > 0)
                //        {
                //            num3 += Mathf.Clamp(startTile.FloatDist(room.ShortcutLeadingToNode(n).StartTile), 8f, 17f) * UnityEngine.Random.value;
                //        }
                //    }

                //    if (num3 > num2)
                //    {
                //        shortCutChoice = m;
                //        num2 = num3;
                //    }
                //}
                // Probably RPC this
                // this was originally a loop, don't foreget that.
                // denArray[shortCutChoice]++;




                RainMeadow.Debug("Trying to create an abstract creature");

                sSpawningAvatar = true;
                AbstractCreature abstractCreature = new AbstractCreature(self.game.world, StaticWorld.GetCreatureTemplate("Slugcat"), null, new WorldCoordinate(0, -1, -1, -1), new EntityID(-1, 0));
                RainMeadow.Debug("assigned ac, moving to den");
                AbstractRoom_Arena_MoveEntityToDen(self.game.world, abstractCreature.Room, abstractCreature); // Arena adds abstract creature then realizes it later
                RainMeadow.Debug("moved, setting online creature");
                SetOnlineCreature(abstractCreature);
                sSpawningAvatar = false;

                if (OnlineManager.lobby.isActive)
                {
                    OnlineManager.instance.Update(); // Subresources are active, gamemode is online, ticks are happening. Not sure why we'd need this here
                }


                if (ModManager.MSC)
                {
                    self.game.cameras[0].followAbstractCreature = abstractCreature;
                }

                if (self.chMeta != null)
                {
                    abstractCreature.state = new PlayerState(abstractCreature, list[0].playerNumber, self.characterStats_Mplayer[0].name, isGhost: false);
                }
                else
                {
                    abstractCreature.state = new PlayerState(abstractCreature, list[0].playerNumber, new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.GetEntry(list[0].playerNumber)), isGhost: false);
                }



                abstractCreature.Realize();
                var shortCutVessel = new ShortcutHandler.ShortCutVessel(new RWCustom.IntVector2(-1, -1), abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);
              

                shortCutVessel.entranceNode = arena.denChoice;
                shortCutVessel.room = self.game.world.GetAbstractRoom(abstractCreature.Room.name);
                abstractCreature.pos.room = self.game.world.offScreenDen.index;
                self.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);
                self.AddPlayer(abstractCreature);

                if (ModManager.MSC)
                {
                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Red)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, -0.75f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.5f);
                    }

                    if ((abstractCreature.realizedCreature as Player).SlugCatClass == SlugcatStats.Name.Yellow)
                    {
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.All, -1, 0, 0.75f);
                        self.creatureCommunities.SetLikeOfPlayer(CreatureCommunities.CommunityID.Scavengers, -1, 0, 0.3f);
                    }


                }


                self.playersSpawned = true;

                if (!OnlineManager.lobby.isOwner)
                {
                    OnlineManager.lobby.owner.InvokeRPC(RPCs.ResetPlayersLeft);
                    arena.nextLevel = false;
                }

            }
            else
            {
                orig(self, room, suggestedDens);

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