using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using static RainMeadow.MeadowProgression;
using IL;

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
            On.Menu.KarmaLadderScreen.ctor += KarmaLadderScreen_ctor;
            On.Menu.KarmaLadderScreen.Update += KarmaLadderScreen_Update;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;

            On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;

            IL.Player.Update += Player_Update1;
            On.Player.Update += Player_Update;

            On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass;
            On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;

            IL.HardmodeStart.ctor += HardmodeStart_ctor;
            new Hook(typeof(HardmodeStart.HardmodePlayer).GetProperty("MainPlayer").GetGetMethod(), this.HardmodeStart_HardmodePlayer_MainPlayer);
            IL.HardmodeStart.SinglePlayerUpdate += HardmodeStart_SinglePlayerUpdate;

            IL.MoreSlugcats.MSCRoomSpecificScript.DS_RIVSTARTcutscene.ctor += ClientDisableUAD;
            IL.MoreSlugcats.CutsceneArtificer.ctor += ClientDisableUAD;
            IL.MoreSlugcats.CutsceneArtificerRobo.ctor += ClientDisableUAD;
            IL.MoreSlugcats.MSCRoomSpecificScript.SI_SAINTINTRO_tut.ctor += ClientDisableUAD;

            On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide;
            On.RegionGate.PlayersStandingStill += PlayersStandingStill;
            On.RegionGate.PlayersInZone += RegionGate_PlayersInZone;

            On.GhostHunch.Update += GhostHunch_Update;

            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.RainWorldGame.Win += RainWorldGame_Win;

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

            IL.Weapon.HitThisObject += Weapon_HitThisObject;
        }

        // bool flag2 = ModManager.CoopAvailable && Custom.rainWorld.options.friendlyFire;
        // becomes
        // bool flag2 = ModManager.CoopAvailable && Custom.rainWorld.options.friendlyFire || isStoryMode && rainmeadowOptions.FriendlyFire.Value;
        private void Weapon_HitThisObject(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                var jumpToDelegate = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdsfld("ModManager", "CoopAvailable"),
                    i => i.MatchBrfalse(out _),
                    i => i.MatchLdsfld("RWCustom.Custom", "rainWorld"),
                    i => i.MatchLdfld<RainWorld>("options"),
                    i => i.MatchLdfld<Options>("friendlyFire"),
                    i => i.MatchBr(out _)
                    );
                // Emit check for isStoryMode
                c.EmitDelegate(() => isStoryMode(out var _));
                c.Emit(OpCodes.Brtrue, jumpToDelegate); // If isStoryMode is true, branch to the jump label

                // If both conditions are false, push false
                c.Emit(OpCodes.Ldc_I4_0); // Push false
                c.Emit(OpCodes.Br, skip);   // Skip to the end

                // Label for when isStoryMode is true
                c.MarkLabel(jumpToDelegate);
                c.Emit(OpCodes.Ldc_I4_1); // Push true

                c.MarkLabel(skip);
            }


            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void TextPrompt_UpdateGameOverString(On.HUD.TextPrompt.orig_UpdateGameOverString orig, TextPrompt self, Options.ControlSetup.Preset controllerType)
        {
            if (isStoryMode(out var _))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    self.gameOverString = $"Wait for others to shelter or rescue you, press {RainMeadow.rainMeadowOptions.SpectatorKey.Value} to spectate, or press PAUSE BUTTON to restart";
                }
                else
                {
                    self.gameOverString = $"Wait for others to shelter or rescue you, press {RainMeadow.rainMeadowOptions.SpectatorKey.Value} to spectate, or press PAUSE BUTTON to dismiss message";
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
            if (isStoryMode(out var _))
            {
                if (!OnlineManager.lobby.isOwner && self.currentlyShowing == TextPrompt.InfoID.GameOver)
                {
                    self.restartNotAllowed = 1; // block clients from GoToDeathScreen
                    bool touchedInput = false;

                    // let clients still have access to pause menu
                    for (int j = 0; j < self.hud.rainWorld.options.controls.Length; j++)
                    {
                        touchedInput = (self.hud.rainWorld.options.controls[j].gamePad || !self.defaultMapControls[j]) ? (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(5) || RWInput.CheckPauseButton(0, inMenu: false)) : (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(11));
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
                OnlineManager.lobby.owner.InvokeOnceRPC(ConsumableRPCs.enableTheGlow);
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
                            opo.owner.InvokeOnceRPC(ConsumableRPCs.explodePuffBall, onlineRoom, self.bodyChunks[0].pos, self.sporeColor, self.color);
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
                if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe))
                    throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
                var scs = OnlineManager.lobby.activeEntities.OfType<StoryClientSettings>().FirstOrDefault(e => e.owner == oe.owner);
                if (scs == null) throw new InvalidProgrammerException("OnlinePlayer doesn't have StoryClientSettings!!");
                self.SlugCatClass = scs.playingAs;
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

        private void HardmodeStart_ctor(ILContext il)
        {
            // don't spawn things if not host
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchStfld<RoomCamera>("followAbstractCreature")
                    );
                c.EmitDelegate(() => OnlineManager.lobby != null && !OnlineManager.lobby.isOwner);
                c.Emit(OpCodes.Brtrue, skip);
                c.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(1),
                    i => i.MatchLdfld<Room>("game"),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_Players")
                    );
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private bool HardmodeStart_HardmodePlayer_MainPlayer(Func<HardmodeStart.HardmodePlayer, bool> orig, HardmodeStart.HardmodePlayer self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                return OnlineManager.lobby.isOwner;
            }
            return orig(self);
        }

        private void HardmodeStart_SinglePlayerUpdate(ILContext il)
        {
            // don't spawn stomach pearl
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdloc(0),
                    i => i.MatchBrfalse(out _),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<HardmodeStart>("playerPosCorrect"),
                    i => i.MatchBrtrue(out _)
                    );
                c.GotoNext(
                    i => i.MatchLdloc(0),
                    i => i.MatchCallOrCallvirt<Player>("get_playerState"),
                    i => i.MatchLdcI4(out _),
                    i => i.MatchStfld<PlayerState>("foodInStomach")
                    );
                c.EmitDelegate(() => OnlineManager.lobby != null && !OnlineManager.lobby.isOwner);
                c.Emit(OpCodes.Brtrue, skip);
                c.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<HardmodeStart>("camPosCorrect"),
                    i => i.MatchBrfalse(out _)
                    );
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void ClientDisableUAD(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.TryGotoNext(moveType: MoveType.After,
                        i => i.MatchStfld<UpdatableAndDeletable>("room")
                        );
                c.EmitDelegate(() => OnlineManager.lobby != null && !OnlineManager.lobby.isOwner);
                c.Emit(OpCodes.Brfalse, skip);
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((UpdatableAndDeletable self) => self.Destroy());
                c.Emit(OpCodes.Ret);
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
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
            if (isStoryMode(out var gameMode))
            {
                OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.MovePlayersToGhostScreen, ghostID.value);
            }
            else
            {
                orig(self, ghostID);
            }
        }

        private void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
        {
            if (isStoryMode(out var gameMode))
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
            if (isStoryMode(out var gameMode))
            {
                OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.MovePlayersToWinScreen, malnourished, gameMode.storyClientSettings.myLastDenPos);
            }
            else
            {
                orig(self, malnourished);
            }
        }

        private SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            var origSaveState = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            if (isStoryMode(out var gameMode))
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    origSaveState.deathPersistentSaveData.ghostsTalkedTo.Clear();
                    foreach (var kvp in gameMode.ghostsTalkedTo)
                        if (ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), kvp.Key, ignoreCase: false, out var rawEnumBase))
                            origSaveState.deathPersistentSaveData.ghostsTalkedTo[(GhostWorldPresence.GhostID)rawEnumBase] = kvp.Value;
                }

                var storyClientSettings = gameMode.clientSettings as StoryClientSettings;

                if (storyClientSettings.myLastDenPos != null)
                {
                    origSaveState.denPosition = storyClientSettings.myLastDenPos;
                }
                else if (gameMode.defaultDenPos != null)
                {
                    storyClientSettings.myLastDenPos = origSaveState.denPosition = gameMode.defaultDenPos;
                }
                if (OnlineManager.lobby.isOwner)
                {
                    gameMode.defaultDenPos = origSaveState.denPosition;
                }

                storyClientSettings.hasSheltered = false;
                gameMode.changedRegions = false;
            }
            return origSaveState;
        }

        private bool PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (isStoryMode(out var storyGameMode) && !storyGameMode.saveToDisk) return false;
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
                var cont = il.DefineLabel();
                var skip = il.DefineLabel();
                c.GotoNext(
                    i => i.MatchLdsfld<ModManager>("CoopAvailable"),
                    i => i.MatchBrfalse(out vanilla)
                    );
                c.GotoLabel(vanilla);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchIsinst<Player>()
                    );
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Brtrue, cont);
                c.Emit(OpCodes.Pop);
                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(cont);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchCallOrCallvirt<Player>("FoodInRoom"),
                    i => i.MatchAdd()
                    );
                c.MarkLabel(skip);
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

        private void Player_Update1(ILContext il)
        {
            try
            {
                // don't call GameOver if player is not ours
                var c = new ILCursor(il);
                ILLabel skip = il.DefineLabel();
                c.GotoNext(
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("game"),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<Player>("dangerGrasp"),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("GameOver")
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Player self) =>
                    (OnlineManager.lobby != null && !(OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe) && oe.isMine)));
                c.Emit(OpCodes.Brtrue, skip);
                c.Index += 6;
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Player_Update(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (isStoryMode(out var gameMode))
            {

                //fetch the online entity and check if it is mine.
                //If it is mine run the below code
                //If not, update from the lobby state
                //self.readyForWin = OnlineManager.lobby.playerid === fetch if this is ours.

                if (OnlinePhysicalObject.map.TryGetValue(self.abstractCreature, out var oe))
                {
                    if (!oe.isMine)
                    {
                        self.readyForWin = gameMode.readyForWinPlayers.Contains(oe.owner.inLobbyId);
                        return;
                    }
                }

                gameMode.storyClientSettings.isDead = self.dead;

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

        private void KarmaLadderScreen_Update(On.Menu.KarmaLadderScreen.orig_Update orig, Menu.KarmaLadderScreen self)
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

        private void KarmaLadderScreen_ctor(On.Menu.KarmaLadderScreen.orig_ctor orig, Menu.KarmaLadderScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            RainMeadow.Debug("In KarmaLadder Screen");
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

        private void GhostHunch_Update(On.GhostHunch.orig_Update orig, GhostHunch self, bool eu)
        {
            orig(self, eu);
            if (isStoryMode(out _) && !OnlineManager.lobby.isOwner)
            {
                if (self.ghostNumber != null && (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.ghostsTalkedTo[self.ghostNumber] > 0)
                    OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.TriggerGhostHunch, self.ghostNumber.value);
            }
        }
    }
}
