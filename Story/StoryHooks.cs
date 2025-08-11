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

        public static bool inVoidSea = false;

        private void StoryHooks()
        {
            On.PlayerProgression.GetOrInitiateSaveState += PlayerProgression_GetOrInitiateSaveState;
            On.PlayerProgression.SaveToDisk += PlayerProgression_SaveToDisk;
            On.Menu.KarmaLadderScreen.Update += KarmaLadderScreen_Update;
            On.Menu.KarmaLadderScreen.Singal += KarmaLadderScreen_Singal;
            On.HUD.KarmaMeter.RippleSymbolSprite += HUD_KarmaMeter_RippleSymbolSprite;

            On.Menu.SleepAndDeathScreen.AddPassageButton += SleepAndDeathScreen_AddPassageButton;
            On.Menu.CustomEndGameScreen.GetDataFromSleepScreen += CustomEndGameScreen_GetDataFromSleepScreen;
            On.Menu.FastTravelScreen.ctor += FastTravelScreen_ctor;
            IL.Menu.FastTravelScreen.ctor += FastTravelScreen_ctor_ClientDontFilterRegions;
            On.Menu.FastTravelScreen.StepRegion += FastTravelScreen_StepRegion;
            On.Menu.FastTravelScreen.InitiateRegionSwitch += FastTravelScreen_InitiateRegionSwitch;
            On.Menu.FastTravelScreen.Update += FastTravelScreen_Update;
            On.Menu.FastTravelScreen.Singal += FastTravelScreen_Singal_ClientLoadGameNormally;

            On.HUD.HUD.InitSinglePlayerHud += HUD_InitSinglePlayerHud;

            IL.HUD.FoodMeter.TrySpawnPupBars += FoodMeter_TrySpawnPupBars_LobbyOwner;
            On.HUD.FoodMeter.TrySpawnPupBars += FoodMeter_TrySpawnPupBars_LobbyClient;
            // On.SlugcatStats.SlugcatFoodMeter += SlugcatStats_SlugcatFoodMeter;

            IL.HardmodeStart.ctor += HardmodeStart_ctor;
            new Hook(typeof(HardmodeStart.HardmodePlayer).GetProperty("MainPlayer").GetGetMethod(), this.HardmodeStart_HardmodePlayer_MainPlayer);
            IL.HardmodeStart.SinglePlayerUpdate += HardmodeStart_SinglePlayerUpdate;

            IL.Player.ctor += Player_ctor_NonHunterCampaignClientDisableRedsIllness;
            On.Player.ctor += Player_ctor_SynchronizeFoodBarForActualPlayers;

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
            new Hook(typeof(RegionGate).GetProperty("MeetRequirement").GetGetMethod(), this.RegionGate_MeetRequirement_StorySync);

            On.GhostHunch.Update += GhostHunch_Update;

            On.RainWorldGame.Win += RainWorldGame_Win;
            On.RainWorldGame.GoToStarveScreen += RainWorldGame_GoToStarveScreen;
            On.RainWorldGame.GhostShutDown += RainWorldGame_GhostShutDown;
            On.RainWorldGame.GoToDeathScreen += RainWorldGame_GoToDeathScreen;
            On.RainWorldGame.GoToRedsGameOver += RainWorldGame_GoToRedsGameOver;

            On.SaveState.SessionEnded += SaveState_SessionEnded;
            IL.SaveState.SessionEnded += SaveState_SessionEnded_DontAssumePlayerRealized;
            On.SaveState.BringUpToDate += SaveState_BringUpToDate;
            On.SaveState.GetSaveStateDenToUse += SaveState_GetSaveStateDenToUse;

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

            IL.Menu.SlugcatSelectMenu.UpdateSelectedSlugcatInMiscProg += SlugcatSelectMenu_UpdateSelectedSlugcatInMiscProg;

            IL.Menu.SlugcatSelectMenu.SliderSetValue += SlugcatSelectMenu_SliderFix;
            IL.Menu.SlugcatSelectMenu.ValueOfSlider += SlugcatSelectMenu_SliderFix;
            IL.Menu.SlugcatSelectMenu.Singal += IL_SlugcatSelectMenu_SingalFix;
            IL.Menu.SlugcatSelectMenu.SetChecked += IL_SlugcatSelectMenu_SetChecked;
            On.Menu.SlugcatSelectMenu.SetChecked += SlugcatSelectMenu_SetChecked;
            On.Menu.SlugcatSelectMenu.GetChecked += SlugcatSelectMenu_GetChecked;
            On.Menu.SlugcatSelectMenu.SliderSetValue += SlugcatSelectMenu_SliderSetValue;
            On.Menu.PauseMenu.SpawnExitContinueButtons += PauseMenu_SpawnExitContinueButtons;

            On.VoidSea.PlayerGhosts.AddGhost += PlayerGhosts_AddGhost;
            On.VoidSea.VoidSeaScene.Update += VoidSeaScene_Update;

            On.Watcher.WarpPoint.NewWorldLoaded_Room += WarpPoint_NewWorldLoaded_Room; // creature moving between WORLDS
            On.Watcher.WarpPoint.Update += Watcher_WarpPoint_Update;
            On.Watcher.WarpPoint.PerformWarp += WarpPoint_PerformWarp;
            On.Watcher.PrinceBehavior.InitateConversation += Watcher_PrinceBehavior_InitateConversation;
            IL.Watcher.Barnacle.LoseShell += Watcher_Barnacle_LoseShell;
            On.Watcher.SpinningTop.SpawnWarpPoint += SpinningTop_SpawnWarpPoint;
            On.Watcher.SpinningTop.RaiseRippleLevel += SpinningTop_RaiseRippleLevel;
            On.Watcher.SpinningTop.Update += SpinningTop_Update;
            On.Watcher.SpinningTop.VanillaRegionSpinningTopEncounter += (On.Watcher.SpinningTop.orig_VanillaRegionSpinningTopEncounter orig, Watcher.SpinningTop self) => {
                orig(self);
                if (OnlineManager.lobby != null && !OnlineManager.lobby.isOwner)
                {
                    RainMeadow.Debug($"encounter of vanilla echo st. {self.vanillaToRippleEncounter}");
                    // Only for echo 2!
                    if (!self.vanillaToRippleEncounter && (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.rippleLevel == 0.5f)
                    {
                        self.hasRequestedShutDown = true;
                        self.room.game.GetStorySession.saveState.sessionEndingFromSpinningTopEncounter = true;
                        self.room.game.Win(false, false);
                        RainWorldGame.ForceSaveNewDenLocation(self.room.game, "HI_W05", true);
                        self.DespawnEcho();
                    }
                }
            };
            On.RainWorldGame.ForceSaveNewDenLocation += RainWorldGame_ForceSaveNewDenLocation;
            
            On.Conversation.InitalizePrefixColor += (On.Conversation.orig_InitalizePrefixColor orig) => {
                try {
                    orig();
                } catch(System.IndexOutOfRangeException e) {
                    RainMeadow.Error($"conversation error (watcher echo likely) {e}");
                    //throw; nonfatal
                }
            };
        }

        private void RainWorldGame_ForceSaveNewDenLocation(On.RainWorldGame.orig_ForceSaveNewDenLocation orig, RainWorldGame game, string roomName, bool saveWorldStates)
        {
            orig(game, roomName, saveWorldStates);
            if (RainMeadow.isStoryMode(out var story)) { }
            if (!OnlineManager.lobby.isOwner)
            {
                OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.ForceSaveNewDenLocation, roomName, saveWorldStates); // tell host to save den location for everyone else
            }
            else
            {
                story.myLastDenPos = roomName;
            }
        }


        public void Watcher_WarpPoint_Update(On.Watcher.WarpPoint.orig_Update orig, Watcher.WarpPoint self, bool eu)
        {
            if (isStoryMode(out var storyGameMode))
            {
                bool readyForWarp = storyGameMode.readyForTransition != StoryGameMode.ReadyForTransition.Closed;
                if (OnlineManager.lobby.isOwner && OnlineManager.lobby.clientSettings.Values.Where(cs => cs.inGame) is var inGameClients && inGameClients.Any())
                {
                    var inGameClientsData = inGameClients.Select(cs => cs.GetData<StoryClientSettingsData>());
                    var inGameAvatarOPOs = inGameClients.SelectMany(cs => cs.avatars.Select(id => id.FindEntity(true))).OfType<OnlinePhysicalObject>();
                    var rooms = inGameAvatarOPOs.Select(opo => opo.apo.pos.room);
                    var wasOneWay = (self.overrideData != null) ? self.overrideData.wasOneWay : self.Data.wasOneWay;
                    // Can't warp to warp points with null rooms (echo warps)
                    // remember that echo warps are one way only, so we will NOT gate thru them
                    // so please do not pretend it's a gate, and no requirements can be met, thanks :)
                    // and ensure theyre in the same room as the warp point itself :)
                    if (rooms.Distinct().Count() == 1 && !wasOneWay && self.room != null && inGameAvatarOPOs.First().apo.Room == self.room.abstractRoom)
                    { // make sure they're at the same room
                        RainWorld.roomIndexToName.TryGetValue(rooms.First(), out var gateRoom);
                        RainMeadow.Debug($"ready for warp {gateRoom}!");
                        storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.MeetRequirement;
                        readyForWarp = true;
                    }
                    else
                    {
                        storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.Closed;
                        storyGameMode.changedRegions = false;
                        readyForWarp = false;
                    }
                }
                if (!OnlineManager.lobby.isOwner || !readyForWarp)
                { // clients cant activate or update warp points unless it is an echo
                    self.triggerTime = 0;
                    self.lastTriggerTime = 0;
                }
            }
            orig(self, eu); // either host or singleplayer
        }

        public void Watcher_Barnacle_LoseShell(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                ILLabel spawnRocks = null;
                c.GotoNext(moveType: MoveType.Before, //code that can be run by client safely
                    i => i.MatchLdarg(0),
                    i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                    i => i.MatchBrtrue(out spawnRocks),
                    i => i.MatchRet()
                );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((Watcher.Barnacle self) => OnlineManager.lobby == null || (self.abstractCreature is AbstractPhysicalObject apo && apo.GetOnlineObject(out var opo) && opo.isMine));
                c.Emit(OpCodes.Brtrue, spawnRocks); //only may the object owner (or singleplayer) add rocks for a barnacle
                c.Emit(OpCodes.Ret);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        public void SpinningTop_SpawnWarpPoint(On.Watcher.SpinningTop.orig_SpawnWarpPoint orig, Watcher.SpinningTop self)
        {
            if (OnlineManager.lobby != null)
            {
                RainMeadow.Debug("spawning warp point from echo");
                PlacedObject placedObject = new(PlacedObject.Type.WarpPoint, null);
                SpinningTopData specialData = self.SpecialData;
                // setup data
                if (specialData != null && placedObject.data is Watcher.WarpPoint.WarpPointData warpData)
                {
                    warpData.destPos = specialData.destPos;
                    warpData.RegionString = specialData.RegionString;
                    warpData.destRoom = specialData.destRoom;
                    warpData.destTimeline = specialData.destTimeline;
                    warpData.panelPos = specialData.panelPos;
                    warpData.deathPersistentWarpPoint = true;
                    warpData.rippleWarp = specialData.rippleWarp;
                    // TODO: All warps by echos are one-way for now this is because we can't reliably obtain the
                    // "source room" of those (see Overworld_WorldLoaded); this leads to warps that warp to the
                    // same region and room which WILL cause issues - so to remediate that we simply pretend all
                    // warps made by echoes are one way only.
                    warpData.oneWay = true; //(specialData.rippleWarp || Region.IsWatcherVanillaRegion(self.room.world.name) || Region.IsVanillaSentientRotRegion(self.room.world.name));
                    if (self.room.game.IsStorySession)
                    {
                        warpData.cycleSpawnedOn = self.room.game.GetStorySession.saveState.cycleNumber;
                    }
                    warpData.destCam = Watcher.WarpPoint.GetDestCam(warpData);
                    placedObject.data = warpData;
                    placedObject.pos = self.pos;
                    Watcher.WarpPoint warpPoint = self.room.TrySpawnWarpPoint(placedObject, false, true, true);
                    if (warpPoint != null)
                    {
                        if (self.CanRaiseRippleLevel() || self.vanillaToRippleEncounter)
                        {
                            self.room.game.GetStorySession.spinningTopWarpsLeadingToRippleScreen.Add(warpPoint.MyIdentifyingString());
                        }
                        if (self.vanillaToRippleEncounter)
                        {
                            (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.minimumRippleLevel = 1f;
                            (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.maximumRippleLevel = 1f;
                            (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.rippleLevel = 1f;
                        }
                        warpPoint.WarpPrecast(); // force cast NOW
                        if (OnlineManager.lobby.isOwner)
                        {
                            StoryRPCs.EchoExecuteWatcherRiftWarp(null, self.room.abstractRoom.name, warpData.ToString()); // maybe just call orig here instead
                        }
                        else
                        {
                            OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.EchoExecuteWatcherRiftWarp, self.room.abstractRoom.name, warpData.ToString()); //tell owner to perform rift for everyone, only IF its an echo
                        }
                        if (!specialData.rippleWarp)
                        {
                            warpPoint.triggerTime = (int)(warpPoint.triggerActivationTime - 1f);
                            warpPoint.strongPull = true;
                        }
                    }
                    else
                    {
                        RainMeadow.Error("could not spawn a warp point for echo");
                    }
                }
                else
                {
                    RainMeadow.Error("echo does not have special data");
                }
            }
            else
            {
                orig(self);
            }
        }

        public void SpinningTop_Update(On.Watcher.SpinningTop.orig_Update orig, Watcher.SpinningTop self, bool eu)
        {
            orig(self, eu);
            if (OnlineManager.lobby != null)
            {
                var maximumRippleLevel = self.room.game.GetStorySession.saveState.deathPersistentSaveData.maximumRippleLevel;
                int num = 3;
                if (maximumRippleLevel != 0f)
                {
                    if (maximumRippleLevel != 0.25f)
                    {
                        if (maximumRippleLevel != 0.5f)
                        { num = -1; }
                        else
                        { num = 3; }
                    }
                    else
                    { num = 2; }
                }
                else
                {
                    num = 1;
                }
                self.vanillaEncounterNumber = num;
            }
        }

        // Static method, fortunely, means we dont have to worry about keeping track of a spinning top (echo)
        public void SpinningTop_RaiseRippleLevel(On.Watcher.SpinningTop.orig_RaiseRippleLevel orig, Room room)
        {
            orig(room);
            if (RainMeadow.isStoryMode(out var story))
            {
                var vector = new UnityEngine.Vector2(
                    room.game.GetStorySession.saveState.deathPersistentSaveData.minimumRippleLevel,
                    room.game.GetStorySession.saveState.deathPersistentSaveData.maximumRippleLevel
                );
                if (OnlineManager.lobby.isOwner)
                {
                    story.rippleLevel = room.game.GetStorySession.saveState.deathPersistentSaveData.rippleLevel;
                }
                if (!OnlineManager.lobby.isOwner && story.rippleLevel < vector.y)
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.RaiseRippleLevel, vector); // host needs notification that we get new rippleLevel
                }
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeOnceRPC(StoryRPCs.PlayRaiseRippleLevelAnimation, vector);
                    }
                }
            }
        }

        public void WarpPoint_PerformWarp(On.Watcher.WarpPoint.orig_PerformWarp orig, Watcher.WarpPoint self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                orig(self);
                World world = self.room.game.overWorld.worldLoader.ReturnWorld();
                var ws = world.GetResource() ?? throw new KeyNotFoundException();
                ws.Deactivate();
                ws.NotNeeded();
            }
            else
            {
                orig(self);
            }
        }

        public void Watcher_PrinceBehavior_InitateConversation(On.Watcher.PrinceBehavior.orig_InitateConversation orig, Watcher.PrinceBehavior self)
        {
            orig(self);
            if (OnlineManager.lobby != null)
            {
                int newValue = self.prince.room.game.GetStorySession.saveState.miscWorldSaveData.highestPrinceConversationSeen;
                RainMeadow.Debug("prince yap acknowledged");
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe)
                        {
                            player.InvokeOnceRPC(StoryRPCs.PrinceSetHighestConversation, newValue);
                        }
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                { // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.PrinceSetHighestConversation, newValue);
                }
                StoryRPCs.PrinceSetHighestConversation(null, newValue);
            }
        }

        private void Player_ctor_SynchronizeFoodBarForActualPlayers(On.Player.orig_ctor orig, Player self, AbstractCreature creature, World world)
        {
            orig(self, creature, world);
            if (isStoryMode(out var storyGameMode) && !self.isNPC)
            {
                IntVector2 intVector = SlugcatStats.SlugcatFoodMeter(storyGameMode.currentCampaign);
                self.slugcatStats.maxFood = intVector.x;
                if (self.abstractCreature.world.game.GetStorySession.saveState.malnourished) {
                    self.slugcatStats.foodToHibernate = intVector.x;
                } else {
                    self.slugcatStats.foodToHibernate = intVector.y; 
                }
                
            }
        }

        private void PauseMenu_SpawnExitContinueButtons(On.Menu.PauseMenu.orig_SpawnExitContinueButtons orig, Menu.PauseMenu self)
        {
            orig(self);
            if (isStoryMode(out var story))
            {
                if (OnlineManager.lobby.isOwner)
                {
                    var restartButton = new SimplerButton(self, self.pages[0], self.Translate("RESTART"), new Vector2(self.exitButton.pos.x - (self.continueButton.pos.x - self.exitButton.pos.x) - self.moveLeft - self.manager.rainWorld.options.SafeScreenOffset.x, Mathf.Max(self.manager.rainWorld.options.SafeScreenOffset.y, 15f)), new Vector2(110f, 30f));
                    restartButton.OnClick += (_) =>
                    {
                        self.game.GoToDeathScreen();
                    };
                    self.pages[0].subObjects.Add(restartButton);
                }
                else
                {
                    self.pauseWarningActive = false;
                }
            }
        }

        private void SlugcatSelectMenu_UpdateSelectedSlugcatInMiscProg(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                //patch going next page transfers your prev page "custom color on?"
                c.GotoNext(MoveType.After, x => x.MatchLdfld<ExtEnumBase>(nameof(ExtEnumBase.value)));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((string value, Menu.SlugcatSelectMenu self) =>
                {
                    return self is StoryOnlineMenu sOM ? sOM.PlayerSelectedSlugcat.value : value;
                });
                c.GotoNext(MoveType.After, x => x.MatchLdfld<ExtEnumBase>(nameof(ExtEnumBase.value)));
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((string value, Menu.SlugcatSelectMenu self) =>
                {
                    return self is StoryOnlineMenu sOM ? sOM.PlayerSelectedSlugcat.value : value;
                });
                /*c.GotoNext(MoveType.AfterLabel,
                    i => i.MatchLdsfld<ModManager>("MMF"),
                    i => i.MatchBrfalse(out _)
                );
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((bool flag, Menu.SlugcatSelectMenu self) => flag || (isStoryMode(out _) && self is StoryOnlineMenu)); dont overload remix flag as makes slider ids null*/

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
        private bool SlugcatSelectMenu_GetChecked(On.Menu.SlugcatSelectMenu.orig_GetChecked orig, Menu.SlugcatSelectMenu self, Menu.CheckBox box)
        {
            if (isStoryMode(out StoryGameMode storyGameMode) && self is StoryOnlineMenu)
            {
                if (box.IDString == "CLIENTSAVERESET")
                {
                    return storyGameMode.saveToDisk;
                }
                if (box.IDString == "ONLINEFRIENDLYFIRE") // online dictionaries do not like updating over the wire and I dont have the energy to deal with that right now
                {
                    return storyGameMode.friendlyFire;
                }
                if (box.IDString == "CAMPAIGNSLUGONLY")
                {
                    return storyGameMode.requireCampaignSlugcat;
                }
            }
            return orig(self, box);
        }
        private void SlugcatSelectMenu_SetChecked(On.Menu.SlugcatSelectMenu.orig_SetChecked orig, Menu.SlugcatSelectMenu self, Menu.CheckBox box, bool c)
        {
            if (isStoryMode(out StoryGameMode storyGameMode) && self is StoryOnlineMenu)
            {
                if (box.IDString == "CLIENTSAVERESET")
                {
                    storyGameMode.saveToDisk = c;
                    return;
                }
                if (box.IDString == "ONLINEFRIENDLYFIRE") // online dictionaries do not like updating over the wire and I dont have the energy to deal with that right now
                {
                    if (ModManager.JollyCoop) self.manager.rainWorld.options.friendlyFire = c;
                    storyGameMode.friendlyFire = c;
                    return;
                }
                if (box.IDString == "CAMPAIGNSLUGONLY")
                {
                    storyGameMode.requireCampaignSlugcat = c;
                    return;
                }
            }
            orig(self, box, c);
        }

        private void IL_SlugcatSelectMenu_SetChecked(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                /*cursor.GotoNext(MoveType.After, x => x.MatchCall<Menu.SlugcatSelectMenu>("colorFromIndex"));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((SlugcatStats.Name slugcat, Menu.SlugcatSelectMenu self) =>
                {
                    return self is StoryOnlineMenu sOM ? sOM.CurrentSlugcat : slugcat;
                }); not required now i suppose*/
                cursor.GotoNext(MoveType.After, x => x.MatchLdfld<ExtEnumBase>(nameof(ExtEnumBase.value)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((string value, Menu.SlugcatSelectMenu self) =>
                {
                    return self is StoryOnlineMenu sOM ? sOM.PlayerSelectedSlugcat.value : value;
                });
                cursor.GotoNext(MoveType.After, x => x.MatchLdfld<ExtEnumBase>(nameof(ExtEnumBase.value)));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((string value, Menu.SlugcatSelectMenu self) =>
                {
                    return self is StoryOnlineMenu sOM ? sOM.PlayerSelectedSlugcat.value : value;
                });

            }
            catch (Exception ex)
            {
                RainMeadow.Error(ex);
            }
        }

        private void IL_SlugcatSelectMenu_SingalFix(ILContext il)
        {
            try
            {
                /*SlugcatStats.Name name = this.slugcatColorOrder[this.slugcatPageIndex]; <- patch this
		  int index = this.activeColorChooser;
		 this.manager.rainWorld.progression.miscProgressionData.colorChoices[name.value][index] = this.colorInterface.defaultColors[this.activeColorChooser];*/

                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After, x => x.MatchStloc(0));
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloca, 0);
                cursor.EmitDelegate((Menu.SlugcatSelectMenu ssM, ref SlugcatStats.Name name) =>
                {
                    name = ssM is StoryOnlineMenu ? ssM.colorInterface.slugcatID : name;
                });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
            }
        }

        private void SlugcatSelectMenu_SliderFix(ILContext context)
        {
            /*
            0	0000	ldarg.0
            1	0001	ldfld	class [mscorlib]System.Collections.Generic.List`1<class SlugcatStats/Name> Menu.SlugcatSelectMenu::slugcatColorOrder
            2	0006	ldarg.0
            3	0007	ldfld	int32 Menu.SlugcatSelectMenu::slugcatPageIndex
            4	000C	callvirt	instance !0 class [mscorlib]System.Collections.Generic.List`1<class SlugcatStats/Name>::get_Item(int32)
            5	0011	stloc.0
            */
            // SlugcatStats.Name name = this.slugcatColorOrder[this.slugcatPageIndex]; becomes 
            try
            {
                ILCursor c = new(context);
                c.GotoNext(MoveType.After, x => x.MatchStloc(0));
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, 0);
                c.EmitDelegate((Menu.SlugcatSelectMenu menu, ref SlugcatStats.Name name) =>
                {
                    name = menu is StoryOnlineMenu storyOnlineMenu ? storyOnlineMenu.PlayerSelectedSlugcat : name;

                });

            }
            catch (Exception except)
            {
                Logger.LogError(except);
            }


        }

        private void SlugcatSelectMenu_SliderSetValue(On.Menu.SlugcatSelectMenu.orig_SliderSetValue orig, Menu.SlugcatSelectMenu self, Menu.Slider slider, float f)
        {
            orig(self, slider, f);
            if (isStoryMode(out _) && self.colorInterface is not null)
            {
                RainMeadow.rainMeadowOptions.BodyColor.Value = self.colorInterface.bodyColors.GetValueOrDefault(0)?.color ?? rainMeadowOptions.BodyColor.Value; //safe check just in case!
                RainMeadow.rainMeadowOptions.EyeColor.Value = self.colorInterface.bodyColors.GetValueOrDefault(1)?.color ?? rainMeadowOptions.EyeColor.Value;
            }
        }

        private void TextPrompt_UpdateGameOverString(On.HUD.TextPrompt.orig_UpdateGameOverString orig, TextPrompt self, Options.ControlSetup.Preset controllerType)
        {
            if (isStoryMode(out _))
            {
                self.gameOverString = Utils.Translate("Wait for others to shelter or rescue you, press ") + (RainMeadow.rainMeadowOptions.SpectatorKey.Value) + Utils.Translate(" to spectate, or press PAUSE BUTTON to dismiss message");
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
                if (isStoryMode(out _))
                {
                    self.restartNotAllowed = 1; // block from GoToDeathScreen

                    bool touchedInput = false;
                    for (int j = 0; j < self.hud.rainWorld.options.controls.Length; j++)
                    {
                        touchedInput = (self.hud.rainWorld.options.controls[j].gamePad || !self.defaultMapControls[j]) ? (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(5) || RWInput.CheckPauseButton(0, inMenu: false)) : (touchedInput || self.hud.rainWorld.options.controls[j].GetButton(11));
                    }
                    if (touchedInput || inVoidSea)
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

        // Doesn't consider slugNPC
        // private RWCustom.IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        // {
        //     if (isStoryMode(out var storyGameMode))
        //     {
        //         return orig(storyGameMode.currentCampaign);
        //     }
        //     return orig(slugcat);
        // }

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
                return self.playerNumber == 0 && OnlineManager.lobby.isOwner;
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

        private void Player_ctor_NonHunterCampaignClientDisableRedsIllness(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                        i => i.MatchLdfld<SaveState>("redExtraCycles"),
                        i => i.MatchCall<RedsIllness>("RedsCycles"),
                        i => i.MatchBlt(out _),
                        i => i.MatchLdsfld<ModManager>("CoopAvailable")
                        );
                c.EmitDelegate((bool coopAvailable) => coopAvailable || OnlineManager.lobby != null);
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

                if (MatchmakingManager.currentInstance.canSendChatMessages)
                    self.AddPart(new ChatHud(self, cam));
            }
        }
        private void FoodMeter_TrySpawnPupBars_LobbyClient(On.HUD.FoodMeter.orig_TrySpawnPupBars orig, FoodMeter self)
        {
            if (OnlineManager.lobby != null && isStoryMode(out var story))
            {
                if (!OnlineManager.lobby.isOwner)
                {

                    // base game checks copied over
                    if (ModManager.MSC &&
                        !self.IsPupFoodMeter && self.pupBars == null &&
                        (self.hud.owner as Player).room != null &&
                        (self.hud.owner as Player).room.game.spawnedPendingObjects)
                    {
                        self.pupBars = new();
                        int num = 1;
                        if (story.pups is not null)
                            foreach (AbstractCreature pup in story.pups)
                            {
                                if (self.pupBars.FirstOrDefault(x => x.abstractPup == pup) is not null)
                                {
                                    continue;
                                }

                                if (pup.realizedCreature is Player NPC)
                                {
                                    FoodMeter foodMeter = new FoodMeter(self.hud, 0, 0, NPC, num);
                                    foodMeter.abstractPup = pup;
                                    self.hud.AddPart(foodMeter);
                                    self.pupBars.Add(foodMeter);
                                    num++;
                                }
                                else RainMeadow.Error("Pup wasn't a realized player");
                            }
                    }
                    return;
                }
            }

            orig(self);
        }

        private void FoodMeter_TrySpawnPupBars_LobbyOwner(ILContext context)
        {
            try
            {
                ILCursor cursor = new(context);
                cursor.EmitDelegate(() =>
                {
                    if (OnlineManager.lobby != null && isStoryMode(out var story))
                    {
                        if (!OnlineManager.lobby.isOwner) return;
                        story.pups.Clear();
                    }
                });
                cursor.GotoNext(x => x.MatchNewobj<FoodMeter>());
                cursor.GotoPrev(MoveType.After, x => x.OpCode == OpCodes.Brfalse_S || x.OpCode == OpCodes.Brfalse);

                // Add pups to gamemode list
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.EmitDelegate((FoodMeter self, int i) =>
                {
                    if (OnlineManager.lobby != null && isStoryMode(out var story))
                    {
                        if (!OnlineManager.lobby.isOwner) return;

                        var pup = (self.hud.owner as Player)?.abstractCreature?.Room?.creatures[i];
                        if (pup != null) story.pups.Add(pup);
                    }
                });

            }
            catch (Exception except)
            {
                Logger.LogError(except);
            }
        }

        private void RainWorldGame_Win(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
        {
            if (isStoryMode(out var storyGameMode) && OnlineManager.lobby != null)
            {
                // ok but remember the watcher hates good portals so we have to clean stuff up
                self.GetStorySession.pendingWarpPointTransferObjects.Clear();
                self.GetStorySession.importantWarpPointTransferedEntities.Clear();
                self.GetStorySession.saveState.importantTransferEntitiesAfterWarpPointSave.Clear();
                string? lastWarpPoint = storyGameMode.myLastWarp?.ToString();
                if (OnlineManager.lobby.isOwner)
                {
                    foreach (var player in OnlineManager.players)
                    {
                        if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToWinScreen, malnourished, fromWarpPoint, storyGameMode.myLastDenPos, lastWarpPoint);
                    }
                }
                else if (RPCEvent.currentRPCEvent is null)
                {
                    // tell host to move everyone else
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToWinScreen, malnourished, fromWarpPoint, storyGameMode.myLastDenPos, lastWarpPoint);
                    return;
                }
                if (fromWarpPoint)
                { // reset gate status
                    storyGameMode.changedRegions = false;
                    storyGameMode.readyForTransition = StoryGameMode.ReadyForTransition.Closed;
                }
            }
            orig(self, malnourished, fromWarpPoint);
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
                    //OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.GoToStarveScreen, storyGameMode.myLastDenPos);
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

        private void SleepAndDeathScreen_AddPassageButton(On.Menu.SleepAndDeathScreen.orig_AddPassageButton orig, Menu.SleepAndDeathScreen self, bool buttonBlack)
        {
            if (isStoryMode(out _) && !OnlineManager.lobby.isOwner) return;
            orig(self, buttonBlack);
        }

        private void CustomEndGameScreen_GetDataFromSleepScreen(On.Menu.CustomEndGameScreen.orig_GetDataFromSleepScreen orig, Menu.CustomEndGameScreen self, WinState.EndgameID endGameID)
        {
            if (isStoryMode(out _) && OnlineManager.lobby.isOwner)
            {
                foreach (var player in OnlineManager.players)
                {
                    if (!player.isMe) player.InvokeOnceRPC(StoryRPCs.GoToPassageScreen, endGameID);
                }
            }

            orig(self, endGameID);
        }

        private void FastTravelScreen_ctor(On.Menu.FastTravelScreen.orig_ctor orig, Menu.FastTravelScreen self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            orig(self, manager, ID);
            if (isStoryMode(out _) && !OnlineManager.lobby.isOwner)
            {
                StoryMenuHelpers.RemoveMenuObjects(self.mapButtonPrompt, self.prevButton, self.nextButton, self.chooseButton, self.buttonInstruction);
            }
        }

        private void FastTravelScreen_ctor_ClientDontFilterRegions(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(MoveType.AfterLabel,
                    i => i.MatchStloc(0)
                );
                c.EmitDelegate((bool flag) =>
                {
                    if (isStoryMode(out _) && !OnlineManager.lobby.isOwner) return true;
                    return flag;
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void FastTravelScreen_StepRegion(On.Menu.FastTravelScreen.orig_StepRegion orig, Menu.FastTravelScreen self, int change)
        {
            if (isStoryMode(out _) && !OnlineManager.lobby.isOwner) return;
            orig(self, change);
        }

        private int RegionAcronymToIndexForFastTravelScreen(Menu.FastTravelScreen self, string acronym) => self.accessibleRegions.IndexOf(Array.FindIndex(self.allRegions, x => x.name == acronym));

        private void FastTravelScreen_InitiateRegionSwitch(On.Menu.FastTravelScreen.orig_InitiateRegionSwitch orig, Menu.FastTravelScreen self, int switchToRegion)
        {
            if (isStoryMode(out var storyGameMode) && !OnlineManager.lobby.isOwner)
            {
                if (storyGameMode.region is not (null or ""))
                    switchToRegion = RegionAcronymToIndexForFastTravelScreen(self, storyGameMode.region);
                if (switchToRegion < 0) switchToRegion = 0;
            }
            orig(self, switchToRegion);
        }

        private void FastTravelScreen_Update(On.Menu.FastTravelScreen.orig_Update orig, Menu.FastTravelScreen self)
        {
            if (isStoryMode(out var storyGameMode) && !OnlineManager.lobby.isOwner && storyGameMode.region is not (null or ""))
            {
                var hostCurrentRegion = RegionAcronymToIndexForFastTravelScreen(self, storyGameMode.region);
                if (hostCurrentRegion != -1 && hostCurrentRegion != self.currentRegion && hostCurrentRegion != self.upcomingRegion)
                    self.InitiateRegionSwitch(hostCurrentRegion);
            }
            orig(self);
            if (storyGameMode is not null)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.region = self.allRegions[self.accessibleRegions[self.currentRegion]].name;
                }
                else if (self.startButton is not null)
                {
                    self.startButton.buttonBehav.greyedOut = !storyGameMode.canJoinGame;
                }
            }
        }

        private void FastTravelScreen_Singal_ClientLoadGameNormally(On.Menu.FastTravelScreen.orig_Singal orig, Menu.FastTravelScreen self, Menu.MenuObject sender, string message)
        {
            if (isStoryMode(out _) && !OnlineManager.lobby.isOwner)
            {
                if (message == "HOLD TO START")
                {
                    self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                    self.PlaySound(SoundID.MENU_Continue_Game);
                    return;
                }
            }

            orig(self, sender, message);
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

        private static List<Func<string, string>> saveStateStringFilter = new();
        public static event Func<string, string> SaveStateStringFilter
        {
            add => saveStateStringFilter.Add(value);
            remove => saveStateStringFilter.Remove(value);
        }

        private static string? SaveStateToString(SaveState? saveState)
        {
            if (saveState is null) return null;

            try
            {
                // HACK: sub in empty objectTrackers to fix SaveState.SaveToString nullref at IL_0767
                var objectTrackers = saveState.objectTrackers;
                saveState.objectTrackers = new();
                var s = saveState.SaveToString();
                saveState.objectTrackers = objectTrackers;

                RainMeadow.Debug($"origSaveState[{s.Length}]:{s}");
                if (saveStateStringFilter.Count > 0)
                {
                    foreach (var del in saveStateStringFilter) s = del(s);
                    RainMeadow.Debug($"filtSaveState[{s.Length}]:{s}");
                }
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
                inVoidSea = false;
                currentSaveState.progression ??= self;
                if (OnlineManager.lobby.isOwner)
                {
                    storyGameMode.saveStateString = SaveStateToString(currentSaveState);
                }
                else
                {
                    // this already syncs the denpos
                    currentSaveState.LoadGame(InflateJoarXML(storyGameMode.saveStateString ?? ""), game);
                }

                RainMeadow.Debug($"START DENPOS save:{currentSaveState.denPosition} last:{storyGameMode.myLastDenPos} lobby:{storyGameMode.defaultDenPos}");
                RainMeadow.Debug($"START WARPPOS save:{currentSaveState.warpPointTargetAfterWarpPointSave} last:{storyGameMode.myLastWarp}");

                if (OnlineManager.lobby.isOwner || storyGameMode.myLastDenPos is null || currentSaveState.denPosition != storyGameMode.defaultDenPos)
                {
                    storyGameMode.myLastDenPos = currentSaveState.denPosition;
                }
                else
                {
                    currentSaveState.denPosition = storyGameMode.myLastDenPos;
                }
                if (OnlineManager.lobby.isOwner || storyGameMode.myLastWarp is null || currentSaveState.warpPointTargetAfterWarpPointSave != storyGameMode.myLastWarp)
                {
                    storyGameMode.myLastWarp = currentSaveState.warpPointTargetAfterWarpPointSave;
                }
                else
                {
                    currentSaveState.warpPointTargetAfterWarpPointSave = storyGameMode.myLastWarp;
                }

                RainMeadow.Debug($"FINAL DENPOS save:{currentSaveState.denPosition}");
                RainMeadow.Debug($"FINAL WARPPOS save:{currentSaveState.warpPointTargetAfterWarpPointSave}");
                if (OnlineManager.lobby.isOwner && storyGameMode.currentCampaign == Watcher.WatcherEnums.SlugcatStatsName.Watcher && currentSaveState.deathPersistentSaveData != null)
                {
                    storyGameMode.rippleLevel = currentSaveState.deathPersistentSaveData.rippleLevel;
                }
            }

            return currentSaveState;
        }

        private bool PlayerProgression_SaveToDisk(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self, bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (isStoryMode(out var storyGameMode) && !storyGameMode.saveToDisk) return false;
            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        private void SaveState_SessionEnded(On.SaveState.orig_SessionEnded orig, SaveState self, RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (storyGameMode.myLastDenPos is not (null or ""))
                {
                    self.denPosition = storyGameMode.myLastDenPos;
                    if (OnlineManager.lobby.isOwner) storyGameMode.defaultDenPos = storyGameMode.myLastDenPos;
                }
                if (storyGameMode.myLastWarp is not null)
                {
                    self.warpPointTargetAfterWarpPointSave = storyGameMode.myLastWarp; // I think we should clear this on session ended to reset so incoming players arent' dropped at a portal
                }
            }
            orig(self, game, survived, newMalnourished);
        }

        private void SaveState_SessionEnded_DontAssumePlayerRealized(ILContext il)
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

        private string SaveState_GetSaveStateDenToUse(On.SaveState.orig_GetSaveStateDenToUse orig, SaveState self)
        {
            // Savefile logic is not cooperative (with our code); this is a way to ensure that PLEASE
            // you WARP us into the room we were meant to be warped into, instead of some random room
            // or wose, a "room out of bounds" (i.e index > region total index)
            if (OnlineManager.lobby != null && self.warpPointTargetAfterWarpPointSave != null)
            {
                string destRoom = self.warpPointTargetAfterWarpPointSave.destRoom;
                RainMeadow.Debug($"hijacking denpos to be {destRoom}");
                return destRoom;
            }
            return orig(self);
        }

        private void SaveState_BringUpToDate(On.SaveState.orig_BringUpToDate orig, SaveState self, RainWorldGame game)
        {
            if (isStoryMode(out _) && !game.manager.menuSetup.FastTravelInitCondition)
            {
                try
                {
                    game.FirstAlivePlayer.pos = new WorldCoordinate(game.world.GetAbstractRoom(self.denPosition).index, -1, -1, -1);
                }
                catch (System.NullReferenceException e) // happens in riv ending
                {
                    RainMeadow.Debug("NOTE: rivulet hackfix null ref exception is bad!");
                }
            }
            orig(self, game);
        }

        private void KarmaLadderScreen_Singal(On.Menu.KarmaLadderScreen.orig_Singal orig, Menu.KarmaLadderScreen self, Menu.MenuObject sender, string message)
        {
            if (isStoryMode(out _))
            {
                if (message == "READY")
                {
                    sender.toggled ^= true;
                    return;
                }

                if (message == "CONTINUE")
                {
                    if (OnlineManager.lobby.isOwner)
                    {
                        RainMeadow.Debug("Continue - host");
                    }
                    else
                    {
                        RainMeadow.Debug("Continue - client");
                    }
                }
            }
            orig(self, sender, message);
        }

        // This is nescesary because sometimes ripple levels are not properly synched
        // we should probably synch them -- but at the moment this helps avoid black screens of death
        private string HUD_KarmaMeter_RippleSymbolSprite(On.HUD.KarmaMeter.orig_RippleSymbolSprite orig, bool small, float rippleLevel)
        {
            if (OnlineManager.lobby != null)
            {
                double num = Math.Round((double)(rippleLevel * 2f), MidpointRounding.AwayFromZero) / 2.0;
                num = Math.Max(num, 1.0);
                return (small ? "smallRipple" : "ripple") + num.ToString("#.0", System.Globalization.CultureInfo.InvariantCulture);
            }
            else
            {
                return orig(small, rippleLevel);
            }
        }

        private void KarmaLadderScreen_Update(On.Menu.KarmaLadderScreen.orig_Update orig, Menu.KarmaLadderScreen self)
        {
            orig(self);

            if (isStoryMode(out var storyGameMode) && self.continueButton != null)
            {
                if (OnlineManager.lobby.isOwner)
                {
                    self.continueButton.buttonBehav.greyedOut = OnlineManager.lobby.clientSettings.Values.Any(cs => cs.inGame);
                }
                else if (storyGameMode.canJoinGame || self.ID == MoreSlugcats.MoreSlugcatsEnums.ProcessID.KarmaToMinScreen)  // arti's ending continues into slideshow
                {
                    self.continueButton.signalText = "CONTINUE";
                    self.continueButton.menuLabel.text = self.Translate("CONTINUE");
                    if (self.continueButton.toggled) self.Singal(self.continueButton, "CONTINUE");
                }
                else
                {
                    self.continueButton.signalText = "READY";
                    self.continueButton.menuLabel.text = self.Translate("READY");
                }
            }
        }

        private void RegionGate_Update(ILContext il)
        {
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
                        if (story.readyForTransition >= StoryGameMode.ReadyForTransition.Opening) return true;
                        story.storyClientData.readyForTransition = false;
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
                        story.storyClientData.readyForTransition = true;
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
                    if (ac.realizedCreature is Player p)
                    {
                        if (p.touchedNoInputCounter < 20) return false;
                        if ((p.slugOnBack != null) && p.slugOnBack.HasASlug) return false;
                    }
                }
            }
            return orig(self);
        }

        private bool RegionGate_AllPlayersThroughToOtherSide(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
        {
            var ret = orig(self);
            if (isStoryMode(out var storyGameMode))
            {
                storyGameMode.storyClientData.readyForTransition = !ret;
                ret = storyGameMode.readyForTransition == StoryGameMode.ReadyForTransition.Closed;
            }
            return ret;
        }

        public bool RegionGate_MeetRequirement_StorySync(orig_RegionGateBool orig, RegionGate self)
        {
            if (isStoryMode(out var storyGameMode))
            {
                // NOTE: Sometimes, just sometimes, an echo may raise watcher to >= 1 ripple level
                // before metting the third echo; thus soft locking the game, this is a prevention
                // for said case, it does not happen often, but it does happen.
                var ret = (self.room.game.Players[0].realizedCreature is Player player && player.maxRippleLevel >= 1f) || orig(self);
                if (ret) StoryRPCs.RegionGateOrWarpMeetRequirement();
                return storyGameMode.readyForTransition >= StoryGameMode.ReadyForTransition.MeetRequirement;
            }
            return orig(self);
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

        private void PlayerGhosts_AddGhost(On.VoidSea.PlayerGhosts.orig_AddGhost orig, VoidSea.PlayerGhosts self)
        {
            if (OnlineManager.lobby != null)
            {
                Vector2 vector = self.originalPlayer.mainBodyChunk.pos + Custom.RNV() * 2000f;
                AbstractCreature abstractCreature = new AbstractCreature(self.voidSea.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Slugcat), null, self.voidSea.room.GetWorldCoordinate(vector), new EntityID(-1, -1));
                abstractCreature.state = new PlayerState(abstractCreature, self.originalPlayer.playerState.playerNumber, self.originalPlayer.SlugCatClass, true);
                self.voidSea.room.abstractRoom.AddEntity(abstractCreature);
                abstractCreature.Realize();  // don't RealizeInRoom yet because that would call PlaceInRoom and ApplyPalette
                for (int i = 0; i < abstractCreature.realizedCreature.bodyChunks.Length; i++)
                {
                    abstractCreature.realizedCreature.bodyChunks[i].restrictInRoomRange = float.MaxValue;
                }
                abstractCreature.realizedCreature.CollideWithTerrain = false;

                self.ghosts.Add(new VoidSea.PlayerGhosts.Ghost(self, abstractCreature.realizedCreature as Player));

                if (RainMeadow.creatureCustomizations.TryGetValue(self.originalPlayer, out var customization))
                {
                    RainMeadow.creatureCustomizations.GetValue(self.ghosts.Last().creature, (c) => customization);
                }

                abstractCreature.RealizeInRoom();  // PlaceInRoom after applying our customization
            }
            else
            {
                orig(self);
            }
        }

        private void VoidSeaScene_Update(On.VoidSea.VoidSeaScene.orig_Update orig, VoidSea.VoidSeaScene self, bool eu)
        {
            orig(self, eu);
            if (isStoryMode(out _))
            {
                foreach (var playerAvatar in OnlineManager.lobby.playerAvatars.Select(kv => kv.Value))
                {
                    if (playerAvatar.type == (byte)OnlineEntity.EntityId.IdType.none) continue; // not in game
                    if (playerAvatar.FindEntity(true) is OnlinePhysicalObject opo && opo.apo is AbstractCreature ac)
                    {
                        // do things with the AbstractCreature we found
                        if (!ac.IsLocal() && opo.apo.realizedObject.Submersion > 0.5f)
                        {
                            Vector2 position = ac.realizedCreature.bodyChunks[0].pos;
                            RainMeadow.Debug("Removed onlinePlayer avatar on submersion at pos: " + position);
                            opo.apo.realizedObject.room.AddObject(new ShockWave(position, 300f, 0.2f, 15, false));
                            opo.apo.realizedObject.room.PlaySound(SoundID.MENU_Karma_Ladder_Hit_Upper_Cap, 0f, 3f, 1f);
                            opo.apo.realizedObject.RemoveFromRoom();
                        }
                        else if (ac.IsLocal() && opo.apo.realizedObject.Submersion > 0.5f)
                        {
                            inVoidSea = true;
                        }
                        else if (ac.IsLocal() && !(opo.apo.realizedObject.Submersion > 0.5f))
                        {
                            inVoidSea = false;
                        }
                    }
                }
            }
        }
    }
}
