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

            //On.ArenaBehaviors.ArenaGameBehavior.Update += ArenaGameBehavior_Update;

            On.ArenaCreatureSpawner.SpawnArenaCreatures += ArenaCreatureSpawner_SpawnArenaCreatures;

            On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;
            On.Menu.ArenaOverlay.ctor += ArenaOverlay_ctor;
            On.Menu.ArenaOverlay.Update += ArenaOverlay_Update;
            On.Menu.ArenaOverlay.PlayerPressedContinue += ArenaOverlay_PlayerPressedContinue;
            On.Menu.PlayerResultBox.ctor += PlayerResultBox_ctor;


            On.Menu.MultiplayerResults.ctor += MultiplayerResults_ctor;
            On.Menu.MultiplayerResults.Singal += MultiplayerResults_Singal;
            On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass1;

            On.ArenaGameSession.ScoreOfPlayer += ArenaGameSession_ScoreOfPlayer;

            On.ArenaGameSession.PlayerLandSpear += ArenaGameSession_PlayerLandSpear;

        }

        private void ArenaGameSession_PlayerLandSpear(On.ArenaGameSession.orig_PlayerLandSpear orig, ArenaGameSession self, Player player, Creature target)
        {
            if (isArenaMode(out var _))
            {
                if (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var roomSession))
                {
                    Error("Error getting exit manager room");
                }

                if (!OnlinePhysicalObject.map.TryGetValue(player.abstractCreature, out var absPlayerCreature))
                {
                    Error("Error getting abs Player Creature");
                }

                if (!OnlinePhysicalObject.map.TryGetValue(target.abstractCreature, out var targetAbsCreature))
                {
                    Error("Error getting targetAbsCreature");
                }

                if (!roomSession.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.Arena_PlayerLandSpear, absPlayerCreature, targetAbsCreature, OnlineManager.mePlayer.id.name)))
                {
                    foreach (OnlinePlayer onlinePlayer in OnlineManager.players)
                    {
                        if (roomSession.isOwner)
                        {
                            RPCs.Arena_PlayerLandSpear(targetAbsCreature, absPlayerCreature, OnlineManager.mePlayer.id.name);
                        }
                        else
                        {
                            onlinePlayer.InvokeRPC(RPCs.Arena_PlayerLandSpear, absPlayerCreature, targetAbsCreature, OnlineManager.mePlayer.id.name);

                        }
                    }

                }
            }
            else
            {
                orig(self, player, target);
            }

        }

        private int ArenaGameSession_ScoreOfPlayer(On.ArenaGameSession.orig_ScoreOfPlayer orig, ArenaGameSession self, Player player, bool inHands)
        {
            if (isArenaMode(out var _))
            {

                if (player == null)
                {
                    return 0;
                }

                int num = 0;
                for (int i = 0; i < self.arenaSitting.players.Count; i++)
                {

                    float num2 = 0f;
                    if (inHands && self.arenaSitting.gameTypeSetup.foodScore != 0)
                    {
                        for (int j = 0; j < player.grasps.Length; j++)
                        {
                            if (player.grasps[j] != null && player.grasps[j].grabbed is IPlayerEdible)
                            {
                                IPlayerEdible playerEdible = player.grasps[j].grabbed as IPlayerEdible;
                                num2 = ((!ModManager.MSC || !(player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint) || (!(playerEdible is JellyFish) && !(playerEdible is Centipede) && !(playerEdible is Fly) && !(playerEdible is VultureGrub) && !(playerEdible is SmallNeedleWorm) && !(playerEdible is Hazer))) ? (num2 + (float)(player.grasps[j].grabbed as IPlayerEdible).FoodPoints) : (num2 + 0f));
                            }
                        }
                    }

                    if (Math.Abs(self.arenaSitting.gameTypeSetup.foodScore) > 99)
                    {
                        if (player.FoodInStomach > 0 || num2 > 0f)
                        {
                            self.arenaSitting.players[i].AddSandboxScore(self.arenaSitting.gameTypeSetup.foodScore);
                        }

                        num += self.arenaSitting.players[i].score;
                    }

                    num += (int)((float)self.arenaSitting.players[i].score + ((float)player.FoodInStomach + num2) * (float)self.arenaSitting.gameTypeSetup.foodScore);
                }

                return num;
            }
            else
            {
                return orig(self, player, inHands);
            }
        }

        // TODO
        // Good job you figured out ArenaSitting crap
        // Now you need to:
        // 1. figure out the ordering of the winner / make the score count for slugs and not for bats
        // 2. Figure out why ExitManager Exit logic still wants to keep the doors closed -- DONE
        // 3. Make host press continue as well so you can proceed since your RPC works!
        // 4. 
        private void PlayerResultBox_ctor(On.Menu.PlayerResultBox.orig_ctor orig, Menu.PlayerResultBox self, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, Vector2 size, ArenaSitting.ArenaPlayer player, int index)
        {
            orig(self, menu, owner, pos, size, player, index);
            if (isArenaMode(out var _))
            {
                if (!ModManager.MSC)
                {
                    self.portrait.RemoveSprites();
                    menu.pages[0].RemoveSubObject(self.portrait);
                    var portaitMapper = (player.playerClass == SlugcatStats.Name.White) ? 0 :
                          (player.playerClass == SlugcatStats.Name.Yellow) ? 1 :
                          (player.playerClass == SlugcatStats.Name.Red) ? 2 :
                          (player.playerClass == SlugcatStats.Name.Night) ? 3 : 0;

                    RainMeadow.Debug(player.playerClass);
                    RainMeadow.Debug(portaitMapper);


                    self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + portaitMapper + (self.DeadPortraint ? "0" : "1"), new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);
                    self.subObjects.Add(self.portrait);
                }
            }

        }

        private void ArenaOverlay_ctor(On.Menu.ArenaOverlay.orig_ctor orig, Menu.ArenaOverlay self, ProcessManager manager, ArenaSitting ArenaSitting, List<ArenaSitting.ArenaPlayer> result)
        {

            //result = new List<ArenaSitting.ArenaPlayer>();
            //for (int i = 0; i < OnlineManager.players.Count; i++)
            //{
            //    var player = new ArenaSitting.ArenaPlayer(i);
            //    player.playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[i]] as ArenaClientSettings).playingAs;

            //    result.Add(player);

            //}
            orig(self, manager, ArenaSitting, result);
            if (isArenaMode(out var _))
            {
                // I hate this, but it's necessary so we order the players correctly instead of putting the winner on top
                for (int i = self.resultBoxes.Count - 1; i >= 0; i--)
                {
                    self.resultBoxes[i].RemoveSprites();
                    self.pages[0].subObjects.Remove(self.resultBoxes[i]);
                }
                self.resultBoxes = new List<Menu.PlayerResultBox>();
                for (int i = 0; i < self.result.Count; i++)
                {
                    self.resultBoxes.Add(new Menu.ArenaOverlayResultBox(self, self.pages[0], result[i], i, result[i].winner));
                    self.resultBoxes[i].playerNameLabel.text = OnlineManager.players[i].id.name;
                    self.pages[0].subObjects.Add(self.resultBoxes[i]);
                }
            }

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
            if (isArenaMode(out var _))
            {

                //self.result = new List<ArenaSitting.ArenaPlayer>();
                //for (int i = 0; i < OnlineManager.players.Count; i++)
                //{
                //    var player = new ArenaSitting.ArenaPlayer(i);
                //    player.playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[i]] as ArenaClientSettings).playingAs;
                //    self.result.Add(player);

                //}
            }
            orig(self, manager);
            if (isArenaMode(out var _))
            {

                //self.result = new List<ArenaSitting.ArenaPlayer>();
                //for (int i = 0; i < OnlineManager.players.Count; i++)
                //{
                //    var player = new ArenaSitting.ArenaPlayer(i);
                //    player.playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[i]] as ArenaClientSettings).playingAs;
                //    self.result.Add(player);

                //}


                //for (int i = self.resultBoxes.Count - 1; i >= 0; i--)
                //{
                //    self.resultBoxes[i].RemoveSprites();
                //    self.pages[0].subObjects.Remove(self.resultBoxes[i]);
                //}

                //self.resultBoxes = new List<Menu.PlayerResultBox>();
                for (int i = 0; i < self.resultBoxes.Count; i++)
                {
                    //self.resultBoxes.Add(new Menu.FinalResultbox(self, self.pages[0], self.result[i], i));
                    self.resultBoxes[i].playerNameLabel.text = OnlineManager.players[i].id.name;
                    //self.pages[0].subObjects.Add(self.resultBoxes[i]);


                }
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

                //self.arenaSitting.players = new List<ArenaSitting.ArenaPlayer>();
                //for (int i = 0; i < OnlineManager.players.Count; i++)
                //{
                //    ArenaSitting.ArenaPlayer player = new ArenaSitting.ArenaPlayer(i);
                //    player.playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[i]] as ArenaClientSettings).playingAs;
                //    self.arenaSitting.AddPlayerWithClass(i, player.playerClass);
                //}
                // TODO: Analyze this here. Might be helpful to keep
                //  self.arenaSitting.players[0].playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[0]] as ArenaClientSettings).playingAs; // intercept playerClass here because we bypass in ArenaLobbyMenu
                On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
            }
        }

        private void ProcessManager_RequestMainProcessSwitch_ProcessID(On.ProcessManager.orig_RequestMainProcessSwitch_ProcessID orig, ProcessManager self, ProcessManager.ProcessID ID)
        {

            if (isArenaMode(out var _))
            {
                if (ID == ProcessManager.ProcessID.MultiplayerMenu && self.currentMainLoop.ID == ProcessManager.ProcessID.Game)
                {
                    ID = Ext_ProcessID.ArenaLobbyMenu;
                }
                orig(self, ID);
            }
            else
            {

                orig(self, ID);
            }
        }

        private void ArenaGameBehavior_Update(On.ArenaBehaviors.ArenaGameBehavior.orig_Update orig, ArenaBehaviors.ArenaGameBehavior self)
        {


            orig(self);

            if (isArenaMode(out var _))
            {

                if (self.gameSession.Players.Count != OnlineManager.players.Count)
                {
                    self.gameSession.Players = new List<AbstractCreature>();
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !self.gameSession.Players.Contains(ac))
                        {
                            self.gameSession.Players.Add(ac);
                        }


                    }
                }

                if (self.gameSession.arenaSitting.players.Count != OnlineManager.players.Count)
                {
                    self.gameSession.arenaSitting.players = new List<ArenaSitting.ArenaPlayer>();
                    for (int i = 0; i < OnlineManager.players.Count; i++)
                    {
                        ArenaSitting.ArenaPlayer newPlayer = new ArenaSitting.ArenaPlayer(i);
                        newPlayer.playerNumber = i;
                        newPlayer.playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[i]] as ArenaClientSettings).playingAs;
                        self.gameSession.arenaSitting.players.Add(newPlayer);

                    }
                }

            }

        }

        private void ArenaOverlay_PlayerPressedContinue(On.Menu.ArenaOverlay.orig_PlayerPressedContinue orig, Menu.ArenaOverlay self)
        {
            if (isArenaMode(out var _))
            {

                if (!OnlineManager.lobby.isOwner) // clients cannot initiate next level but should notify host they're ready
                {

                    //self.playersContinueButtons = null;
                    if (!OnlineManager.lobby.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.Arena_ReadyForNextLevel, OnlineManager.mePlayer.id.name)))
                    {
                        OnlineManager.lobby.owner.InvokeRPC(RPCs.Arena_ReadyForNextLevel, OnlineManager.mePlayer.id.name);
                    }
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
                    foreach (OnlinePlayer player in OnlineManager.players)
                    {
                        if (player.id == OnlineManager.lobby.owner.id && arena.clientWaiting == OnlineManager.players.Count - 1)
                        {
                            RPCs.Arena_NextLevelCall();
                        }

                        else
                        {
                            player.InvokeRPC(RPCs.IncrementPlayersLeftt);
                            player.InvokeRPC(RPCs.Arena_NextLevelCall);


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
            if (isArenaMode(out var _))
            {

                if (self.Players.Count != OnlineManager.players.Count)
                {
                    self.Players = new List<AbstractCreature>();
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !self.Players.Contains(ac))
                        {
                            self.Players.Add(ac);

                        }


                    }
                }

                if (self.arenaSitting.players.Count != OnlineManager.players.Count)
                {
                    self.arenaSitting.players = new List<ArenaSitting.ArenaPlayer>();
                    for (int i = 0; i < OnlineManager.players.Count; i++)
                    {
                        ArenaSitting.ArenaPlayer newPlayer = new ArenaSitting.ArenaPlayer(i);
                        newPlayer.playerNumber = i;
                        newPlayer.playerClass = (OnlineManager.lobby.clientSettings[OnlineManager.players[i]] as ArenaClientSettings).playingAs;
                        self.arenaSitting.players.Add(newPlayer);

                    }


                }
                orig(self);



            }
            else
            {
                orig(self);
            }

            for (int i = 0; i < self.Players.Count; i++)
            {
                if (self.Players[i] != null && self.Players[i].realizedCreature != null)
                {
                    RainMeadow.Debug(self.ScoreOfPlayer(self.Players[i].realizedCreature as Player, inHands: true));
                }
            }

            // Comp score is standard
            RainMeadow.Debug("DENENTRY " + self.GameTypeSetup.denEntryRule); //0
            RainMeadow.Debug("SPEAR " + self.GameTypeSetup.spearHitScore); //0
            RainMeadow.Debug("TOENTER " + self.GameTypeSetup.ScoreToEnterDen);



        }


        private void RespawnFlies_Update(On.ArenaBehaviors.RespawnFlies.orig_Update orig, ArenaBehaviors.RespawnFlies self)
        {
            if (isArenaMode(out var _))
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
                return;
            }
        }

        private void Evilifier_Update(On.ArenaBehaviors.Evilifier.orig_Update orig, ArenaBehaviors.Evilifier self)
        {
            if (isArenaMode(out var _))
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
            if (isArenaMode(out var _))
            {

                if (self == null)
                {
                    return;
                }
                if (self.room == null)
                {
                    return;
                }
                if (!self.room.shortCutsReady)
                {
                    return;
                }

                orig(self);
                RainMeadow.Debug(self.ExitsOpen());
                RainMeadow.Debug(self.gameSession.GameTypeSetup.denEntryRule);
            }
            else
            {
                orig(self);
            }



        }
        private bool ExitManager_ExitsOpen(On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ArenaBehaviors.ExitManager self)
        {

            if (isArenaMode(out var _))
            {
                if (self.anyPlayerHasEnoughScore)
                {
                    return true;
                }
                return false;
                //return false;
                //var deadCount = 0;

                //foreach (var player in self.gameSession.Players)
                //{
                //    if (player.realizedCreature != null && (player.realizedCreature.State.dead))
                //    {
                //        deadCount++;
                //    }
                //}

                //if (deadCount != 0 && deadCount == self.gameSession.Players.Count - 1)
                //{
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}


            }
            else
            {
                return orig(self);
            }

        }


        private void ArenaGameSession_SpawnPlayers(On.ArenaGameSession.orig_SpawnPlayers orig, ArenaGameSession self, Room room, List<int> suggestedDens)
        {

            if (isArenaMode(out var _))
            {
                if (RainMeadow.isArenaMode(out var arena))
                {

                    List<OnlinePlayer> list = new List<OnlinePlayer>();


                    List<OnlinePlayer> list2 = new List<OnlinePlayer>();
                    for (int j = 0; j < OnlineManager.players.Count; j++)
                    {
                        list2.Add(OnlineManager.players[j]);
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
                        abstractCreature.state = new PlayerState(abstractCreature, 0, self.characterStats_Mplayer[0].name, isGhost: false);
                    }
                    else
                    {
                        abstractCreature.state = new PlayerState(abstractCreature, 0, new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.GetEntry(0)), isGhost: false);
                    }


                    RainMeadow.Debug("Arena: Realize Creature!");
                    abstractCreature.Realize();
                    var shortCutVessel = new ShortcutHandler.ShortCutVessel(new RWCustom.IntVector2(-1, -1), abstractCreature.realizedCreature, self.game.world.GetAbstractRoom(0), 0);
                    shortCutVessel.entranceNode = num;
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
                if (WorldSession.map.TryGetValue(world, out var ws) && OnlineManager.lobby.gameMode.ShouldSyncAPOInWorld(ws, apo)) ws.ApoEnteringWorld(apo);
                if (RoomSession.map.TryGetValue(asbtRoom, out var rs) && OnlineManager.lobby.gameMode.ShouldSyncAPOInRoom(rs, apo)) rs.ApoLeavingRoom(apo);
            }
        }

    }


}