using HUD;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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

            IL.Menu.DreamScreen.Update += DreamScreen_Update_DisableArtiFlashbacks;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.Update += MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_Update;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.TriggerBossFight += MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_TriggerBossFight;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.TriggerFadeToEnding += MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_TriggerFadeToEnding;
            On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.SummonScavengers += MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_SummonScavengers;

            IL.RegionGate.Update += RegionGate_Update;
            On.RegionGate.PlayersInZone += RegionGate_PlayersInZone;
            On.RegionGate.PlayersStandingStill += RegionGate_PlayersStandingStill;
            On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide;

            On.GhostHunch.Update += GhostHunch_Update;

            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GoToStarveScreen += RainWorldGame_GoToStarveScreen;
            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;

            IL.SaveState.SessionEnded += SaveState_SessionEnded;

            On.WaterNut.Swell += WaterNut_Swell;
            On.SporePlant.Pacify += SporePlant_Pacify;

            On.Oracle.CreateMarble += Oracle_CreateMarble;
            On.Oracle.SetUpMarbles += Oracle_SetUpMarbles;
            On.Oracle.SetUpSwarmers += Oracle_SetUpSwarmers;
            On.OracleSwarmer.BitByPlayer += OracleSwarmer_BitByPlayer;
            On.SLOracleSwarmer.BitByPlayer += SLOracleSwarmer_BitByPlayer;
            On.CoralBrain.CoralNeuronSystem.PlaceSwarmers += OnCoralNeuronSystem_PlaceSwarmers;
            On.SSOracleSwarmer.NewRoom += SSOracleSwarmer_NewRoom;

            On.Oracle.ctor += Oracle_ctor;
            IL.Room.ReadyForAI += Room_ReadyForAI;
            On.SLOracleWakeUpProcedure.SwarmerEnterRoom += SLOracleWakeUpProcedure_SwarmerEnterRoom;
            IL.SLOracleWakeUpProcedure.Update += SLOracleWakeUpProcedure_Update;
            IL.SLOracleBehavior.Update += SLOracleBehavior_Update;
            On.SLOracleBehavior.Update += SLOracleBehavior_Update1;
            On.SLOracleBehaviorHasMark.Update += SLOracleBehaviorHasMark_Update;
            On.SLOracleBehaviorHasMark.PlayerPutItemOnGround += SLOracleBehaviorHasMark_PlayerPutItemOnGround;

            On.HUD.TextPrompt.Update += TextPrompt_Update;
            On.HUD.TextPrompt.UpdateGameOverString += TextPrompt_UpdateGameOverString;

            On.Weapon.HitThisObject += Weapon_HitThisObject;
            On.Menu.SlugcatSelectMenu.CustomColorInterface.ctor += CustomColorInterface_ctor;
            On.Menu.SlugcatSelectMenu.SliderSetValue += SlugcatSelectMenu_SliderSetValue;
            On.Menu.SlugcatSelectMenu.SetChecked += SlugcatSelectMenu_SetChecked;
            On.Menu.SlugcatSelectMenu.GetChecked += SlugcatSelectMenu_GetChecked;
            On.Menu.PauseMenu.SpawnExitContinueButtons += PauseMenu_SpawnExitContinueButtons;

            On.VoidSpawnKeeper.AddOneSpawn += VoidSpawnKeeper_AddOneSpawn;
        }


        private void VoidSpawnKeeper_AddOneSpawn(On.VoidSpawnKeeper.orig_AddOneSpawn orig, VoidSpawnKeeper self)
        {
            if (isStoryMode(out var _) && !OnlineManager.lobby.isOwner)
            {
                return;
            }
            else
            {
                orig(self);
            }
        }
        private void PauseMenu_SpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, Menu.PauseMenu self)
        {
            orig(self);
            if (isStoryMode(out var story))
            {
                var restartButton = new SimplerButton(self, self.pages[0], self.Translate("RESTART"), new Vector2(self.exitButton.pos.x - (self.continueButton.pos.x - self.exitButton.pos.x) - self.moveLeft - self.manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)), new Vector2(110f, 30f));
                restartButton.OnClick += (_) =>
                {
                    (self.game.cameras[0].hud.rainWorld.processManager.currentMainLoop as RainWorldGame).GoToDeathScreen();
                };
                self.pages[0].subObjects.Add(restartButton);
            }
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
                if (isStoryMode(out _))
                {
                    self.restartNotAllowed = 1; // block from GoToDeathScreen

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
            if (!self.IsLocal()) return;
            orig(self);
        }

        private void Room_ReadyForAI(ILContext il)
        {
            try
            {
                // don't spawn oracles if room already contains oracle
                var c = new ILCursor(il);
                ILLabel skip = null;
                c.GotoNext(MoveType.After,
                    i => i.MatchLdloc(0),
                    i => i.MatchOr(),
                    i => i.MatchBrfalse(out skip)
                );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Room room) =>
                {
                    if (OnlineManager.lobby == null) return false;
                    RainMeadow.Debug($"existing oracle in room? {room.abstractRoom.GetResource().activeEntities.FirstOrDefault(x => x is OnlinePhysicalObject opo && opo.apo.type == AbstractPhysicalObject.AbstractObjectType.Oracle)}");
                    RainMeadow.Debug($"existing oracle in world? {room.world.GetResource().activeEntities.FirstOrDefault(x => x is OnlinePhysicalObject opo && opo.apo.type == AbstractPhysicalObject.AbstractObjectType.Oracle)}");
                    return room.world.GetResource().activeEntities.Any(x => x is OnlinePhysicalObject opo && opo.apo.type == AbstractPhysicalObject.AbstractObjectType.Oracle);
                });
                //c.EmitDelegate((Room room) => OnlineManager.lobby != null && room.abstractRoom.GetResource().activeEntities.Any(x => x is OnlinePhysicalObject opo && opo.apo.type == AbstractPhysicalObject.AbstractObjectType.Oracle));
                c.Emit(OpCodes.Brtrue, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void SLOracleWakeUpProcedure_SwarmerEnterRoom(On.SLOracleWakeUpProcedure.orig_SwarmerEnterRoom orig, SLOracleWakeUpProcedure self, IntVector2 tilePos)
        {
            if (!self.SLOracle.IsLocal()) return;
            orig(self, tilePos);
        }

        private void SLOracleWakeUpProcedure_Update(ILContext il)
        {
            try
            {
                // remote don't spawn SLOracleSwarmer during GoToOracle phase
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<SLOracleWakeUpProcedure>("phase"),
                    i => i.MatchLdsfld<SLOracleWakeUpProcedure.Phase>("GoToOracle"),
                    i => i.MatchCall("ExtEnum`1<SLOracleWakeUpProcedure/Phase>", "op_Equality"),
                    i => i.MatchBrfalse(out _)
                );
                c.GotoNext(MoveType.AfterLabel,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("world"),
                    i => i.MatchLdsfld<AbstractPhysicalObject.AbstractObjectType>("SLOracleSwarmer"),
                    i => i.MatchLdnull(),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<SLOracleWakeUpProcedure>("resqueSwarmer"),
                    i => i.MatchCallvirt<PhysicalObject>("get_firstChunk"),
                    i => i.MatchLdfld<BodyChunk>("pos"),
                    i => i.MatchCallvirt<Room>("GetWorldCoordinate"),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("game"),
                    i => i.MatchCallvirt<RainWorldGame>("GetNewID"),
                    i => i.MatchNewobj<AbstractPhysicalObject>(),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("world"),
                    i => i.MatchNewobj<SLOracleSwarmer>(),
                    i => i.MatchStloc(4)
                );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((SLOracleWakeUpProcedure wakeUpProcedure) => wakeUpProcedure.SLOracle?.IsLocal() ?? true);
                c.Emit(OpCodes.Brfalse, skip);
                c.GotoNext(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<SLOracleWakeUpProcedure>("SLOracle"),
                    i => i.MatchLdfld<Oracle>("mySwarmers"),
                    i => i.MatchLdloc(4),
                    i => i.MatchCallvirt("System.Collections.Generic.List`1<OracleSwarmer>", "Add")
                );
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void SLOracleBehavior_Update(ILContext il)
        {
            try
            {
                // remote don't spawn converted SSSwarmer
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(MoveType.AfterLabel,
                    // holdingObject = null;
                    i => i.MatchLdarg(0),
                    i => i.MatchLdnull(),
                    i => i.MatchStfld<SLOracleBehavior>("holdingObject"),
                    // SLOracleSwarmer sLOracleSwarmer = new SLOracleSwarmer(new AbstractPhysicalObject(oracle.room.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null, oracle.room.GetWorldCoordinate(pos), oracle.room.game.GetNewID()), oracle.room.world);
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<OracleBehavior>("oracle"),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("world"),
                    i => i.MatchLdsfld<AbstractPhysicalObject.AbstractObjectType>("SLOracleSwarmer"),
                    i => i.MatchLdnull(),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<OracleBehavior>("oracle"),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdloc(4),
                    i => i.MatchCallvirt<Room>("GetWorldCoordinate"),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<OracleBehavior>("oracle"),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("game"),
                    i => i.MatchCallvirt<RainWorldGame>("GetNewID"),
                    i => i.MatchNewobj<AbstractPhysicalObject>(),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<OracleBehavior>("oracle"),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchLdfld<Room>("world"),
                    i => i.MatchNewobj<SLOracleSwarmer>(),
                    i => i.MatchStloc(5)
                );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((SLOracleBehavior behavior) => behavior.oracle?.IsLocal() ?? true);
                c.Emit(OpCodes.Brfalse, skip);
                c.GotoNext(MoveType.AfterLabel,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallvirt<SLOracleBehavior>("ConvertingSSSwarmer")
                );
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // don't drop prematurely
        private void SLOracleBehavior_Update1(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            var holdingObject = self.holdingObject;
            orig(self, eu);
            if (!self.oracle.IsLocal()) self.holdingObject = holdingObject;
        }

        // don't drop prematurely
        private void SLOracleBehaviorHasMark_Update(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            var holdingObject = self.holdingObject;
            orig(self, eu);
            if (!self.oracle.IsLocal()) self.holdingObject = holdingObject;
        }

        // consider syncing via RPC
        private void SLOracleBehaviorHasMark_PlayerPutItemOnGround(On.SLOracleBehaviorHasMark.orig_PlayerPutItemOnGround orig, SLOracleBehaviorHasMark self)
        {
            orig(self);
            if (OnlineManager.lobby != null && self.oracle.abstractPhysicalObject.GetOnlineObject(out var opo) && !opo.isMine)
            {
                opo.Request();
            }
        }

        private void Oracle_ctor(On.Oracle.orig_ctor orig, Oracle self, AbstractPhysicalObject abstractPhysicalObject, Room room)
        {
            orig(self, abstractPhysicalObject, room);
            if (OnlineManager.lobby != null && abstractPhysicalObject.GetOnlineObject() is null)
            {
                room.abstractRoom.AddEntity(abstractPhysicalObject);
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

        // HACK: arti flashbacks use singleRoomWorlds which we don't handle well, disabling for now
        // ideally this should be offline but that's another can of worms
        private void DreamScreen_Update_DisableArtiFlashbacks(ILContext il)
        {
            try
            {
                // bool flag = ModManager.MSC && dreamID.Index >= MoreSlugcatsEnums.DreamID.ArtificerFamilyA.Index && dreamID.Index <= MoreSlugcatsEnums.DreamID.ArtificerNightmare.Index;
                //becomes
                // bool flag = !isStoryMode(out _) && ModManager.MSC && dreamID.Index >= MoreSlugcatsEnums.DreamID.ArtificerFamilyA.Index && dreamID.Index <= MoreSlugcatsEnums.DreamID.ArtificerNightmare.Index;
                var c = new ILCursor(il);
                c.GotoNext(moveType: MoveType.AfterLabel,
                    i => i.MatchStloc(1)
                    );
                c.EmitDelegate((bool flag) => !isStoryMode(out _) && flag);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_Update(On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.orig_Update orig, MoreSlugcats.MSCRoomSpecificScript.LC_FINAL self, bool eu)
        {
            if (isStoryMode(out _) && !(self.room.abstractRoom.GetResource()?.isOwner ?? true))
            {
                self.king = self.room.updateList.OfType<Scavenger>().FirstOrDefault(scav => scav.King);
            }
            orig(self, eu);
        }

        private void MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_TriggerBossFight(On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.orig_TriggerBossFight orig, MoreSlugcats.MSCRoomSpecificScript.LC_FINAL self)
        {
            if (isStoryMode(out _) && !(self.room.abstractRoom.GetResource()?.isOwner ?? true)) return;
            orig(self);
        }

        private void MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_TriggerFadeToEnding(On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.orig_TriggerFadeToEnding orig, MoreSlugcats.MSCRoomSpecificScript.LC_FINAL self)
        {
            if (isStoryMode(out _))
            {
                foreach (var player in OnlineManager.players)
                {
                    if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.LC_FINAL_TriggerFadeToEnding);
                }
            }

            orig(self);
        }

        private void MoreSlugcats_MSCRoomSpecificScript_LC_FINAL_SummonScavengers(On.MoreSlugcats.MSCRoomSpecificScript.LC_FINAL.orig_SummonScavengers orig, MoreSlugcats.MSCRoomSpecificScript.LC_FINAL self)
        {
            if (isStoryMode(out _) && !(self.room.abstractRoom.GetResource()?.isOwner ?? true)) return;
            orig(self);
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

        private void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToWinScreen, malnourished, storyGameMode.myLastDenPos);
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToWinScreen, malnourished, storyGameMode.myLastDenPos);
                    return;
                }
            }

            orig(self, malnourished);
        }

        private void RainWorldGame_GoToStarveScreen(On.RainWorldGame.orig_GoToStarveScreen orig, RainWorldGame self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToStarveScreen, storyGameMode.myLastDenPos);
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToStarveScreen, storyGameMode.myLastDenPos);
                    return;
                }
            }

            orig(self);
        }

        private void RainWorldGame_GhostShutDown(On.RainWorldGame.orig_GhostShutDown orig, RainWorldGame self, GhostWorldPresence.GhostID ghostID)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToGhostScreen, ghostID);
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToGhostScreen, ghostID);
                    return;
                }
            }

            orig(self, ghostID);
        }

        private void RainWorldGame_GoToDeathScreen(On.RainWorldGame.orig_GoToDeathScreen orig, RainWorldGame self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToDeathScreen);
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // only host can end the game
                    //OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.GoToDeathScreen);
                    return;
                }
            }

            orig(self);
        }

        private void RainWorldGame_GoToRedsGameOver(On.RainWorldGame.orig_GoToRedsGameOver orig, RainWorldGame self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToRedsGameOver);
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToRedsGameOver);
                    return;
                }
            }

            orig(self);
        }

        public static readonly string[] joarxml = {
            "<cA>",
            "<cB>",
            "<cC>",
            "<coA>",
            "<coB>",
            "<dpA>",
            "<dpB>",
            "<dpC>",
            "<dpD>",
            "<egA>",
            "<mpdA>",
            "<mpdB>",
            "<mpdC>",
            "<mwA>",
            "<mwB>",
            "<oA>",
            "<pOT>",
            "<progDivA>",
            "<progDivB>",
            "<rA>",
            "<rB>",
            "<rgA>",
            "<rgB>",
            "<rgC>",
            "<stkA>",
            "<svA>",
            "<svB>",
            "<svC>",
            "<svD>",
            "<wsA>",
        };

        public static string DeflateJoarXML(string s)
        {
            if (s is null or "") return "";
            for (var i = 0; i < joarxml.Length; i++)
                s = s.Replace(joarxml[i], ((char)(i + 1)).ToString());
            return s;
        }

        public static string InflateJoarXML(string s)
        {
            if (s is null or "") return "";
            for (var i = 0; i < joarxml.Length; i++)
                s = s.Replace(((char)(i + 1)).ToString(), joarxml[i]);
            return s;
        }

        private static string? SaveStateToString(SaveState? saveState)
        {
            if (saveState is null) return null;

            try
            {
                var s = saveState.SaveToString();
                RainMeadow.Debug($"origSaveState[{s.Length}]:{s}");
                s = Regex.Replace(s, @"(?<=>)(TUTMESSAGES|SONGSPLAYRECORDS|LINEAGES|OBJECTS|OBJECTTRACKERS|POPULATION|STICKS|RESPAWNS|WAITRESPAWNS|COMMUNITIES|SWALLOWEDITEMS|UNRECOGNIZEDSWALLOWED|FLOWERPOS)<(.*?)B>.*?<\2A>", "");
                RainMeadow.Debug($"trimSaveState[{s.Length}]:{s}");
                s = DeflateJoarXML(s);
                RainMeadow.Debug($"abbrSaveState[{s.Length}]");
                return s;
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
            return null;
        }

        private SaveState PlayerProgression_GetOrInitiateSaveState(On.PlayerProgression.orig_GetOrInitiateSaveState orig, PlayerProgression self, SlugcatStats.Name saveStateNumber, RainWorldGame game, ProcessManager.MenuSetup setup, bool saveAsDeathOrQuit)
        {
            var currentSaveState = orig(self, saveStateNumber, game, setup, saveAsDeathOrQuit);
            if (isStoryMode(out var storyGameMode))
            {
                currentSaveState.progression ??= self;
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.saveStateString = SaveStateToString(currentSaveState);
                }
                else
                {
                    currentSaveState.LoadGame(InflateJoarXML(storyGameMode.saveStateString ?? ""), game);
                }

                storyGameMode.myLastDenPos ??= storyGameMode.defaultDenPos;
                if (storyGameMode.myLastDenPos is not null) currentSaveState.denPosition = storyGameMode.myLastDenPos;
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.defaultDenPos = currentSaveState.denPosition;
                }

                return currentSaveState;
            }

            return currentSaveState;
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
                    else if (gameMode.isInGame || self.ID == MoreSlugcats.MoreSlugcatsEnums.ProcessID.KarmaToMinScreen)
                    {
                        RainMeadow.Debug("Continue - client");
                    }
                    else
                    {
                        sender.toggled = !sender.toggled;
                        isPlayerReady = sender.toggled;
                        RainMeadow.Debug(sender.toggled ? "Ready!" : "Cancelled!");
                        return;
                    }
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
                else if (gameMode.isInGame || self.ID == MoreSlugcats.MoreSlugcatsEnums.ProcessID.KarmaToMinScreen)  // arti's ending continues into slideshow
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

        private void RegionGate_Update(ILContext il)
        {
            // if (story.readyForGate == 1)
            //     open gate
            // else
            //     story.storyClientData.readyForGate = true
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<RegionGate>("mode"),
                    i => i.MatchLdsfld<RegionGate.Mode>("MiddleClosed"),
                    i => i.MatchCall("ExtEnum`1<RegionGate/Mode>", "op_Equality"),
                    i => i.MatchBrfalse(out _)
                );
                c.EmitDelegate(() =>
                {
                    if (isStoryMode(out var story))
                    {
                        if (story.readyForGate == 1) return true;
                        story.storyClientData.readyForGate = false;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brtrue, skip);
                c.GotoNext(moveType: MoveType.AfterLabel,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdsfld<RegionGate.Mode>("ClosingAirLock"),
                    i => i.MatchStfld<RegionGate>("mode")
                );
                c.EmitDelegate(() =>
                {
                    if (isStoryMode(out var story))
                    {
                        story.storyClientData.readyForGate = true;
                        return true;
                    }
                    return false;
                });
                c.Emit(OpCodes.Brfalse, skip);
                c.Emit(OpCodes.Ret);
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private int RegionGate_PlayersInZone(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            var ret = orig(self);
            if (isStoryMode(out var storyGameMode))
            {
                foreach (var ac in OnlineManager.lobby.playerAvatars.Where(kvp => !kvp.Key.isMe).Select(kvp => kvp.Value.FindEntity())
                    .Select(oe => (oe as OnlinePhysicalObject)?.apo).OfType<AbstractCreature>())
                {
                    if (ac.Room.index != self.room.abstractRoom.index || ret != self.DetectZone(ac))
                        return -1;
                }
            }
            return ret;
        }

        private bool RegionGate_PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                foreach (var ac in OnlineManager.lobby.playerAvatars.Where(kvp => !kvp.Key.isMe).Select(kvp => kvp.Value.FindEntity())
                    .Select(oe => (oe as OnlinePhysicalObject)?.apo).OfType<AbstractCreature>())
                {
                    if (ac.realizedCreature is Player p && p.touchedNoInputCounter < 20)
                        return false;
                }
            }
            return orig(self);
        }

        private bool RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
        {
            var ret = orig(self);
            if (isStoryMode(out var storyGameMode))
            {
                storyGameMode.storyClientData.readyForGate = !ret;
                ret = storyGameMode.readyForGate == 0;
            }
            return ret;
        }

        private void GhostHunch_Update(On.GhostHunch.orig_Update orig, GhostHunch self, bool eu)
        {
            orig(self, eu);
            if (isStoryMode(out _) && !OnlineManager.lobby.isOwner)
            {
                if (self.ghostNumber != null)
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.TriggerGhostHunch, self.ghostNumber.value);
            }
        }
    }
}