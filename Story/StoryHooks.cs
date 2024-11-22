using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

            On.Weapon.HitThisObject += Weapon_HitThisObject;
            On.Menu.SlugcatSelectMenu.CustomColorInterface.ctor += CustomColorInterface_ctor;
            On.Menu.SlugcatSelectMenu.SliderSetValue += SlugcatSelectMenu_SliderSetValue;
            On.Menu.SlugcatSelectMenu.SetChecked += SlugcatSelectMenu_SetChecked;
            On.Menu.SlugcatSelectMenu.GetChecked += SlugcatSelectMenu_GetChecked;
        }


        private bool SlugcatSelectMenu_GetChecked(On.Menu.SlugcatSelectMenu.orig_GetChecked orig, Menu.SlugcatSelectMenu self, Menu.CheckBox box)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (box.IDString == "COLORS")
                {
                    return self.colorChecked;
                }

                if (box.IDString == "RESTART")
                {
                    return self.restartChecked;
                }

                if (box.IDString == "CLIENTSAVERESET")
                {
                    return storyGameMode.saveToDisk;
                }


                if (box.IDString == "ONLINEFRIENDLYFIRE")
                {
                    return storyGameMode.friendlyFire;
                }

                return false;
            }
            else
            {
                return orig(self, box);
            }
        }

        private void SlugcatSelectMenu_SetChecked(On.Menu.SlugcatSelectMenu.orig_SetChecked orig, Menu.SlugcatSelectMenu self, Menu.CheckBox box, bool c)
        {
            if (isStoryMode(out var storyGameMode) && self is StoryOnlineMenu storyMenu)
            {

                if (box.IDString == "COLORS")
                {
                    self.colorChecked = c;
                    if (self.colorChecked && !self.CheckJollyCoopAvailable(self.colorFromIndex(self.slugcatPageIndex)))
                    {
                        self.AddColorButtons();
                        self.manager.rainWorld.progression.miscProgressionData.colorsEnabled[self.slugcatColorOrder[self.slugcatPageIndex].value] = true;
                    }
                    else
                    {
                        self.RemoveColorButtons();
                        self.manager.rainWorld.progression.miscProgressionData.colorsEnabled[self.slugcatColorOrder[self.slugcatPageIndex].value] = false;
                    }
                }

                if (box.IDString == "RESTART")
                {
                    self.restartChecked = c;
                    self.UpdateStartButtonText();

                }
                if (box.IDString == "CLIENTSAVERESET")
                {
                    storyGameMode.saveToDisk = c;
                }

                if (box.IDString == "ONLINEFRIENDLYFIRE") // online dictionaries do not like updating over the wire and I dont have the energy to deal with that right now
                {
                    storyGameMode.friendlyFire = c;

                }
            }
            else
            {
                orig(self, box, c);
            }
        }

        private void SlugcatSelectMenu_SliderSetValue(On.Menu.SlugcatSelectMenu.orig_SliderSetValue orig, Menu.SlugcatSelectMenu self, Menu.Slider slider, float f)
        {
            orig(self, slider, f);
            if (isStoryMode(out var story))
            {

                self.colorInterface.bodyColors[self.activeColorChooser].color = RWCustom.Custom.HSL2RGB(self.hueSlider.floatValue, self.satSlider.floatValue, self.litSlider.floatValue);
                RainMeadow.Debug(self.colorInterface.bodyColors[self.activeColorChooser].color);
                story.avatarSettings.bodyColor = self.colorInterface.bodyColors[0].color;
                RainMeadow.rainMeadowOptions.BodyColor.Value = self.colorInterface.bodyColors[0].color;

                story.avatarSettings.eyeColor = self.colorInterface.bodyColors[1].color;
                RainMeadow.rainMeadowOptions.EyeColor.Value = self.colorInterface.bodyColors[1].color;


            }
        }

        private void CustomColorInterface_ctor(On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_ctor orig, Menu.SlugcatSelectMenu.CustomColorInterface self, Menu.Menu menu, Menu.MenuObject owner, Vector2 pos, SlugcatStats.Name slugcatID, List<string> names, List<string> defaultColors)
        {
            orig(self, menu, owner, pos, slugcatID, names, defaultColors);
            if (isStoryMode(out var _))
            {
                self.bodyColors[0].color = RainMeadow.rainMeadowOptions.BodyColor.Value;
                self.bodyColors[1].color = RainMeadow.rainMeadowOptions.EyeColor.Value;
            }


        }

        private bool Weapon_HitThisObject(On.Weapon.orig_HitThisObject orig, Weapon self, PhysicalObject obj)
        {
            if (isStoryMode(out var story) && story.friendlyFire && obj is Player && self is Spear && self.thrownBy != null && self.thrownBy is Player)
            {
                return true;
            }
            return orig(self, obj);
        }

        private void TextPrompt_UpdateGameOverString(On.HUD.TextPrompt.orig_UpdateGameOverString orig, TextPrompt self, Options.ControlSetup.Preset controllerType)
        {
            if (isStoryMode(out _))
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
            if (OnlineManager.lobby != null && self.currentlyShowing == TextPrompt.InfoID.GameOver)
            {
                if (ChatHud.chatButtonActive)
                {
                    self.restartNotAllowed = 1; // block GoToDeathScreen if we're typing
                    return;
                }
                if (isStoryMode(out _) && !OnlineManager.lobby.isOwner)
                {
                    self.restartNotAllowed = 1; // block clients from GoToDeathScreen

                    // let clients still have access to pause menu
                    bool touchedInput = false;
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
                self.AddPart(new SpectatorHud(self, cam));
                self.AddPart(new Pointing(self));
                self.AddPart(new ChatHud(self, cam));
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
            if (isStoryMode(out var storyGameMode))
            {
                OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.MovePlayersToWinScreen, malnourished, storyGameMode.myLastDenPos);
            }
            else
            {
                orig(self, malnourished);
            }
        }

        private SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            if (isStoryMode(out var storyGameMode))
            {
                var origLoadInProgress = self.loadInProgress;
                if (!OnlineManager.lobby.isOwner && self.starvedSaveState is null) self.loadInProgress = true;  // don't load client save
                var currentSaveState = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
                self.loadInProgress = origLoadInProgress;

                if (!OnlineManager.lobby.isOwner)
                {
                    currentSaveState.deathPersistentSaveData.ghostsTalkedTo.Clear();
                    foreach (var kvp in storyGameMode.ghostsTalkedTo)
                        if (ExtEnumBase.TryParse(typeof(GhostWorldPresence.GhostID), kvp.Key, ignoreCase: false, out var rawEnumBase))
                            currentSaveState.deathPersistentSaveData.ghostsTalkedTo[(GhostWorldPresence.GhostID)rawEnumBase] = kvp.Value;
                }

                if (storyGameMode.myLastDenPos != null)
                {
                    currentSaveState.denPosition = storyGameMode.myLastDenPos;
                }
                else if (storyGameMode.defaultDenPos != null)
                {
                    storyGameMode.myLastDenPos = currentSaveState.denPosition = storyGameMode.defaultDenPos;
                }
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.defaultDenPos = currentSaveState.denPosition;
                }

                return currentSaveState;
            }

            return orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
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

        private void KarmaLadderScreen_Update(On.Menu.KarmaLadderScreen.orig_Update orig, Menu.KarmaLadderScreen self)
        {
            orig(self);

            if (isStoryMode(out var gameMode) && self.continueButton != null)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    self.continueButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
                }
                else if (gameMode.didStartCycle)
                {
                    if (isPlayerReady)
                    {
                        self.Singal(self.continueButton, "CONTINUE");
                    }
                    else
                    {
                        self.continueButton.menuLabel.text = self.Translate("CONTINUE");
                    }
                }
                else
                {
                    self.continueButton.menuLabel.text = self.Translate("READY");
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
            }
        }

        private bool RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
        {

            if (isStoryMode(out var storyGameMode))
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
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
                self.room.game.cameras[0].hud.parts.Add(new SpectatorHud(self.room.game.cameras[0].hud, self.room.game.cameras[0]));
                self.room.game.cameras[0].hud.parts.Add(new Pointing(self.room.game.cameras[0].hud));
                return true;
            }

            return orig(self);
        }


        private int RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                int regionGateZone = -1;
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
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
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
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
                    if (part is OnlineHUD || part is PlayerSpecificOnlineHud || part is SpectatorHud)
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
