using Kittehface.Framework20;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void JollyHooks()
        {

            // feature hooks
            new Hook(typeof(RainWorldGame).GetProperty(nameof(RainWorldGame.StoryPlayerCount)).GetGetMethod(),
                RainWorldGame_get_StoryPlayerCount);

            On.RainWorldGame.ctor += RainWorldGame_ctorSetUserInput;
            On.JollyCoop.JollyMenu.JollySetupDialog.Update += JollySetupDialog_Update;
            On.JollyCoop.JollyMenu.SymbolButtonToggle.Update += SymbolButtonToggle_Update;
            On.JollyCoop.JollyMenu.JollySlidingMenu.Singal += JollySlidingMenu_Singal;
            On.ProcessManager.IsGameInMultiplayerContext += ProcessManager_IsGameInMultiplayerContext;
            On.RoomRealizer.CanAbstractizeRoom += RoomRealizer_CanAbstractizeRoom;
            On.JollyCoop.JollyCustom.SlugClassMenu += JollyCoop_JollyCustom_SlugClassMenu;
            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud1;
            On.RoomCamera.ChangeCameraToPlayer += RoomCamera_ChangeCameraToPlayer;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.Update += JollyPlayerSelector_Update;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.GetPupButtonOffName += GetPupButtonOffName;
            IL.JollyCoop.JollyHUD.JollyMeter.ctor += JollyMeter_ctor;
            IL.JollyCoop.JollyHUD.JollyMeter.Draw += JollyMeter_Draw;
            IL.JollyCoop.JollyHUD.JollyMeter.PlayerIcon.Update += JollyMeter_PlayerIcon_Draw;
            On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess += SlugcatSelectMenu_CommunicateWithUpcomingProcess;
            On.Menu.InputOptionsMenu.Singal += InputOptionsMenu_Singal;
            On.RoomCamera.Update += RoomCamera_Update1;
            IL.RoomSpecificScript.SU_C04StartUp.Update += SU_C04StartUp_Update;


            // disabling jolly co-op code.
            IL.Menu.CharacterSelectPage.ctor += SoftDisableJollyCoOP;
            IL.Menu.CharacterSelectPage.SetUpSelectables += SoftDisableJollyCoOP;
            IL.Menu.CharacterSelectPage.Update += SoftDisableJollyCoOP;
            IL.Menu.CharacterSelectPage.UpdateSelectedSlugcat += SoftDisableJollyCoOP;
            IL.Menu.InitializationScreen.Update += SoftDisableJollyCoOP;
            IL.Menu.MainMenu.ctor += SoftDisableJollyCoOP;

            IL.Menu.SlugcatSelectMenu.AddColorInterface += SoftDisableJollyCoOP;
            IL.Menu.SlugcatSelectMenu.ctor += SoftDisableJollyCoOP;
            IL.Menu.SlugcatSelectMenu.Update += SoftDisableJollyCoOP;
            
            // IL.Menu.SlugcatSelectMenu.CheckJollyCoopAvailable += SoftDisableJollyCoOP;

            // IL.Options.ApplyOption += SoftDisableJollyCoOP; 
            // IL.Options.CheckJollyProfileRequestNeeded += SoftDisableJollyCoOP; 
            // IL.Options.get_JollyPlayerCount += SoftDisableJollyCoOP; 
            // IL.Options.ToString += SoftDisableJollyCoOP; 


            IL.PlayerProgression.SaveToDisk += SoftDisableJollyCoOP;
            IL.PlayerProgression.SyncLoadModState += SoftDisableJollyCoOP;


            // IL.ProcessManager.IsGameInMultiplayerContext += SoftDisableJollyCoOP; 
            // IL.RainWorld.LoadModResources += SoftDisableJollyCoOP; 
            // IL.RainWorld.OnModsInit += SoftDisableJollyCoOP; 

            // needs it's own hook to call UserInput.SetUserCount after  
            // IL.RainWorldGame.ctor += SoftDisableJollyCoOP; 
            // IL.SlugcatStats.HiddenOrUnplayableSlugcat += SoftDisableJollyCoOP; 

            IL.AbstractCreature.IsEnteringDen += SoftDisableJollyCoOP;
            IL.AbstractCreature.RealizeInRoom += SoftDisableJollyCoOP;
            IL.Creature.Update += SoftDisableJollyCoOP;
            IL.DaddyCorruption.EatenCreature.Update += SoftDisableJollyCoOP;
            IL.Ghost.Update += SoftDisableJollyCoOP;

            // the jolly code here is fine.
            IL.HardmodeStart.ctor += WhiteListJollyCoop;
            IL.HardmodeStart.Update += WhiteListJollyCoop;

            IL.HUD.HUD.InitSinglePlayerHud += SoftDisableJollyCoOP;
            IL.HUD.TextPrompt.EnterGameOverMode += SoftDisableJollyCoOP;
            IL.HUD.TextPrompt.Update += SoftDisableJollyCoOP;
            IL.ImageTrigger.AttemptTriggerFire += SoftDisableJollyCoOP;
            IL.KarmaFlower.BitByPlayer += SoftDisableJollyCoOP;
            IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += SoftDisableJollyCoOP;
            IL.Menu.ChallengeSelectPage.StartGame += SoftDisableJollyCoOP;
            IL.Menu.CharacterSelectPage.LoadGame += SoftDisableJollyCoOP;
            IL.Menu.ControlMap.ctor += SoftDisableJollyCoOP;
            IL.Menu.ExpeditionGameOver.Singal += SoftDisableJollyCoOP;
            IL.Menu.ExpeditionMenu.CommunicateWithUpcomingProcess += SoftDisableJollyCoOP;
            IL.Menu.InputOptionsMenu.Singal += SoftDisableJollyCoOP;
            IL.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess += SoftDisableJollyCoOP;
            IL.Menu.SlugcatSelectMenu.StartGame += SoftDisableJollyCoOP;
            IL.Menu.SlugcatSelectMenu.Update += SoftDisableJollyCoOP;
            IL.MoreSlugcats.BreathMeter.Update += SoftDisableJollyCoOP;
            IL.MoreSlugcats.MSCRoomSpecificScript.GW_C05ArtificerMessage.Update += SoftDisableJollyCoOP;
            IL.MoreSlugcats.MSCRoomSpecificScript.OE_GourmandEnding.Update += SoftDisableJollyCoOP;
            IL.MoreSlugcats.MSCRoomSpecificScript.SU_A42Message.Update += SoftDisableJollyCoOP;
            IL.MoreSlugcats.MSCRoomSpecificScript.SU_PMPSTATION01_safety.Update += SoftDisableJollyCoOP;
            IL.MoreSlugcats.MSCRoomSpecificScript.SU_SMIntroMessage.Update += SoftDisableJollyCoOP;
            IL.OracleBehavior.FindPlayer += SoftDisableJollyCoOP;
            IL.OracleBehavior.ctor += SoftDisableJollyCoOP;

            // IL.OverseerAbstractAI.get_RelevantPlayer += SoftDisableJollyCoOP; 

            IL.Player.AddFood += WhiteListJollyCoop;
            IL.Player.AddQuarterFood += WhiteListJollyCoop;
            IL.Player.SubtractFood += WhiteListJollyCoop;


            IL.Player.CanEatMeat += SoftDisableJollyCoOP;
            IL.Player.CanIPickThisUp += SoftDisableJollyCoOP;
            IL.Player.CanIPutDeadSlugOnBack += SoftDisableJollyCoOP;
            IL.Player.CanMaulCreature += SoftDisableJollyCoOP;
            IL.Player.checkInput += SoftDisableJollyCoOP;
            IL.Player.ClassMechanicsArtificer += SoftDisableJollyCoOP;
            IL.Player.Collide += SoftDisableJollyCoOP;
            IL.Player.Destroy += SoftDisableJollyCoOP;
            IL.Player.Die += SoftDisableJollyCoOP;
            IL.Player.GetHeldItemDirection += SoftDisableJollyCoOP;

            IL.Player.GetInitialSlugcatClass += SoftDisableJollyCoOP;

            // new ILHook(typeof(Player).GetProperty(nameof(Player.CanPutSlugToBack)).GetGetMethod(), SoftDisableJollyCoOP); 
            // IL.Player.get_CanPutSpearToBack += SoftDisableJollyCoOP; 
            // IL.Player.get_CanRetrieveSlugFromBack += SoftDisableJollyCoOP; 

            // IL.Player.get_InitialShortcutWaitTime += SoftDisableJollyCoOP; 

            new ILHook(typeof(Player).GetProperty(nameof(Player.RevealMap)).GetGetMethod(), SoftDisableJollyCoOP);
            new ILHook(typeof(Player).GetProperty(nameof(Player.slugcatStats)).GetGetMethod(), SoftDisableJollyCoOP);

            IL.Player.Grabability += SoftDisableJollyCoOP;
            IL.Player.GrabUpdate += SoftDisableJollyCoOP;
            IL.Player.GraphicsModuleUpdated += SoftDisableJollyCoOP;
            IL.Player.HeavyCarry += SoftDisableJollyCoOP;
            IL.Player.JollyUpdate += SoftDisableJollyCoOP;
            IL.Player.ObjectEaten += SoftDisableJollyCoOP;
            IL.Player.PermaDie += SoftDisableJollyCoOP;
            IL.Player.ctor += SoftDisableJollyCoOP;
            IL.Player.ProcessChatLog += SoftDisableJollyCoOP;
            IL.Player.PyroDeathThreshold += SoftDisableJollyCoOP;
            IL.Player.SaveStomachObjectInPlayerState += SoftDisableJollyCoOP;
            IL.Player.SetMalnourished += SoftDisableJollyCoOP;
            IL.Player.SlugcatGrab += SoftDisableJollyCoOP;
            IL.Player.SlugOnBack.GraphicsModuleUpdated += SoftDisableJollyCoOP;
            IL.Player.SlugOnBack.SlugToBack += SoftDisableJollyCoOP;
            IL.Player.SlugOnBack.Update += SoftDisableJollyCoOP;
            IL.Player.SlugSlamConditions += SoftDisableJollyCoOP;
            IL.Player.Stun += SoftDisableJollyCoOP;
            IL.Player.Update += SoftDisableJollyCoOP;
            IL.Player.UpdateMSC += SoftDisableJollyCoOP;
            IL.PlayerGraphics.ApplyPalette += SoftDisableJollyCoOP;
            IL.PlayerGraphics.CosmeticPearl.DrawSprites += SoftDisableJollyCoOP;
            IL.PlayerGraphics.CustomColorsEnabled += SoftDisableJollyCoOP;
            IL.PlayerGraphics.DrawSprites += SoftDisableJollyCoOP;
            // new ILHook(typeof(PlayerGraphics).GetProperty(nameof(PlayerGraphics.RenderAsPup)).GetGetMethod(), SoftDisableJollyCoOP);  
            new ILHook(typeof(PlayerGraphics).GetProperty(nameof(PlayerGraphics.useJollyColor)).GetGetMethod(), SoftDisableJollyCoOP);
            IL.PlayerGraphics.MSCUpdate += SoftDisableJollyCoOP;
            IL.PlayerGraphics.ctor += SoftDisableJollyCoOP;
            // IL.PlayerGraphics.SlugcatColor += SoftDisableJollyCoOP; 
            IL.PlayerGraphics.TailSpeckles.DrawSprites += SoftDisableJollyCoOP;
            IL.ProcessManager.IsGameInMultiplayerContext += SoftDisableJollyCoOP;
            IL.RainWorld.Update += SoftDisableJollyCoOP;
            IL.RainWorldGame.AppendCycleToStatisticsForPlayers += SoftDisableJollyCoOP;
            IL.RainWorldGame.BeatGameMode += SoftDisableJollyCoOP;
            IL.RainWorldGame.CommunicateWithUpcomingProcess += SoftDisableJollyCoOP;
            IL.RainWorldGame.GameOver += SoftDisableJollyCoOP;

            // IL.RainWorldGame.get_FirstAlivePlayer += SoftDisableJollyCoOP; 
            // IL.RainWorldGame.get_FirstAnyPlayer += SoftDisableJollyCoOP; 
            // IL.RainWorldGame.get_StoryPlayerCount += SoftDisableJollyCoOP; 

            IL.RainWorldGame.GoToRedsGameOver += SoftDisableJollyCoOP;
            IL.RainWorldGame.InActiveGate += SoftDisableJollyCoOP;
            IL.RainWorldGame.InClosingShelter += SoftDisableJollyCoOP;
            IL.RainWorldGame.ctor += SoftDisableJollyCoOP;
            IL.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += SoftDisableJollyCoOP;
            IL.RedsIllness.ctor += SoftDisableJollyCoOP;

            // RegionGate.get_MeetRequirement += SoftDisableJollyCoOP; 

            IL.RegionGate.ListOfPlayersInZone += SoftDisableJollyCoOP;
            IL.RegionGate.PlayersInZone += SoftDisableJollyCoOP;
            IL.RegionGate.PlayersStandingStill += SoftDisableJollyCoOP;

            IL.RoomCamera.EnterCutsceneMode += SoftDisableJollyCoOP;
            IL.RoomCamera.Update += SoftDisableJollyCoOP;

            IL.RoomRealizer.CanAbstractizeRoom += SoftDisableJollyCoOP;

            // Also fine...
            IL.RoomSpecificScript.SB_A14KarmaIncrease.Update += WhiteListJollyCoop;
            IL.RoomSpecificScript.SL_C12JetFish.ctor += WhiteListJollyCoop;
            IL.RoomSpecificScript.SU_C04StartUp.Update += WhiteListJollyCoop;

            // 
            IL.ShelterDoor.DoorClosed += WhiteListJollyCoop;


            IL.SaveState.BringUpToDate += SoftDisableJollyCoOP;
            IL.SaveState.SessionEnded += SoftDisableJollyCoOP;
            IL.ShelterDoor.Close += SoftDisableJollyCoOP;
            
            IL.ShelterDoor.Update += SoftDisableJollyCoOP;
            IL.ShortcutHandler.SuckInCreature += SoftDisableJollyCoOP;
            IL.ShortcutHandler.Update += SoftDisableJollyCoOP;
            IL.SLOracleBehavior.Update += SoftDisableJollyCoOP;
            IL.SlugcatHand.Update += SoftDisableJollyCoOP;
            IL.Spear.DrawSprites += SoftDisableJollyCoOP;
            IL.Spear.HitSomethingWithoutStopping += SoftDisableJollyCoOP;
            IL.SSOracleBehavior.SeePlayer += SoftDisableJollyCoOP;
            IL.SSOracleBehavior.SSOracleGetGreenNeuron.Update += SoftDisableJollyCoOP;
            IL.SSOracleBehavior.ThrowOutBehavior.Update += SoftDisableJollyCoOP;
            IL.SSOracleBehavior.Update += SoftDisableJollyCoOP;
            IL.StoryGameSession.PlaceKarmaFlowerOnDeathSpot += SoftDisableJollyCoOP;
            IL.StoryGameSession.TimeTick += SoftDisableJollyCoOP;
            IL.VoidSea.VoidSeaScene.DestroyMainWorm += SoftDisableJollyCoOP;
            IL.VoidSea.VoidSeaScene.MovedToSecondSpace += SoftDisableJollyCoOP;
            IL.VoidSea.VoidSeaScene.TheEgg.DrawSprites += SoftDisableJollyCoOP;
            IL.VoidSea.VoidSeaScene.TheEgg.Update += SoftDisableJollyCoOP;
            IL.VoidSea.VoidSeaScene.Update += SoftDisableJollyCoOP;
            IL.VoidSea.VoidSeaScene.WormLightFade.DrawSprites += SoftDisableJollyCoOP;
            IL.VoidSea.VoidWorm.Arm.Update += SoftDisableJollyCoOP;
            IL.VoidSea.VoidWorm.BackgroundWormBehavior.Update += SoftDisableJollyCoOP;
            IL.VoidSea.VoidWorm.Head.Update += SoftDisableJollyCoOP;
            IL.VoidSea.VoidWorm.MainWormBehavior.Update += SoftDisableJollyCoOP;
            IL.VoidSea.VoidWorm.Update += SoftDisableJollyCoOP;
            IL.Vulture.AccessSkyGate += SoftDisableJollyCoOP;
            IL.Weapon.HitThisObject += SoftDisableJollyCoOP;
            IL.WormGrass.WormGrassPatch.InteractWithCreature += SoftDisableJollyCoOP;
        }

        bool ProcessManager_IsGameInMultiplayerContext(On.ProcessManager.orig_IsGameInMultiplayerContext orig, ProcessManager self)
        {
            if (isStoryMode(out var story) && (story.avatarCount > 1))
            {
                return true;
            }


            return orig(self);
        }

        void SymbolButtonToggle_Update(On.JollyCoop.JollyMenu.SymbolButtonToggle.orig_Update orig, JollyCoop.JollyMenu.SymbolButtonToggle self)
        {
            orig(self);
            if (self.buttonBehav.greyedOut && self is not JollyCoop.JollyMenu.SymbolButtonTogglePupButton)
            {
                self.symbol.color = Menu.Menu.MenuColor(Menu.Menu.MenuColors.DarkGrey).rgb;
            }
        }

        void JollySlidingMenu_Singal(On.JollyCoop.JollyMenu.JollySlidingMenu.orig_Singal orig, JollyCoop.JollyMenu.JollySlidingMenu self, Menu.MenuObject sender, string message)
        {


            if (isStoryMode(out var story))
            {
                if (message.Contains("CLASSCHANGE"))
                {
                    for (int i = 0; i < self.playerSelector.Length; i++)
                    {
                        if (message == "CLASSCHANGE" + i)
                        {
                            if (self.menu.manager.currentMainLoop is StoryOnlineMenu story_menu)
                            {
                                var currentslugcat = story_menu.playerSelectedSlugcats[i];
                                if (currentslugcat is null)
                                {
                                    currentslugcat = story.currentCampaign;
                                }

                                int current_index = story_menu.SelectableSlugcats.IndexOf(currentslugcat);
                                int newcharacterindex = (current_index + 1) % story_menu.SelectableSlugcats.Length;
                                story_menu.playerSelectedSlugcats[i] = story_menu.SelectableSlugcats[newcharacterindex];

                                self.JollyOptions(i).playerClass = story_menu.SelectableSlugcats[newcharacterindex];
                                self.menu.PlaySound(SoundID.MENU_Error_Ping);
                                self.SetPortraitsDirty();
                                return;
                            }
                        }
                    }
                }

                if (message.Equals("friendly_fire"))
                {
                    story.friendlyFire = message.Contains("on");
                }
            }

            orig(self, sender, message);
        }

        private string GetPupButtonOffName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_GetPupButtonOffName orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
        {
            if (OnlineManager.lobby != null)
            {
                SlugcatStats.Name playerClass = self.JollyOptions(self.index).playerClass;
                if (ModManager.MSC && playerClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                {
                    return "pup_on";
                }
            }

            return orig(self);
        }


        private void JollySetupDialog_Update(On.JollyCoop.JollyMenu.JollySetupDialog.orig_Update orig, JollyCoop.JollyMenu.JollySetupDialog self)
        {
            orig(self);

            if (isStoryMode(out var story))
            {
                for (int i = 0; i < story.avatarCount; i++)
                {
                    if (self.manager.currentMainLoop is StoryOnlineMenu story_menu)
                    {
                        var currentslugcat = story_menu.playerSelectedSlugcats[i];
                        
                        if (currentslugcat != self.slidingMenu.playerSelector[i].slugName)
                            self.slidingMenu.playerSelector[i].dirty = true;
                        
                        if (currentslugcat is null)
                        {
                            currentslugcat = story.currentCampaign;
                        }

                        if (!ModManager.MSC)
                        {
                            self.slidingMenu.playerSelector[i].pupButton.buttonBehav.greyedOut = true;
                            if (self.slidingMenu.playerSelector[i].pupButton.isToggled)
                            {
                                self.slidingMenu.playerSelector[i].pupButton.Toggle();
                            }
                        }
                        else
                        {
                            if (currentslugcat == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
                            {
                                self.slidingMenu.playerSelector[i].pupButton.buttonBehav.greyedOut = true;
                            }
                        }


                    }
                }

                // self.slidingMenu.friendlyToggle.buttonBehav.greyedOut = true;
                // self.slidingMenu.cameraCyclesToggle.buttonBehav.greyedOut = true;
                self.slidingMenu.smartShortcutToggle.buttonBehav.greyedOut = true;
                self.slidingMenu.friendlyLizardsToggle.buttonBehav.greyedOut = true;
                self.slidingMenu.friendlySteal.buttonBehav.greyedOut = true;
                // self.slidingMenu.hudToggle.buttonBehav.greyedOut = true;

                if (self.slidingMenu.friendlyToggle.isToggled == story.friendlyFire) self.slidingMenu.friendlyToggle.Toggle();
                if (!self.slidingMenu.friendlyLizardsToggle.isToggled) self.slidingMenu.friendlyToggle.Toggle();
                // if (!self.slidingMenu.cameraCyclesToggle.isToggled) self.slidingMenu.cameraCyclesToggle.Toggle();
                if (self.slidingMenu.smartShortcutToggle.isToggled) self.slidingMenu.smartShortcutToggle.Toggle();
                if (self.slidingMenu.friendlySteal.isToggled) self.slidingMenu.friendlySteal.Toggle();
                // if (self.slidingMenu.hudToggle.isToggled) self.slidingMenu.hudToggle.Toggle();
            }

        }


        private void SU_C04StartUp_Update(ILContext context)
        {
            try
            {
                ILCursor c = new(context);
                int i = 0;

                var sw = Stopwatch.StartNew();
                while (c.TryGotoNext(MoveType.After,
                    x => (x.MatchLdfld<Room>(nameof(Room.physicalObjects)))
                ))
                {
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate((List<PhysicalObject>[] objs, RoomSpecificScript.SU_C04StartUp startup) =>
                    {
                        if (isStoryMode(out _))
                        {
                            return objs.Select(x => x.Where(y => y?.IsLocal() ?? true).ToList()).ToArray();
                        }

                        return objs;
                    });

                    i++;
                }
                sw.Stop();
                RainMeadow.Debug($"Replacec {i} SU_C04 physical object references in {sw.Elapsed.TotalSeconds}s");
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }


        private void WhiteListJollyCoop(ILContext context)
        {
            try
            {
                ILCursor c = new(context);
                int i = 0;

                var sw = Stopwatch.StartNew();
                while (c.TryGotoNext(MoveType.After,
                    x => (x.MatchLdsfld<ModManager>(nameof(ModManager.JollyCoop)) || x.MatchLdsfld<ModManager>(nameof(ModManager.CoopAvailable)))
                ))
                {
                    c.Emit(OpCodes.Ldc_I4, c.Prev.MatchLdsfld<ModManager>(nameof(ModManager.CoopAvailable)) ? 1 : 0);
                    c.EmitDelegate((bool value, int coopavail) =>
                    {
                        if (isStoryMode(out var story))
                        {
                            if (coopavail != 0)
                            {
                                return story.avatarCount > 1;
                            }

                            return ModManager.JollyCoop;
                        }

                        return value;
                    });

                    i++;
                }
                sw.Stop();
                RainMeadow.Debug($"Replace {i} Jolly CoOP checks in {context.Method.Name} in {sw.Elapsed.TotalSeconds}s");
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }


        private void SoftDisableJollyCoOP(ILContext context)
        {
            try
            {
                ILCursor c = new(context);
                int i = 0;

                var sw = Stopwatch.StartNew();
                while (c.TryGotoNext(MoveType.After,
                    x => (x.MatchLdsfld<ModManager>(nameof(ModManager.JollyCoop)) || x.MatchLdsfld<ModManager>(nameof(ModManager.CoopAvailable)))
                ))
                {
                    c.EmitDelegate((bool value) =>
                    {
                        if (OnlineManager.lobby != null)
                        {
                            return false; // all Jolly co-op features need to reimplented anyway. 
                        }

                        return value;
                    });

                    i++;
                }
                sw.Stop();
                RainMeadow.Debug($"Replace {i} Jolly CoOP checks in {context.Method.Name} in {sw.Elapsed.TotalSeconds}s");
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        private int RainWorldGame_get_StoryPlayerCount(Func<RainWorldGame, int> orig, RainWorldGame self)
        {
            if (isStoryMode(out var story))
            {
                return story.avatarCount;
            }

            return orig(self);
        }

        private void RainWorldGame_ctorSetUserInput(On.RainWorldGame.orig_ctor orig, global::RainWorldGame self, global::ProcessManager manager)
        {
            orig(self, manager);

            if (isStoryMode(out var story))
            {
                UserInput.SetUserCount(story.avatarCount);
            }
        }

        private bool RoomRealizer_CanAbstractizeRoom(On.RoomRealizer.orig_CanAbstractizeRoom orig, RoomRealizer self, RoomRealizer.RealizedRoomTracker tracker)
        {
            if (OnlineManager.lobby != null)
            {
                foreach (AbstractCreature creature in OnlineManager.lobby.gameMode.avatars.Select(x => x.abstractCreature))
                {
                    if (tracker.room.creatures.Contains(creature))
                    {
                        return false;
                    }
                }
            }

            return orig(self, tracker);
        }

        private SlugcatStats.Name JollyCoop_JollyCustom_SlugClassMenu(On.JollyCoop.JollyCustom.orig_SlugClassMenu orig, int playerNumber, SlugcatStats.Name fallBack)
        {
            if (isStoryMode(out var story))
            {
                if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is StoryOnlineMenu menu)
                {
                    return menu.playerSelectedSlugcats?[playerNumber] ?? menu.slugcatColorOrder[menu.slugcatPageIndex];
                }

            }
            return orig(playerNumber, fallBack);
        }

        private void HUD_InitSinglePlayerHud1(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            orig(self, cam);

            if (OnlineManager.lobby != null && ModManager.JollyCoop && self.rainWorld.options.jollyHud)
            {
                self.AddPart(new JollyCoop.JollyHUD.JollyMeter(self, self.fContainers[1]));
            }
        }

        private void RoomCamera_ChangeCameraToPlayer(On.RoomCamera.orig_ChangeCameraToPlayer orig, RoomCamera self, AbstractCreature cameraTarget)
        {
            orig(self, cameraTarget);
            if (OnlineManager.lobby != null)
            {
                RainMeadow.DebugMe();
                var spectator = self.hud.parts.OfType<SpectatorHud>().FirstOrDefault();
                if (spectator is not null && spectator.isSpectating)
                {
                    spectator.ClearSpectatee();
                }

                if (self.followAbstractCreature is not null)
                {
                    if (self.followAbstractCreature.Room.realizedRoom is null)
                    {
                        self.followAbstractCreature.Room.world.ActivateRoom(self.followAbstractCreature.Room);
                    }

                    if (self.room?.abstractRoom != self.followAbstractCreature.Room && self.followAbstractCreature.Room.realizedRoom is not null)
                    {
                        self.MoveCamera(self.followAbstractCreature.Room.realizedRoom, -1);
                    }
                    self.GetCameraBestIndex();
                }
            }

        }

        private void JollyPlayerSelector_Update(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_Update orig, JollyCoop.JollyMenu.JollyPlayerSelector self)
        {
            orig(self);
            if (isStoryMode(out var story))
            {
                self.playerLabelSelector.greyedOut = !self.Joined || (self.dialog.Options.JollyPlayerCount > 1);
            }
        }

        private void JollyMeter_ctor(ILContext ctx)
        {
            try
            {
                ILCursor cursor = new(ctx);
                cursor.GotoNext(MoveType.After, x => x.MatchCall<PlayerGraphics>(nameof(PlayerGraphics.SlugcatColor)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate((Color orig_color, JollyCoop.JollyHUD.JollyMeter self, List<AbstractCreature> players, int index) =>
                {
                    try
                    {
                        if (isStoryMode(out var story))
                        {
                            RainMeadow.Debug(story.avatarSettings[index].bodyColor);
                            return story.avatarSettings[index].bodyColor;
                        }

                    }
                    catch (Exception except)
                    {
                        RainMeadow.Error(except);
                    }
                    return orig_color;
                });


            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }


        private void JollyMeter_Draw(ILContext ctx)
        {
            try
            {
                ILCursor cursor = new(ctx);
                while (cursor.TryGotoNext(MoveType.After, x => x.MatchCall<PlayerGraphics>(nameof(PlayerGraphics.SlugcatColor))))
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate((Color orig_color, JollyCoop.JollyHUD.JollyMeter self) =>
                    {
                        try
                        {
                            if (isStoryMode(out var story))
                            {
                                return story.avatarSettings[self.playerStateFocusedByCamera.playerNumber].bodyColor;
                            }

                        }
                        catch (Exception except)
                        {
                            RainMeadow.Error(except);
                        }
                        return orig_color;
                    });

                }


            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }


        private void JollyMeter_PlayerIcon_Draw(ILContext ctx)
        {
            try
            {
                ILCursor cursor = new(ctx);

                while (cursor.TryGotoNext(MoveType.After, x => x.MatchCall<PlayerGraphics>(nameof(PlayerGraphics.SlugcatColor))))
                {
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate((Color orig_color, JollyCoop.JollyHUD.JollyMeter.PlayerIcon self) =>
                    {
                        try
                        {
                            if (isStoryMode(out var story))
                            {
                                return story.avatarSettings[self.playerState.playerNumber].bodyColor;
                            }

                        }
                        catch (Exception except)
                        {
                            RainMeadow.Error(except);
                        }
                        return orig_color;
                    });
                }


            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        private void RoomCamera_Update1(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            if (ModManager.JollyCoop)
            {
                if (self.cameraTriggerCooldown > 0)
                {
                    self.cameraTriggerCooldown--;
                }

                if (self.hud != null)
                {
                    if (self.followAbstractCreature != null)
                    {
                        if (self.followAbstractCreature.realizedCreature != null)
                        {
                            if (self.followAbstractCreature.realizedCreature is Player p)
                            {
                                if (self.hud?.owner != p && p.IsLocal())
                                {
                                    self.hud!.owner = p;
                                }
                            }
                        }
                    }
                }

            }

            orig(self);
        }
        public void SlugcatSelectMenu_CommunicateWithUpcomingProcess(On.Menu.SlugcatSelectMenu.orig_CommunicateWithUpcomingProcess orig, Menu.SlugcatSelectMenu self, MainLoopProcess nextProcess)
        {
            if (self is StoryOnlineMenu && ModManager.JollyCoop)
            {
                if (nextProcess is Menu.InputOptionsMenu inputOptions)
                {
                    inputOptions.fromJollyMenu = true;
                    inputOptions.previousMenu = RainMeadow.Ext_ProcessID.StoryMenu;
                    return;
                }
            }

            orig(self, nextProcess);
        }

        private void InputOptionsMenu_Singal(On.Menu.InputOptionsMenu.orig_Singal orig, Menu.InputOptionsMenu self, Menu.MenuObject sender, string message)
        {
            if (OnlineManager.lobby != null)
            {
                if (message == "BACK")
                {
                    if (self.previousMenu != null)
                    {
                        self.manager.RequestMainProcessSwitch(self.previousMenu);
                    }
                    else
                    {
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.OptionsMenu);
                    }
                    self.PlaySound(SoundID.MENU_Switch_Page_Out);
                    self.manager.rainWorld.options.Save();
                    return;
                }
            }

            orig(self, sender, message);
        }
    }
}
