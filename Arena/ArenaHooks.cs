using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public static bool isArenaMode(out ArenaOnlineGameMode gameMode)
        {
            gameMode = null;
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is ArenaOnlineGameMode arena)
            {
                gameMode = arena;
                return true;
            }
            return false;
        }

        public static bool isArenaCompetitive(ArenaOnlineGameMode arena)
        {
            if (arena.currentGameMode == ArenaSetup.GameTypeID.Competitive.value)
            {
                return true;
            }
            return false;
        }

        public static bool killedCreatures;

        private void ArenaHooks()
        {

            On.Spear.Update += Spear_Update;


            On.ArenaGameSession.SpawnPlayers += ArenaGameSession_SpawnPlayers;
            On.ArenaGameSession.Update += ArenaGameSession_Update;
            On.ArenaGameSession.EndSession += ArenaGameSession_EndSession;
            On.ArenaGameSession.EndOfSessionLogPlayerAsAlive += ArenaGameSession_EndOfSessionLogPlayerAsAlive;
            On.ArenaGameSession.Killing += ArenaGameSession_Killing;
            On.ArenaGameSession.SpawnCreatures += ArenaGameSession_SpawnCreatures;
            On.ArenaGameSession.ctor += ArenaGameSession_ctor;
            On.ArenaGameSession.PlayersStillActive += ArenaGameSession_PlayersStillActive;
            On.ArenaGameSession.PlayerLandSpear += ArenaGameSession_PlayerLandSpear;
            On.ArenaGameSession.ScoreOfPlayer += ArenaGameSession_ScoreOfPlayer;
            On.ArenaGameSession.SpawnItem += ArenaGameSession_SpawnItem;
            IL.ArenaGameSession.ctor += OverwriteArenaPlayerMax;

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
            On.Menu.ArenaSettingsInterface.SetSelected += ArenaSettingsInterface_SetSelected;
            On.Menu.ArenaSettingsInterface.SetChecked += ArenaSettingsInterface_SetChecked;
            On.Menu.ArenaSettingsInterface.ctor += ArenaSettingsInterface_ctor;
            On.Menu.ArenaSettingsInterface.Update += ArenaSettingsInterface_Update;

            On.Menu.LevelSelector.LevelToPlaylist += LevelSelector_LevelToPlaylist;
            On.Menu.LevelSelector.LevelFromPlayList += LevelSelector_LevelFromPlayList;
            On.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons += MultiplayerMenu_InitiateGameTypeSpecificButtons;
            On.Menu.MultiplayerMenu.ArenaImage += MultiplayerMenu_ArenaImage;
            On.Menu.MultiplayerMenu.ctor += MultiplayerMenu_ctor;
            On.Menu.PauseMenu.Singal += PauseMenu_Singal;

            IL.CreatureCommunities.ctor += OverwriteArenaPlayerMax;
            On.CreatureCommunities.LikeOfPlayer += CreatureCommunities_LikeOfPlayer;
            On.RWInput.PlayerRecentController_int += RWInput_PlayerRecentController_int;
            On.RWInput.PlayerInputLogic_int_int += RWInput_PlayerInputLogic_int_int;
            On.RWInput.PlayerUIInput_int += RWInput_PlayerUIInput_int;

            On.MultiplayerUnlocks.IsLevelUnlocked += MultiplayerUnlocks_IsLevelUnlocked;
            On.MultiplayerUnlocks.IsCreatureUnlockedForLevelSpawn += MultiplayerUnlocks_IsCreatureUnlockedForLevelSpawn;


            On.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
            On.CreatureSymbol.ColorOfCreature += CreatureSymbol_ColorOfCreature;
            On.MoreSlugcats.SingularityBomb.ctor += SingularityBomb_ctor;
            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint1;
        }

        private void Player_ClassMechanicsSaint1(ILContext il)
        {

            try
            {
                var c = new ILCursor(il);
                ILLabel skip = il.DefineLabel();
                c.GotoNext(
                     i => i.MatchLdloc(18),
                     i => i.MatchIsinst<Creature>(),
                     i => i.MatchCallvirt<Creature>("Die")
                     );
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, 18);
                c.EmitDelegate((Player self, PhysicalObject po) =>
                {
                    if (self.IsLocal() && isArenaMode(out var _))
                    {
                        if (OnlinePhysicalObject.map.TryGetValue(po.abstractPhysicalObject, out var opo))
                        {
                            opo.owner.InvokeOnceRPC(RPCs.Creature_Die, opo);
                        }
                    }
                });

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

        }

        private void SingularityBomb_ctor(On.MoreSlugcats.SingularityBomb.orig_ctor orig, SingularityBomb self, AbstractPhysicalObject abstractPhysicalObject, World world)
        {
            if (isArenaMode(out var _))
            {
                self.zeroMode = true;
                orig(self, abstractPhysicalObject, world);
            }
            else
            {
                orig(self, abstractPhysicalObject, world);
            }
        }


        private void ArenaGameSession_SpawnItem(On.ArenaGameSession.orig_SpawnItem orig, ArenaGameSession self, Room room, PlacedObject placedObj)
        {
            if (isArenaMode(out var _) && ((placedObj.data as PlacedObject.MultiplayerItemData).type == PlacedObject.MultiplayerItemData.Type.SporePlant))
            {

                return;

            }
            else
            {
                orig(self, room, placedObj);

            }
        }

        private float CreatureCommunities_LikeOfPlayer(On.CreatureCommunities.orig_LikeOfPlayer orig, CreatureCommunities self, CreatureCommunities.CommunityID commID, int region, int playerNumber)
        {
            if (isArenaMode(out var _))
            {
                playerNumber = 0;
            }
            return orig(self, commID, region, playerNumber);
        }

        private void ArenaGameSession_EndSession(On.ArenaGameSession.orig_EndSession orig, ArenaGameSession self)
        {
            orig(self);
            if (isArenaMode(out var _))
            {
                if (!killedCreatures)
                {
                    if (RoomSession.map.TryGetValue(self.room.abstractRoom, out var roomSession))
                    {
                        // we go over all APOs in the room
                        var entities = self.room.abstractRoom.entities;
                        for (int i = entities.Count - 1; i >= 0; i--)
                        {
                            if (entities[i] is AbstractPhysicalObject apo && OnlinePhysicalObject.map.TryGetValue(apo, out var oe) && apo is AbstractCreature ac && ac.creatureTemplate.type.value != CreatureTemplate.Type.Slugcat.value)
                            {
                                for (int num = ac.stuckObjects.Count - 1; num >= 0; num--)
                                {
                                    if (ac.stuckObjects[num] is AbstractPhysicalObject.AbstractSpearStick && ac.stuckObjects[num].A.type == AbstractPhysicalObject.AbstractObjectType.Spear && ac.stuckObjects[num].A.realizedObject != null)
                                    {
                                        (ac.stuckObjects[num].A.realizedObject as Spear).ChangeMode(Weapon.Mode.Free);
                                    }
                                }
                                if (ac.realizedCreature != null && ac.realizedCreature.State.alive && ac.realizedCreature.grasps == null)
                                {
                                    ac.realizedCreature.Die();
                                }

                                oe.apo.LoseAllStuckObjects();
                                if (!oe.isMine)
                                {
                                    // not-online-aware removal
                                    Debug("removing remote entity from game " + oe);
                                    oe.beingMoved = true;

                                    if (oe.apo.realizedObject is Creature c && c.inShortcut)
                                    {
                                        if (c.RemoveFromShortcuts()) c.inShortcut = false;
                                    }

                                    entities.Remove(oe.apo);

                                    self.room.abstractRoom.creatures.Remove(oe.apo as AbstractCreature);

                                    self.room.RemoveObject(oe.apo.realizedObject);
                                    self.room.CleanOutObjectNotInThisRoom(oe.apo.realizedObject);
                                    oe.beingMoved = false;
                                }
                                else // mine leave the old online world elegantly
                                {
                                    Debug("removing my entity from online " + oe);
                                    oe.ExitResource(roomSession);
                                    oe.ExitResource(roomSession.worldSession);
                                }
                            }
                        }
                    }
                    killedCreatures = true;
                }
            }
        }

        private void PauseMenu_Singal(On.Menu.PauseMenu.orig_Singal orig, Menu.PauseMenu self, Menu.MenuObject sender, string message)
        {
            if (message == "EXIT" && isArenaMode(out var arena))
            {
                arena.returnToLobby = true;
            }
            orig(self, sender, message);
        }

        private bool MultiplayerUnlocks_IsCreatureUnlockedForLevelSpawn(On.MultiplayerUnlocks.orig_IsCreatureUnlockedForLevelSpawn orig, MultiplayerUnlocks self, CreatureTemplate.Type tp)
        {
            if (isArenaMode(out var _))
            {

                return true;

            }
            return orig(self, tp);
        }

        private Color CreatureSymbol_ColorOfCreature(On.CreatureSymbol.orig_ColorOfCreature orig, IconSymbol.IconSymbolData iconData)
        {
            if (isArenaMode(out var _))
            {
                if (iconData.critType == CreatureTemplate.Type.Slugcat)
                {
                    return Color.white;
                }
            }

            return orig(iconData);
        }

        private void ArenaSettingsInterface_Update(On.Menu.ArenaSettingsInterface.orig_Update orig, Menu.ArenaSettingsInterface self)
        {
            orig(self);
            if (isArenaMode(out var _) && self.spearsHitCheckbox != null && OnlineManager.lobby.isOwner)
            {
                self.spearsHitCheckbox.buttonBehav.greyedOut = false;

            }
        }
        private void MultiplayerMenu_ctor(On.Menu.MultiplayerMenu.orig_ctor orig, Menu.MultiplayerMenu self, ProcessManager manager)
        {
            if (isArenaMode(out var arena)) // normally we would work this into a new arena game type but we need the instance for all the goodies inside it each time we back out of the menu and come back
            {
                var comp = new Competitive();
                if (!arena.registeredGameModes.ContainsKey(comp))
                {
                    arena.registeredGameModes.Add(new Competitive(), Competitive.CompetitiveMode.value);
                }
            }

            orig(self, manager);

        }

        private string MultiplayerMenu_ArenaImage(On.Menu.MultiplayerMenu.orig_ArenaImage orig, Menu.MultiplayerMenu self, SlugcatStats.Name classID, int color)
        {
            if (isArenaMode(out var arena))
            {

                if (classID == null)
                {
                    return "MultiplayerPortrait" + color + "2";
                }

                var slugList = ArenaHelpers.AllSlugcats();
                var baseGameSlugs = ArenaHelpers.BaseGameSlugcats();

                RainMeadow.Debug("Player is playing as " + classID + "with color index " + color);

                if (baseGameSlugs.Contains(classID) && color <= 3)
                {
                    return "MultiplayerPortrait" + color + "1";
                }

                if (ModManager.MSC && color > 3 && baseGameSlugs.Contains(classID))
                {
                    if (classID == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel)
                    {
                        int randomChoice = UnityEngine.Random.Range(0, 5);
                        return "MultiplayerPortrait" + $"{randomChoice}1-" + slugList[color];
                    }
                    return "MultiplayerPortrait" + "41-" + slugList[color];

                }

                if (!baseGameSlugs.Contains(classID))
                {

                    color = 0;
                    return "MultiplayerPortrait" + color + "1-" + classID.ToString();
                }
                return orig(self, classID, color);
            }
            else
            {
                return orig(self, classID, color);
            }
        }

        private void ArenaGameSession_PlayerLandSpear(On.ArenaGameSession.orig_PlayerLandSpear orig, ArenaGameSession self, Player player, Creature target)
        {

            if (isArenaMode(out var arena))
            {

                if (self.sessionEnded || self.GameTypeSetup.spearHitScore == 0 || !CreatureSymbol.DoesCreatureEarnATrophy(target.Template.type))
                {
                    return;
                }

                if (!player.IsLocal())
                {
                    return;
                }
                for (int i = 0; i < self.arenaSitting.players.Count; i++)
                {

                    if (!OnlinePhysicalObject.map.TryGetValue(player.abstractPhysicalObject, out var op))
                    {

                        RainMeadow.Error("Could not get PlayerLandSpear player");
                    }
                    var onlineArenaPlayer = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, self.arenaSitting.players[i].playerNumber);

                    if (op.owner != onlineArenaPlayer)
                    {
                        continue;
                    }
                    arena.onlineArenaGameMode.LandSpear(arena, self, player, target, self.arenaSitting.players[i]);

                }

            }
            else
            {
                orig(self, player, target);
            }
        }

        private int ArenaGameSession_PlayersStillActive(On.ArenaGameSession.orig_PlayersStillActive orig, ArenaGameSession self, bool addToAliveTime, bool dontCountSandboxLosers)
        {
            if (isArenaMode(out var arena))
            {
                int num = 0;
                for (int i = 0; i < self.Players.Count; i++)
                {
                    bool flag = true;
                    if (!self.Players[i].state.alive)
                    {
                        flag = false;
                    }

                    if (flag && self.exitManager != null && self.exitManager.IsPlayerInDen(self.Players[i]))
                    {
                        flag = false;
                    }

                    if (flag && self.Players[i].realizedCreature != null && (self.Players[i].realizedCreature as Player).dangerGrasp != null)
                    {
                        flag = false;
                    }

                    if (flag)
                    {
                        for (int j = 0; j < self.arenaSitting.players.Count; j++)
                        {

                            if (self.Players[i].Room == self.game.world.offScreenDen && self.arenaSitting.players[j].hasEnteredGameArea)
                            {
                                flag = false;
                            }

                            if (dontCountSandboxLosers && self.arenaSitting.players[j].sandboxWin < 0)
                            {
                                flag = false;
                            }

                            break;

                        }
                    }

                    if (flag)
                    {
                        num++;
                    }
                }

                return num;
            }
            else
            {
                return orig(self, addToAliveTime, dontCountSandboxLosers);
            }
        }

        private void Player_ClassMechanicsSaint(On.Player.orig_ClassMechanicsSaint orig, Player self)
        {

            orig(self);
            if (isArenaMode(out var _))
            {
                var duration = 0.35f * (self.maxGodTime / 400f); // we'll see how that feels for now
                self.godTimer = Mathf.Min(self.godTimer + duration, self.maxGodTime);

            }

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

                    var spearsHitSync = self.spearsHitCheckbox.IDString;
                    var spearsHitValue = self.GetGameTypeSetup.spearsHitPlayers;

                    var aggroAISync = self.evilAICheckBox.IDString;
                    var aggroAISyncValue = self.GetGameTypeSetup.evilAI;

                    arena.onlineArenaSettingsInterfaceMultiChoice.Add(roomRepeatSync, roomRepeatValue);
                    arena.onlineArenaSettingsInterfaceMultiChoice.Add(rainSyncKey, rainSyncValue);
                    arena.onlineArenaSettingsInterfaceMultiChoice.Add(wildLifeSyncKey, wildLifeSyncValue);

                    arena.onlineArenaSettingsInterfaceeBool.Add(spearsHitSync, spearsHitValue);
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

            }
            else
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
                    arena.playList = self.levelsPlaylist.PlayList;

                }
            }
        }

        private void LevelSelector_LevelToPlaylist(On.Menu.LevelSelector.orig_LevelToPlaylist orig, Menu.LevelSelector self, string levelName)
        {
            if (isArenaMode(out var arena))
            {
                foreach (var player in OnlineManager.players)
                {
                    if (player.id == OnlineManager.lobby.owner.id)
                    {
                        continue;
                    }
                    player.InvokeOnceRPC(ArenaRPCs.Arena_LevelToPlaylist, levelName);

                }
                if (!OnlineManager.lobby.isOwner)
                {
                    return;
                }
                orig(self, levelName);
                arena.playList = self.levelsPlaylist.PlayList;
                foreach (var i in arena.playList)
                {
                    RainMeadow.Debug(i);
                }
            }
            else
            {
                orig(self, levelName);
            }

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
                killedCreatures = false;
                if (!ModManager.MSC)
                {
                    self.characterStats = new SlugcatStats(arena.avatarSettings.playingAs, false); // limited support for fun stuff outside MSC
                }
                self.outsidePlayersCountAsDead = false; // prevent killing scugs in dens
                arena.onlineArenaGameMode.ArenaSessionCtor(arena, orig, self, game);
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

                for (int i = 0; i < self.room.shortcuts.Length; i++)
                {
                    // Ensure that i is within bounds for both arrays
                    if (i < self.entranceSprites.GetLength(0) && self.entranceSprites[i, 0] != null)
                    {
                        if (self.room.shortcuts[i].shortCutType == ShortcutData.Type.RoomExit)
                        {
                            self.entranceSprites[i, 0].element = Futile.atlasManager.GetElementWithName(toShelters ? "ShortcutShelter" : "ShortcutDots");
                        }
                    }
                    else
                    {
                        RainMeadow.Debug("Index out of bounds for entranceSprites or entranceSprites[i, 0] is null.");
                    }
                }


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

                if (self.sessionEnded || (ModManager.MSC && player.AI != null))
                {
                    return;
                }

                IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(killedCrit.abstractCreature);

                for (int i = 0; i < self.arenaSitting.players.Count; i++)
                {
                    if (absPlayerCreature.owner.inLobbyId == arena.arenaSittingOnlineOrder[i])
                    {
                        arena.onlineArenaGameMode.Killing(arena, orig, self, player, killedCrit, i);

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
                self.portrait.RemoveSprites();
                menu.pages[0].RemoveSubObject(self.portrait);

                var currentName = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, self.player.playerNumber);
                var userNameBackup = "Unknown user";
                try
                {
                    userNameBackup = currentName.id.name;
                    self.playerNameLabel.text = userNameBackup;
                }
                catch
                {
                    self.playerNameLabel.text = userNameBackup;
                }


                if (!ModManager.MSC)
                {
                    if (ArenaHelpers.BaseGameSlugcats().Contains(player.playerClass))
                    {
                        var portaitMapper = (player.playerClass == SlugcatStats.Name.White) ? 0 :
                              (player.playerClass == SlugcatStats.Name.Yellow) ? 1 :
                              (player.playerClass == SlugcatStats.Name.Red) ? 2 :
                              (player.playerClass == SlugcatStats.Name.Night) ? 3 : 0;


                        self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + portaitMapper + (self.DeadPortraint ? "0" : "1"), new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);
                    }
                    else
                    {
                        if (arena.playerResultColors.ContainsKey(userNameBackup))
                        {
                            self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + arena.playerResultColors[userNameBackup] + (self.DeadPortraint ? "0" : "1") + "-" + player.playerClass.value, new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);
                        }
                        else
                        {
                            self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + "0" + (self.DeadPortraint ? "0" : "1"), new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);

                        }

                    }
                    self.subObjects.Add(self.portrait);

                }
                if (ModManager.MSC)
                {
                    if (player.playerClass == SlugcatStats.Name.Night)
                    {
                        self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + "3" + (self.DeadPortraint ? "0" : "1"), new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);
                    }

                    else if (player.playerClass == MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                    {
                        self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + "4" + (self.DeadPortraint ? "0" : "1") + "-" + player.playerClass.value, new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);
                    }
                    else
                    {
                        if (arena.playerResultColors.ContainsKey(userNameBackup))
                        {
                            self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + arena.playerResultColors[currentName.id.name] + (self.DeadPortraint ? "0" : "1") + "-" + player.playerClass.value, new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);

                        }
                        else
                        {
                            self.portrait = new Menu.MenuIllustration(menu, self, "", "MultiplayerPortrait" + "0" + (self.DeadPortraint ? "0" : "1") + "-" + player.playerClass.value, new Vector2(size.y / 2f, size.y / 2f), crispPixels: true, anchorCenter: true);

                        }
                    }
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
                    arena.returnToLobby = true;
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



                }

            }
            else
            {
                orig(self, sender, message);
            }
        }

        private void ArenaCreatureSpawner_SpawnArenaCreatures(On.ArenaCreatureSpawner.orig_SpawnArenaCreatures orig, RainWorldGame game, ArenaSetup.GameTypeSetup.WildLifeSetting wildLifeSetting, ref List<AbstractCreature> availableCreatures, ref MultiplayerUnlocks unlocks)
        {
            if (isArenaMode(out var arena))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    RainMeadow.Debug("Spawning creature");

                    arena.onlineArenaGameMode.ArenaCreatureSpawner_SpawnCreatures(arena, orig, game, wildLifeSetting, ref availableCreatures, ref unlocks);

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

            if (isArenaMode(out var arena))
            {
                arena.onlineArenaGameMode.HUD_InitMultiplayerHud(arena, self, session);
            }
            else
            {
                orig(self, session);
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
                    // self.playersContinueButtons = null;
                    self.PlaySound(SoundID.UI_Multiplayer_Player_Result_Box_Player_Ready);
                    return;

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
                        if (player.id == OnlineManager.lobby.owner.id && arena.playerLeftGame == arena.arenaSittingOnlineOrder.Count - 1)
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
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac && !self.Players.Contains(ac) && ac.state.alive)
                        {
                            self.Players.Add(ac);
                        }
                    }
                }

                arena.onlineArenaGameMode.ArenaSessionUpdate(arena, self);

                if (!self.sessionEnded)
                {
                    foreach (var s in self.arenaSitting.players)
                    {
                        var os = ArenaHelpers.FindOnlinePlayerByFakePlayerNumber(arena, s.playerNumber); // current player
                        {
                            for (int i = 0; i < self.Players.Count; i++)
                            {
                                if (OnlinePhysicalObject.map.TryGetValue(self.Players[i], out var onlineC))
                                {
                                    if (onlineC.owner == os && self.Players[i].realizedCreature != null && !self.Players[i].realizedCreature.State.dead)
                                    {
                                        s.timeAlive++;
                                    }
                                }
                                else
                                {
                                    if (self.Players[i].state.alive) // alive and without an owner? Die
                                    {
                                        self.Players[i].Die();
                                    }
                                }
                            }
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

            if (isArenaMode(out var arena))
            {
                return arena.onlineArenaGameMode.IsExitsOpen(arena, orig, self);

            }

            return orig(self);

        }


        private void ArenaGameSession_SpawnPlayers(On.ArenaGameSession.orig_SpawnPlayers orig, ArenaGameSession self, Room room, List<int> suggestedDens)
        {

            if (isArenaMode(out var arena))
            {

                arena.onlineArenaGameMode.SpawnPlayer(arena, self, room, suggestedDens);

            }

            else
            {
                orig(self, room, suggestedDens);
            }
        }
    }
}
