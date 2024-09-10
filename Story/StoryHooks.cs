using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private bool isPlayerReady = false;

        public static bool isStoryMode(out StoryGameMode gameMode)
        {
            gameMode = null;
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode sgm)
            {
                gameMode = sgm;
                return true;
            }
            return false;
        }

        private void StoryHooks()
        {
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState;
            On.PlayerProgression.SaveToDisk += PlayerProgression_SaveToDisk;
            On.Menu.SleepAndDeathScreen.ctor += SleepAndDeathScreen_ctor;
            On.Menu.SleepAndDeathScreen.Update += SleepAndDeathScreen_Update;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;

            On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;

            On.Player.Update += Player_Update;

            On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;


            On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide;
            On.RegionGate.PlayersStandingStill += PlayersStandingStill;
            On.RegionGate.PlayersInZone += RegionGate_PlayersInZone;

            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GameOver += RainWorldGame_GameOver;

            On.SaveState.BringUpToDate += SaveState_BringUpToDate;
            IL.SaveState.SessionEnded += SaveState_SessionEnded;

            On.WaterNut.Swell += WaterNut_Swell;
            On.SporePlant.Pacify += SporePlant_Pacify;

            On.PuffBall.Explode += PuffBall_Explode;

            On.Oracle.CreateMarble += Oracle_CreateMarble;
            On.Oracle.SetUpMarbles += Oracle_SetUpMarbles;
            On.Oracle.SetUpSwarmers += Oracle_SetUpSwarmers;
            On.OracleSwarmer.BitByPlayer += OracleSwarmer_BitByPlayer;
            On.SLOracleSwarmer.BitByPlayer += SLOracleSwarmer_BitByPlayer;
            On.CoralBrain.CoralNeuronSystem.PlaceSwarmers += OnCoralNeuronSystem_PlaceSwarmers;
            On.SSOracleSwarmer.NewRoom += SSOracleSwarmer_NewRoom;
            On.HUD.TextPrompt.Update += TextPrompt_Update;
            On.HUD.TextPrompt.UpdateGameOverString += TextPrompt_UpdateGameOverString;

        }


        private void TextPrompt_UpdateGameOverString(On.HUD.TextPrompt.orig_UpdateGameOverString orig, TextPrompt self, Options.ControlSetup.Preset controllerType)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    self.gameOverString = "A slugcat has fallen. Perform a rescue, shelter, or press PAUSE BUTTON to restart";
                }
                else
                {
                    self.gameOverString = $"A slugcat has fallen. Perform a rescue, shelter, or press";
                }
            }

            else
            {
                orig(self, controllerType);
            }
        }

        private void TextPrompt_Update(On.HUD.TextPrompt.orig_Update orig, TextPrompt self)
        {
            orig(self);
            if (isStoryMode(out var storyGameMode))
            {
                if (!OnlineManager.lobby.isOwner && self.currentlyShowing == TextPrompt.InfoID.GameOver)
                {
                    self.restartNotAllowed = 1; // block clients from GoToDeathScreen
                    bool touchedInput = false;

                    // let clients still have access to pause menu
                    for (int j = 0; j < self.hud.rainWorld.options.controls.Length; j++)
                    {
                        touchedInput = ((self.hud.rainWorld.options.controls[j].gamePad || !self.defaultMapControls[j]) ? (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(5) || RWInput.CheckPauseButton(0, inMenu: false)) : (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(11)));
                    }
                    if (touchedInput)
                    {
                        self.gameOverMode = false;
                    }
                }
            }
        }

            private void SSOracleSwarmer_NewRoom(On.SSOracleSwarmer.orig_NewRoom orig, SSOracleSwarmer self, Room newRoom)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self, newRoom);
                    return;
                }

                if (!ModManager.MSC)
                {
                    newRoom.abstractRoom.AddEntity(self.abstractPhysicalObject);
                }
            }

            //Only spawn if we own the room
            private void OnCoralNeuronSystem_PlaceSwarmers(On.CoralBrain.CoralNeuronSystem.orig_PlaceSwarmers orig, CoralBrain.CoralNeuronSystem self)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self);
                    return;
                }
                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (room.isOwner)
                {
                    orig(self);
                }
            }


            private void SLOracleSwarmer_BitByPlayer(On.SLOracleSwarmer.orig_BitByPlayer orig, SLOracleSwarmer self, Creature.Grasp grasp, bool eu)
            {
                orig(self, grasp, eu);
                if (self.slatedForDeletetion == true)
                {
                    SwarmerEaten();
                }
            }

            private void OracleSwarmer_BitByPlayer(On.OracleSwarmer.orig_BitByPlayer orig, OracleSwarmer self, Creature.Grasp grasp, bool eu)
            {
                orig(self, grasp, eu);
                if (self.slatedForDeletetion == true)
                {
                    SwarmerEaten();
                }
            }

            private void SwarmerEaten()
            {
                if (OnlineManager.lobby == null) return;
                if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
                {
                    if (!OnlineManager.lobby.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(ConsumableRPCs.enableTheGlow)))
                    {
                        OnlineManager.lobby.owner.InvokeRPC(ConsumableRPCs.enableTheGlow);
                    }
                }
            }

            private void Oracle_SetUpSwarmers(On.Oracle.orig_SetUpSwarmers orig, Oracle self)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self);
                    return;
                }

                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (room.isOwner)
                {
                    orig(self); //Only setup the room if we are the room owner.
                    foreach (var swamer in self.mySwarmers)
                    {
                        var apo = swamer.abstractPhysicalObject;
                        self.room.world.GetResource().ApoEnteringWorld(apo);
                        self.room.abstractRoom.GetResource().ApoEnteringRoom(apo, apo.pos);
                    }
                }
            }

            private void PuffBall_Explode(On.PuffBall.orig_Explode orig, PuffBall self)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self);
                    return;
                }

                RoomSession.map.TryGetValue(self.room.abstractRoom, out var onlineRoom);
                OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineSporePlant);

                if (onlineSporePlant.isMine)
                {
                    foreach (var kv in OnlineManager.lobby.playerAvatars)
                    {
                        var playerAvatar = kv.Value;
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none || kv.Key.isMe) continue; // not in game or is me
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                        {
                            if (ac.Room == self.room.abstractRoom)
                            {
                                if (!opo.owner.OutgoingEvents.Any(e => e is RPCEvent rpc && rpc.IsIdentical(ConsumableRPCs.explodePuffBall, onlineSporePlant, self.bodyChunks[0].pos)))
                                {
                                    opo.owner.InvokeRPC(ConsumableRPCs.explodePuffBall, onlineRoom, self.bodyChunks[0].pos, self.sporeColor, self.color);
                                }
                            }
                        }
                    }
                    orig(self);
                    return;
                }
            }

            private void Oracle_SetUpMarbles(On.Oracle.orig_SetUpMarbles orig, Oracle self)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self);
                    return;
                }

                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (room.isOwner)
                {
                    orig(self); //Only setup the room if we are the room owner.
                }
            }

            private void Oracle_CreateMarble(On.Oracle.orig_CreateMarble orig, Oracle self, PhysicalObject orbitObj, Vector2 ps, int circle, float dist, int color)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self, orbitObj, ps, circle, dist, color);
                    return;
                }

                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (room.isOwner)
                {
                    AbstractPhysicalObject abstractPhysicalObject = new PebblesPearl.AbstractPebblesPearl(self.room.world, null, self.room.GetWorldCoordinate(ps), self.room.game.GetNewID(), -1, -1, null, color, self.pearlCounter * ((ModManager.MSC && self.room.world.name == "DM") ? -1 : 1));
                    self.pearlCounter++;
                    self.room.abstractRoom.AddEntity(abstractPhysicalObject);

                    abstractPhysicalObject.RealizeInRoom();

                    PebblesPearl pebblesPearl = abstractPhysicalObject.realizedObject as PebblesPearl;
                    pebblesPearl.oracle = self;
                    pebblesPearl.firstChunk.HardSetPosition(ps);
                    pebblesPearl.orbitObj = orbitObj;
                    if (orbitObj == null)
                    {
                        pebblesPearl.hoverPos = new Vector2?(ps);
                    }
                    pebblesPearl.orbitCircle = circle;
                    pebblesPearl.orbitDistance = dist;
                    pebblesPearl.marbleColor = (abstractPhysicalObject as PebblesPearl.AbstractPebblesPearl).color;
                    self.marbles.Add(pebblesPearl);
                }
                else
                {
                    return;
                }
            }

            private void SporePlant_Pacify(On.SporePlant.orig_Pacify orig, SporePlant self)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self);
                    return;
                }

                RoomSession.map.TryGetValue(self.room.abstractRoom, out var room);
                if (!room.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
                {
                    OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineSporePlant);
                    room.owner.InvokeRPC(ConsumableRPCs.pacifySporePlant, onlineSporePlant);
                }
                else
                {
                    orig(self);
                }
            }

            private void WaterNut_Swell(On.WaterNut.orig_Swell orig, WaterNut self)
            {
                if (OnlineManager.lobby == null)
                {
                    orig(self);
                    return;
                }
                self.room.PlaySound(SoundID.Water_Nut_Swell, self.firstChunk.pos);

                var abstractWaterNut = self.abstractPhysicalObject as WaterNut.AbstractWaterNut;
                OnlinePhysicalObject.map.TryGetValue(abstractWaterNut, out var onlineWaterNut);

                if (onlineWaterNut.isMine && OnlineManager.lobby.gameMode is StoryGameMode)
                {
                    if (self.grabbedBy.Count > 0)
                    {
                        self.grabbedBy[0].Release();
                    }

                    EntityID id = self.room.world.game.GetNewID();
                    var abstractSwollenWaterNut = new WaterNut.AbstractWaterNut(abstractWaterNut.world, null, abstractWaterNut.pos, id, abstractWaterNut.originRoom, abstractWaterNut.placedObjectIndex, null, true);
                    self.room.abstractRoom.AddEntity(abstractSwollenWaterNut);
                    OnlinePhysicalObject.map.TryGetValue(abstractSwollenWaterNut, out var onlineSwollenWaterNut);

                    abstractSwollenWaterNut.RealizeInRoom();

                    SwollenWaterNut swollenWaterNut = abstractSwollenWaterNut.realizedObject as SwollenWaterNut;
                    //self.room.AddObject(swollenWaterNut);
                    swollenWaterNut.firstChunk.HardSetPosition(self.firstChunk.pos);
                    swollenWaterNut.AbstrConsumable.isFresh = abstractSwollenWaterNut.isFresh;
                    onlineSwollenWaterNut.realized = true;
                    self.Destroy();
                }
            }

            private void Player_GetInitialSlugcatClass(On.Player.orig_GetInitialSlugcatClass orig, Player self)
            {
                orig(self);
                if (isStoryMode(out var storyGameMode))
                {
                    self.SlugCatClass = (storyGameMode.clientSettings as StoryClientSettings).playingAs;
                }
            }

            private RWCustom.IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
            {
                if (isStoryMode(out var storyGameMode))
                {
                    return orig(storyGameMode.currentCampaign);
                }
                return orig(slugcat);
            }

        private void HUD_InitSinglePlayerHud(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);
            if (isStoryMode(out var gameMode))
            {
                self.AddPart(new OnlineHUD(self, cam, gameMode));
                self.AddPart(new SpectatorHud(self, cam, gameMode));

            }
        }
        private void RainWorldGame_GhostShutDown(On.RainWorldGame.orig_GhostShutDown orig, RainWorldGame self, GhostWorldPresence.GhostID ghostID)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    OnlineManager.lobby.owner.InvokeRPC(RPCs.MovePlayersToGhostScreen, ghostID.value);
                }
                else
                {
                    RPCs.MovePlayersToGhostScreen(ghostID.value);
                }
            }
            else
            {
                orig(self, ghostID);
            }
        }
        private void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    RPCs.MovePlayersToDeathScreen();
                }
            }
            else
            {
                orig(self);
            }
        }

        private void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                string denPos = null;
                if (OnlineManager.lobby.playerAvatars.TryGetValue(OnlineManager.mePlayer, out var playerAvatar))
                {
                    if (playerAvatar.type != (byte)OnlineEntity.EntityId.IdType.none
                        && (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac))
                    {
                        denPos = self.world.GetAbstractRoom(ac.pos).name;
                    }
                }
                RainMeadow.Debug($"({malnourished}, {denPos})");
                if (OnlineManager.lobby.isOwner)
                {
                    RPCs.MovePlayersToWinScreen(malnourished, denPos);
                }
                else if (!OnlineManager.lobby.owner.OutgoingEvents.Any(e => e is RPCEvent rpc
                    && rpc.IsIdentical(RPCs.MovePlayersToWinScreen, malnourished, denPos)))
                {
                    OnlineManager.lobby.owner.InvokeRPC(RPCs.MovePlayersToWinScreen, malnourished, denPos);
                }
            }
            else
            {
                orig(self, malnourished);
            }
        }

        private void RainWorldGame_GameOver(On.RainWorldGame.orig_GameOver orig, RainWorldGame self, Creature.Grasp dependentOnGrasp)
        {
            if (isStoryMode(out var gameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    RPCs.InitGameOver();
                    return;
                }
                //Initiate death only when all players are dead
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                    {
                        if (ac.state.alive) return;
                    }
                }
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeRPC(RPCs.InitGameOver);
                    }
                    else
                    {
                        orig(self, dependentOnGrasp);
                    }
                }
            }
            else
            {
                orig(self, dependentOnGrasp);
            }
        }

        private SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            var origSaveState = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            if (isStoryMode(out var gameMode))
            {
                var storyClientSettings = gameMode.clientSettings as StoryClientSettings;

                if (OnlineManager.lobby.isOwner)
                {
                    gameMode.defaultDenPos = origSaveState.denPosition;
                }
                else if (storyClientSettings.myLastDenPos != null)
                {
                    origSaveState.denPosition = storyClientSettings.myLastDenPos;
                }
                else if (gameMode.defaultDenPos != null)
                {
                    origSaveState.denPosition = gameMode.defaultDenPos;
                }
            }
            return origSaveState;
        }

        private bool PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (OnlineManager.lobby != null && !OnlineManager.lobby.isOwner) return false;
            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        private void SaveState_SessionEnded(ILContext il)
        {
            // food += (game.session.Players[i].realizedCreature as Player).FoodInRoom(eatAndDestroy: true);
            //becomes
            // food += (game.session.Players[i].realizedCreature as Player)?.FoodInRoom(eatAndDestroy: true);
            try
            {
                var c = new ILCursor(il);
                var vanilla = il.DefineLabel();
                var isinstplayer = il.DefineLabel();
                var postadd = il.DefineLabel();
                var postdup = il.DefineLabel();
                var postpop = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdsfld<ModManager>("CoopAvailable"),
                    i => i.MatchBrfalse(out vanilla)
                    );
                c.GotoLabel(vanilla);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchIsinst<Player>()
                    );
                c.MarkLabel(isinstplayer);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchCallOrCallvirt<Player>("FoodInRoom"),
                    i => i.MatchAdd()
                    );
                c.MarkLabel(postadd);
                c.GotoLabel(isinstplayer);
                c.Emit(OpCodes.Dup);
                c.MarkLabel(postdup);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, postadd);
                c.MarkLabel(postpop);
                c.GotoLabel(postdup);
                c.Emit(OpCodes.Brtrue, postpop);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void SaveState_BringUpToDate(On.SaveState.orig_BringUpToDate orig, SaveState self, RainWorldGame game)
        {
            if (isStoryMode(out var gameMode))
            {
                var denPos = self.denPosition;
                orig(self, game);
                self.denPosition = denPos;
            }
            else
            {
                orig(self, game);
            }
        }

            private void KarmaLadderScreen_Singal(On.Menu.KarmaLadderScreen.orig_Singal orig, Menu.KarmaLadderScreen self, Menu.MenuObject sender, string message)
            {
                if (isStoryMode(out var gameMode))
                {
                    if (message == "CONTINUE")
                    {
                        if (OnlineManager.lobby.isOwner)
                        {
                            RainMeadow.Debug("Continue - host");
                            gameMode.didStartCycle = true;
                        }
                        else if (!gameMode.didStartCycle)
                        {
                            sender.toggled = !sender.toggled;
                            isPlayerReady = sender.toggled;
                            RainMeadow.Debug(sender.toggled ? "Ready!" : "Cancelled!");
                            return;
                        }
                        RainMeadow.Debug("Continue - client");
                    }
                }
                orig(self, sender, message);
            }

            private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
            {
                orig(self, eu);
                if (isStoryMode(out var gameMode))
                {

                    //fetch the online entity and check if it is mine. 
                    //If it is mine run the below code
                    //If not, update from the lobby state
                    //self.readyForWin = OnlineMAnager.lobby.playerid === fetch if this is ours. 

                    if (OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var oe))
                    {
                        if (!oe.isMine)
                        {
                            self.readyForWin = gameMode.readyForWinPlayers.Contains(oe.owner.inLobbyId);
                            return;
                        }
                    }

                    if (self.readyForWin
                        && self.touchedNoInputCounter > (ModManager.MMF ? 40 : 20)
                        && RWCustom.Custom.ManhattanDistance(self.abstractCreature.pos.Tile, self.room.shortcuts[0].StartTile) > 3)
                    {
                        gameMode.storyClientSettings.readyForWin = true;
                    }
                    else
                    {
                        gameMode.storyClientSettings.readyForWin = false;
                    }
                }
            }

            private void SleepAndDeathScreen_Update(On.Menu.SleepAndDeathScreen.orig_Update orig, Menu.SleepAndDeathScreen self)
            {
                orig(self);

                if (isStoryMode(out var gameMode))
                {
                    if (OnlineManager.lobby.isOwner)
                    {
                        self.continueButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
                    }
                    else
                    {
                        if (gameMode.didStartCycle)
                        {
                            if (isPlayerReady)
                            {
                                self.Singal(self.continueButton, "CONTINUE");
                            }
                            else if (self.continueButton != null)
                            {
                                self.continueButton.menuLabel.text = self.Translate("CONTINUE");
                            }
                        }
                        else if (self.continueButton != null)
                        {
                            self.continueButton.menuLabel.text = self.Translate("READY");
                        }
                    }
                }
            }

            private void SleepAndDeathScreen_ctor(On.Menu.SleepAndDeathScreen.orig_ctor orig, Menu.SleepAndDeathScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
            {
                RainMeadow.Debug("In SleepAndDeath Screen");
                orig(self, manager, ID);

                if (isStoryMode(out var gameMode))
                {
                    isPlayerReady = false;
                    gameMode.didStartCycle = false;
                }
            }

            private bool RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
            {

                if (isStoryMode(out var storyGameMode))
                {
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                        {
                            if (ac.pos.room == self.room.abstractRoom.index && (!self.letThroughDir || ac.pos.x < self.room.TileWidth / 2 + 3)
                                && (self.letThroughDir || ac.pos.x > self.room.TileWidth / 2 - 4))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false; // not loaded
                        }

                    }

                self.room.game.cameras[0].hud.parts.Add(new OnlineHUD(self.room.game.cameras[0].hud, self.room.game.cameras[0], storyGameMode));
                self.room.game.cameras[0].hud.parts.Add(new SpectatorHud(self.room.game.cameras[0].hud, self.room.game.cameras[0], storyGameMode));

                    return true;
                }
                return orig(self);

            }


            private int RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
            {
                if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
                {
                    int regionGateZone = -1;
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                        {
                            if (ac.Room == self.room.abstractRoom)
                            {
                                int zone = self.DetectZone(ac);
                                if (zone != regionGateZone && regionGateZone != -1)
                                {
                                    return -1;
                                }
                                regionGateZone = zone;
                            }
                        }
                        else
                        {
                            return -1; // not loaded
                        }
                    }

                    return regionGateZone;
                }
                return orig(self);
            }

            private bool PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
            {
                if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
                {
                    foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Values)
                    {
                        if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                        if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                        {
                            if (ac.Room != self.room.abstractRoom
                            || ((ac.realizedCreature as Player)?.touchedNoInputCounter ?? 0) < (ModManager.MMF ? 40 : 20))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false; // not loaded
                        }
                    }

                    List<HudPart> partsToRemove = new List<HudPart>();

                    foreach (HudPart part in self.room.game.cameras[0].hud.parts)
                    {
                        if (part is OnlineHUD || part is PlayerSpecificOnlineHud)
                        {

                            partsToRemove.Add(part);
                        }
                    }

                foreach (HudPart part in partsToRemove)
                {
                    part.slatedForDeletion = true;
                    part.ClearSprites();
                    self.room.game.cameras[0].hud.parts.Remove(part);
                }
                return true;
            }
            return orig(self);
        }
    }
}