﻿using HUD;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Newtonsoft.Json.Linq;
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
            On.ArenaGameSession.EndOfSessionLogPlayerAsAlive += ArenaGameSession_EndOfSessionLogPlayerAsAlive;
            On.ArenaGameSession.Killing += ArenaGameSession_Killing;
            On.ArenaGameSession.AddHUD += ArenaGameSession_AddHUD;
            On.ArenaGameSession.SpawnCreatures += ArenaGameSession_SpawnCreatures;
            On.ArenaGameSession.ctor += ArenaGameSession_ctor;

            On.ArenaSitting.SessionEnded += ArenaSitting_SessionEnded;



            On.ArenaBehaviors.ExitManager.ExitsOpen += ExitManager_ExitsOpen;
            On.ArenaBehaviors.ExitManager.Update += ExitManager_Update;
            On.ArenaBehaviors.ExitManager.PlayerTryingToEnterDen += ExitManager_PlayerTryingToEnterDen;
            On.ArenaBehaviors.Evilifier.Update += Evilifier_Update;
            On.ArenaBehaviors.RespawnFlies.Update += RespawnFlies_Update;



            On.ShortcutGraphics.ChangeAllExitsToSheltersOrDots += ShortcutGraphics_ChangeAllExitsToSheltersOrDots;

            On.ArenaCreatureSpawner.SpawnArenaCreatures += ArenaCreatureSpawner_SpawnArenaCreatures;

            On.HUD.HUD.InitMultiplayerHud += HUD_InitMultiplayerHud;
            On.Menu.ArenaOverlay.Update += ArenaOverlay_Update;
            On.Menu.ArenaOverlay.PlayerPressedContinue += ArenaOverlay_PlayerPressedContinue;
            On.Menu.PlayerResultBox.ctor += PlayerResultBox_ctor;

            On.Menu.MultiplayerResults.ctor += MultiplayerResults_ctor;
            On.Menu.MultiplayerResults.Singal += MultiplayerResults_Singal;

            // On.ArenaGameSession.ScoreOfPlayer += ArenaGameSession_ScoreOfPlayer;


            IL.CreatureCommunities.ctor += OverwriteArenaPlayerMax;
            IL.ArenaGameSession.ctor += OverwriteArenaPlayerMax;
            On.RWInput.PlayerRecentController_int += RWInput_PlayerRecentController_int;
            On.RWInput.PlayerInputLogic_int_int += RWInput_PlayerInputLogic_int_int;
            On.RWInput.PlayerUIInput_int += RWInput_PlayerUIInput_int;

            On.MultiplayerUnlocks.IsLevelUnlocked += MultiplayerUnlocks_IsLevelUnlocked;
            On.Menu.LevelSelector.LevelToPlaylist += LevelSelector_LevelToPlaylist;
            On.Menu.LevelSelector.LevelFromPlayList += LevelSelector_LevelFromPlayList;

            On.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons += MultiplayerMenu_InitiateGameTypeSpecificButtons;
            On.Menu.ArenaSettingsInterface.SetSelected += ArenaSettingsInterface_SetSelected;
            On.Menu.ArenaSettingsInterface.SetChecked += ArenaSettingsInterface_SetChecked;
            On.Menu.ArenaSettingsInterface.ctor += ArenaSettingsInterface_ctor;

        }

        private void ArenaSettingsInterface_ctor(On.Menu.ArenaSettingsInterface.orig_ctor orig, Menu.ArenaSettingsInterface self, Menu.Menu menu, Menu.MenuObject owner)
        {
            orig(self, menu, owner);
            if (isArenaMode(out var arena))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    arena.onlineArenaSettingsInterfaceMultiChoice = new Dictionary<string, int>();
                    arena.onlineArenaSettingsInterfaceeBool = new Dictionary<string, bool>();


                    // Add lobby syncs for those who join after we've started the game session

                    var roomRepeatSync = "ROOMREPEAT"; // Don't ask.
                    int roomRepeatValue = self.GetGameTypeSetup.levelRepeats;


                    var rainSyncKey = self.rainTimer.IDString;
                    int rainSyncValue = self.GetGameTypeSetup.sessionTimeLengthIndex;

                    var wildLifeSyncKey = self.wildlifeArray.IDString;
                    int wildLifeSyncValue = self.GetGameTypeSetup.wildLifeSetting.index;

                    //var spearsHitSync = self.spearsHitCheckbox.IDString; Not dealing with this logic.
                    //var spearsHitValue = self.GetGameTypeSetup.spearsHitPlayers;

                    var aggroAISync = self.evilAICheckBox.IDString;
                    var aggroAISyncValue = self.GetGameTypeSetup.evilAI;

                    arena.onlineArenaSettingsInterfaceMultiChoice.Add(roomRepeatSync, roomRepeatValue);
                    arena.onlineArenaSettingsInterfaceMultiChoice.Add(rainSyncKey, rainSyncValue);
                    arena.onlineArenaSettingsInterfaceMultiChoice.Add(wildLifeSyncKey, wildLifeSyncValue);

                    // arena.onlineArenaSettingsInterfaceeBool.Add(spearsHitSync, spearsHitValue);
                    arena.onlineArenaSettingsInterfaceeBool.Add(aggroAISync, aggroAISyncValue);



                }
                if (!OnlineManager.lobby.isOwner)
                {
                    foreach (var selectable in self.menu.pages[0].selectables)
                    {
                        if (selectable is Menu.MultipleChoiceArray.MultipleChoiceButton)
                        {
                            RainMeadow.Debug((selectable as Menu.MultipleChoiceArray.MultipleChoiceButton).multipleChoiceArray.IDString);
                            var onlineArrayMutliChoice = (selectable as Menu.MultipleChoiceArray.MultipleChoiceButton).multipleChoiceArray.IDString;
                            if (arena.onlineArenaSettingsInterfaceMultiChoice.ContainsKey(onlineArrayMutliChoice)) 
                            {
                                self.SetSelected((selectable as Menu.MultipleChoiceArray.MultipleChoiceButton).multipleChoiceArray, arena.onlineArenaSettingsInterfaceMultiChoice[onlineArrayMutliChoice]);
                            }
                            (selectable as Menu.MultipleChoiceArray.MultipleChoiceButton).buttonBehav.greyedOut = true;

                        }
                        if (selectable is Menu.CheckBox)
                        {

                            var onlineArrayBool = (selectable as Menu.CheckBox).IDString;
                            if (arena.onlineArenaSettingsInterfaceeBool.ContainsKey(onlineArrayBool))
                            {
                                self.SetChecked((selectable as Menu.CheckBox), arena.onlineArenaSettingsInterfaceeBool[onlineArrayBool]);
                            }
                           (selectable as Menu.CheckBox).buttonBehav.greyedOut = true;

                        }

                    }
                }
            }

        }

        private void ArenaSettingsInterface_SetSelected(On.Menu.ArenaSettingsInterface.orig_SetSelected orig, Menu.ArenaSettingsInterface self, Menu.MultipleChoiceArray array, int i)
        {
           
            if (isArenaMode(out var arena))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    if (arena.onlineArenaSettingsInterfaceMultiChoice.ContainsKey(array.IDString))
                    {
                        RainMeadow.Debug($"Setting {array.IDString} to value {i}");
                        arena.onlineArenaSettingsInterfaceMultiChoice[array.IDString] = i;
                    }
                    foreach (var player in OnlineManager.players)
                    {
                        if (player.id == OnlineManager.lobby.owner.id || player.isMe)
                        {
                            continue;
                        }
                        player.InvokeOnceRPC(ArenaRPCs.Arena_UpdateSelectedChoice, array.IDString, i);
                    }
                }
                orig(self, array, i);

            } else
            {
                orig(self, array, i);
            }


        }

        private void ArenaSettingsInterface_SetChecked(On.Menu.ArenaSettingsInterface.orig_SetChecked orig, Menu.ArenaSettingsInterface self, Menu.CheckBox box, bool c)
        {
            orig(self, box, c);

            if (isArenaMode(out var arena))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    if (arena.onlineArenaSettingsInterfaceeBool.ContainsKey(box.IDString))
                    {
                        RainMeadow.Debug($"Setting {box.IDString} to value {c}");
                        arena.onlineArenaSettingsInterfaceeBool[box.IDString] = c;
                    }
                    foreach (var player in OnlineManager.players)
                    {
                        if (player.id == OnlineManager.lobby.owner.id || player.isMe)
                        {
                            continue;
                        }
                        player.InvokeOnceRPC(ArenaRPCs.Arena_UpdateSelectedCheckbox, box.IDString, c);
                    }
                }

            }

        }






        private void MultiplayerMenu_InitiateGameTypeSpecificButtons(On.Menu.MultiplayerMenu.orig_InitiateGameTypeSpecificButtons orig, Menu.MultiplayerMenu self)
        {
            if (isArenaMode(out var _))
            {
                self.currentGameType = ArenaSetup.GameTypeID.Competitive; // force override for now
            }
            orig(self);
        }

        private void LevelSelector_LevelFromPlayList(On.Menu.LevelSelector.orig_LevelFromPlayList orig, Menu.LevelSelector self, int index)
        {
            if (isArenaMode(out var arena))
            {
                foreach (var player in OnlineManager.players)
                {
                    if (player.id == OnlineManager.lobby.owner.id || player.isMe)
                    {
                        continue;
                    }
                    player.InvokeOnceRPC(ArenaRPCs.Arena_LevelFromPlaylist, index, self.levelsPlaylist.levelItems[index].name);

                }
                if (!OnlineManager.lobby.isOwner)
                {
                    return;
                }


            }
            orig(self, index);
            if (isArenaMode(out var _))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    try
                    {
                        arena.playList.RemoveAt(index);
                    }
                    catch
                    {
                        RainMeadow.Debug("Arena: Empty playlist");
                    }
                }
            }
        }

        private void LevelSelector_LevelToPlaylist(On.Menu.LevelSelector.orig_LevelToPlaylist orig, Menu.LevelSelector self, string levelName)
        {
            if (isArenaMode(out var arena))
            {
                foreach (var player in OnlineManager.players)
                {
                    if (player.id == OnlineManager.lobby.owner.id || player.isMe)
                    {
                        continue;
                    }
                    player.InvokeOnceRPC(ArenaRPCs.Arena_LevelToPlaylist, levelName);

                }
                if (!OnlineManager.lobby.isOwner)
                {
                    return;
                }
                if (OnlineManager.lobby.isOwner)
                {
                    arena.playList.Add(levelName);
                }
            }
            orig(self, levelName);

        }

        private bool MultiplayerUnlocks_IsLevelUnlocked(On.MultiplayerUnlocks.orig_IsLevelUnlocked orig, MultiplayerUnlocks self, string levelName)
        {
            if (isArenaMode(out var _))
            {

                return true;

            }
            return orig(self, levelName);
        }


        private bool ArenaGameSession_EndOfSessionLogPlayerAsAlive(On.ArenaGameSession.orig_EndOfSessionLogPlayerAsAlive orig, ArenaGameSession self, int playerNumber)
        {
            if (isArenaMode(out var arena))
            {
                var onlinePlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, playerNumber);
                for (int i = 0; i < self.exitManager.playersInDens.Count; i++)
                {

                    if (!OnlinePhysicalObject.map.TryGetValue(self.exitManager.playersInDens[i].creature.abstractCreature, out var onlineAC))
                    {
                        RainMeadow.Error("Error getting online AC from playersInDens!");
                        return false;

                    }
                    if (onlineAC.owner == onlinePlayer)
                    {
                        RainMeadow.Debug("Found player in den match");
                        return true;
                    }
                }

                for (int j = 0; j < self.Players.Count; j++)
                {
                    if (!OnlinePhysicalObject.map.TryGetValue(self.Players[j], out var onlineAC))
                    {
                        RainMeadow.Error("Error getting online AC from players!");
                        return false;

                    }
                    if (onlineAC.owner == onlinePlayer)
                    {
                        RainMeadow.Debug("Found Player state end session");
                        return self.Players[j].state.alive;

                    }
                }
            }
            return orig(self, playerNumber);
        }

        private void ArenaSitting_SessionEnded(On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self, ArenaGameSession session)
        {
            if (isArenaMode(out var arena))
            {
                int score = 0;
                for (int i = 0; i < self.players.Count; i++)
                {
                    self.players[i].alive = session.EndOfSessionLogPlayerAsAlive(self.players[i].playerNumber);
                    if (self.players[i].alive)
                    {
                        self.players[i].AddSandboxScore(self.gameTypeSetup.survivalScore);
                    }
                    self.players[i].score += 100 * self.players[i].sandboxWin;
                    score += self.players[i].score;
                }

                List<ArenaSitting.ArenaPlayer> list = new List<ArenaSitting.ArenaPlayer>();


                for (int m = 0; m < self.players.Count; m++)
                {
                    ArenaSitting.ArenaPlayer arenaPlayer = self.players[m];
                    bool flag = false;
                    for (int n = 0; n < list.Count; n++)
                    {
                        if (self.PlayerSessionResultSort(arenaPlayer, list[n]))
                        {
                            list.Insert(n, arenaPlayer);
                            flag = true;
                            break;
                        }
                    }

                    if (!flag)
                    {
                        list.Add(arenaPlayer);
                    }
                }


                if (self.gameTypeSetup.gameType == ArenaSetup.GameTypeID.Competitive)
                {
                    if (list.Count == 1)
                    {
                        list[0].winner = list[0].alive;
                    }
                    else if (list.Count > 1)
                    {
                        if (list[0].alive && !list[1].alive)
                        {
                            list[0].winner = true;
                        }
                        else if (list[0].score > list[1].score)
                        {
                            list[0].winner = true;
                        }
                    }
                }
                // More gamemodes here?


                for (int num2 = 0; num2 < list.Count; num2++)
                {
                    if (list[num2].winner)
                    {
                        list[num2].wins++;
                    }

                    if (!self.players[num2].alive)
                    {
                        self.players[num2].deaths++;
                    }

                    self.players[num2].totScore += self.players[num2].score;
                }

                session.game.arenaOverlay = new Menu.ArenaOverlay(session.game.manager, self, list);
                session.game.manager.sideProcesses.Add(session.game.arenaOverlay);
            }
            else
            {
                orig(self, session);
            }

        }

        private Player.InputPackage RWInput_PlayerUIInput_int(On.RWInput.orig_PlayerUIInput_int orig, int playerNumber)
        {
            if (isArenaMode(out var _))
            {
                playerNumber = 0;
            }
            return orig(playerNumber);

        }

        private Player.InputPackage RWInput_PlayerInputLogic_int_int(On.RWInput.orig_PlayerInputLogic_int_int orig, int categoryID, int playerNumber)
        {
            if (isArenaMode(out var _))
            {
                playerNumber = 0;
            }
            return orig(categoryID, playerNumber);

        }

        private Rewired.Controller RWInput_PlayerRecentController_int(On.RWInput.orig_PlayerRecentController_int orig, int playerNumber)
        {
            if (isArenaMode(out var _))
            {
                playerNumber = 0;
            }
            return orig(playerNumber);

        }

        private void ArenaGameSession_ctor(On.ArenaGameSession.orig_ctor orig, ArenaGameSession self, RainWorldGame game)
        {
            orig(self, game);
            if (isArenaMode(out var arena))
            {
                self.outsidePlayersCountAsDead = false; // prevent killing scugs in dens
                On.ProcessManager.RequestMainProcessSwitch_ProcessID += ProcessManager_RequestMainProcessSwitch_ProcessID;
            }


        }
        private void OverwriteArenaPlayerMax(ILContext il) => OverwriteArenaPlayerMax(il, false);


        // Thank you, Dragon-Seeker
        private void OverwriteArenaPlayerMax(ILContext il, bool checkLdarg = false, int maxReplace = -1)
        {

            List<Func<Instruction, bool>> predicates = new List<Func<Instruction, bool>>();

            if (checkLdarg) predicates.Add(i => i.MatchLdarg(0));

            predicates.Add(i => i.MatchLdcI4(4));

            var cursor = new ILCursor(il);
            var x = 0;

            while (cursor.TryGotoNext(MoveType.After, predicates.ToArray()))
            {
                x++;

                cursor.EmitDelegate((int oldNum) =>
                {
                    if (isArenaMode(out var arena))
                    {
                        return arena.arenaSittingOnlineOrder.Count;
                    }
                    return oldNum;
                });

                if (maxReplace == x) break;
            }

            if (x == 0)
            {
                Logger.LogError($"Error in adjusting ArenaPlayerMax at Method: {il.Method.Name}]");
            }
            else
            {
                Logger.LogInfo($"Increased player count: Method at {il.Method.Name}]");
            }
        }


        private void ShortcutGraphics_ChangeAllExitsToSheltersOrDots(On.ShortcutGraphics.orig_ChangeAllExitsToSheltersOrDots orig, ShortcutGraphics self, bool toShelters)
        {
            if (isArenaMode(out var arena))
            {

                if (self.room == null)
                {
                    return;
                }
                if (self.room.shortcuts == null)
                {
                    return;
                }

                orig(self, toShelters);
            }
            else
            {
                orig(self, toShelters);
            }
        }

        private void ArenaGameSession_Killing(On.ArenaGameSession.orig_Killing orig, ArenaGameSession self, Player player, Creature killedCrit)
        {
            if (isArenaMode(out var arena))
            {

                RainMeadow.Debug(this);
                if (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var roomSession))
                {
                    Error("Error getting exit manager room");
                }

                if (!OnlinePhysicalObject.map.TryGetValue(player.abstractCreature, out var absPlayerCreature))
                {
                    Error("Error getting abs Player Creature");
                }

                if (!OnlinePhysicalObject.map.TryGetValue(killedCrit.abstractCreature, out var targetAbsCreature))
                {
                    Error("Error getting targetAbsCreature");
                }

                foreach (var onlinePlayer in OnlineManager.players)
                {
                    if (!onlinePlayer.isMe)
                    {
                        //self.playersContinueButtons = null;
                        onlinePlayer.InvokeOnceRPC(ArenaRPCs.Arena_Killing, absPlayerCreature, targetAbsCreature, onlinePlayer.id.name);
                    }
                    else
                    {
                        if (self.sessionEnded || (ModManager.MSC && player.AI != null))
                        {
                            return;
                        }

                        IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(killedCrit.abstractCreature);

                        for (int i = 0; i < self.arenaSitting.players.Count; i++)
                        {

                            if (absPlayerCreature.owner.inLobbyId == arena.arenaSittingOnlineOrder[i])
                            {

                                if (CreatureSymbol.DoesCreatureEarnATrophy(killedCrit.Template.type))
                                {
                                    self.arenaSitting.players[i].roundKills.Add(iconSymbolData);
                                    self.arenaSitting.players[i].allKills.Add(iconSymbolData);
                                }

                                int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconSymbolData).Index;
                                if (index >= 0)
                                {
                                    self.arenaSitting.players[i].AddSandboxScore(self.arenaSitting.gameTypeSetup.killScores[index]);
                                }
                                else
                                {
                                    self.arenaSitting.players[i].AddSandboxScore(0);
                                }

                                break;
                            }

                        }
                        if (!CreatureSymbol.DoesCreatureEarnATrophy(killedCrit.Template.type))
                        {
                            return;
                        }

                    }


                }

            }
            else
            {
                orig(self, player, killedCrit);
            }
        }

        // TODO: Unused for Comp?
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

        private void PlayerResultBox_ctor(On.Menu.PlayerResultBox.orig_ctor orig, Menu.PlayerResultBox self, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, Vector2 size, ArenaSitting.ArenaPlayer player, int index)
        {

            orig(self, menu, owner, pos, size, player, index); // stupid rectangle
            if (self.backgroundRect == null)
            {
                RainMeadow.Debug("Rectangle went missing. Bringing it back");
                self.backgroundRect = new Menu.RoundedRect(menu, self, new Vector2(0.01f, 0.01f), size, filled: true);
                self.subObjects.Add(self.backgroundRect);
            }
            if (isArenaMode(out var arena) && self.backgroundRect != null)
            {

                var currentName = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, self.player.playerNumber);
                self.playerNameLabel.text = currentName.id.name;

                if (!ModManager.MSC)
                {
                    // TODO: Test this with recent arenasitting changes
                    self.portrait.RemoveSprites();
                    menu.pages[0].RemoveSubObject(self.portrait);
                    var portaitMapper = (player.playerClass == SlugcatStats.Name.White) ? 0 :
                          (player.playerClass == SlugcatStats.Name.Yellow) ? 1 :
                          (player.playerClass == SlugcatStats.Name.Red) ? 2 :
                          (player.playerClass == SlugcatStats.Name.Night) ? 3 : 0;


                    self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + portaitMapper + (self.DeadPortraint ? "0" : "1"), new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);
                    self.subObjects.Add(self.portrait);

                }
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
            if (isArenaMode(out var arena))
            {

                var exitButton = new Menu.SimpleButton(self, self.pages[0], self.Translate("EXIT"), "EXIT", new Vector2(856f, 50f), new Vector2(110f, 30f));
                self.pages[0].subObjects.Add(exitButton);
            }
        }

        private void MultiplayerResults_Singal(On.Menu.MultiplayerResults.orig_Singal orig, Menu.MultiplayerResults self, Menu.MenuObject sender, string message)
        {
            if (isArenaMode(out var arena))
            {

                if (message != null)
                {
                    if (message == "CONTINUE")
                    {
                        self.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.ArenaLobbyMenu);
                        self.manager.rainWorld.options.DeleteArenaSitting();
                        self.PlaySound(SoundID.MENU_Switch_Page_In);

                    }

                    if (message == "EXIT")
                    {

                        self.manager.rainWorld.options.DeleteArenaSitting();
                        OnlineManager.LeaveLobby();
                        self.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.LobbySelectMenu);
                        self.PlaySound(SoundID.MENU_Switch_Page_In);
                    }
                    self.ArenaSitting.players.Clear();
                    arena.returnToLobby = true;



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
                self.AddPart(new Pointing(self));

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

                    if (!roomSession.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(ArenaRPCs.AddShortCutVessel, new RWCustom.IntVector2(-1, -1), onlineVessel, roomSession, 0)))
                    {
                        foreach (OnlinePlayer player in OnlineManager.players)
                        {
                            if (roomSession.isOwner)
                            {

                                ArenaRPCs.AddShortCutVessel(new RWCustom.IntVector2(-1, -1), onlineVessel, roomSession, 0);
                            }
                            else
                            {
                                player.InvokeRPC(ArenaRPCs.AddShortCutVessel, new RWCustom.IntVector2(-1, -1), onlineVessel, roomSession, 0);

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


        private void ArenaOverlay_PlayerPressedContinue(On.Menu.ArenaOverlay.orig_PlayerPressedContinue orig, Menu.ArenaOverlay self)
        {
            if (isArenaMode(out var arena))
            {


                if (!OnlineManager.lobby.isOwner)
                {
                    self.playersContinueButtons = null;
                    self.PlaySound(SoundID.UI_Multiplayer_Player_Result_Box_Player_Ready);

                    //for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
                    //{
                    //    if (self.resultBoxes[i].playerNameLabel.text == OnlineManager.mePlayer.id.name)
                    //    {
                    //        self.result[i].readyForNextRound = true;
                    //    }
                    //}

                    //foreach (var player in OnlineManager.players)
                    //{

                    //    if (!player.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(RPCs.Arena_ReadyForNextLevel, player.id.name)))
                    //    {
                    //        player.InvokeRPC(RPCs.Arena_ReadyForNextLevel, OnlineManager.mePlayer.id.name);
                    //    }

                    //}

                }
                else
                {
                    for (int i = 0; i < arena.arenaSittingOnlineOrder.Count; i++)
                    {
                        self.result[i].readyForNextRound = true;

                    }
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
                if (self.resultBoxes[0].backgroundRect == null)
                {
                    return;
                }

                if (self.countdownToNextRound == 0 && !self.nextLevelCall)
                {
                    foreach (OnlinePlayer player in OnlineManager.players)
                    {
                        if (player.id == OnlineManager.lobby.owner.id && arena.clientWaiting == arena.arenaSittingOnlineOrder.Count - 1)
                        {
                            ArenaRPCs.Arena_NextLevelCall();
                        }

                        else
                        {
                            player.InvokeRPC(ArenaRPCs.Arena_IncrementPlayersLeftt);
                            player.InvokeRPC(ArenaRPCs.Arena_NextLevelCall);


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
            orig(self);

            if (isArenaMode(out var arena))
            {
                if (self.Players.Count != arena.arenaSittingOnlineOrder.Count)
                {
                    var extraPlayers = self.Players.Skip(OnlineManager.players.Count).ToList();
                    self.Players.RemoveAll(p => extraPlayers.Contains(p));

                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !self.Players.Contains(ac))
                        {
                            self.Players.Add(ac);
                        }
                    }
                }


            }
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
                if (self.room.shortcuts == null)
                {
                    return;
                }
                if (!self.room.shortCutsReady)
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

            if (isArenaMode(out var _))
            {
                var deadCount = 0;
                foreach (var player in self.gameSession.Players)
                {
                    if (player.realizedCreature != null && (player.realizedCreature.State.dead || player.state.dead))
                    {

                        deadCount++;
                    }
                }

                if (deadCount != 0 && deadCount == self.gameSession.Players.Count - 1)
                {

                    return true;
                }

                if (self.world.rainCycle.TimeUntilRain <= 100)
                {
                    return true;
                }

                orig(self);
            }

                return orig(self);

        }


        private void ArenaGameSession_SpawnPlayers(On.ArenaGameSession.orig_SpawnPlayers orig, ArenaGameSession self, Room room, List<int> suggestedDens)
        {

            if (isArenaMode(out var arena))
            {

                List<OnlinePlayer> list = new List<OnlinePlayer>();


                List<OnlinePlayer> list2 = new List<OnlinePlayer>();
                for (int j = 0; j < arena.arenaSittingOnlineOrder.Count; j++)
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
                RainMeadow.Debug("assigned ac, registering");
                self.game.world.GetResource().ApoEnteringWorld(abstractCreature);
                sSpawningAvatar = false;

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

            else
            {
                orig(self, room, suggestedDens);
            }
        }
    }
}
