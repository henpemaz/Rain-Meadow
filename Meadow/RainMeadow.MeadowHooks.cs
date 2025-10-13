﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private Texture2D nightsky;
        private Texture2D nightskyGlow;
        private void MeadowHooks()
        {
            _ = Ext_SoundID.RM_Slugcat_Call; //load

            byte[] array = File.ReadAllBytes(AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + "rm_nightsky_base.png"));
            this.nightsky = new Texture2D(512, 512, TextureFormat.RGBA32, false, false);
            nightsky.LoadImage(array);
            nightsky.wrapMode = TextureWrapMode.Repeat;
            nightsky.filterMode = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_RM_NightSky", nightsky);
            array = File.ReadAllBytes(AssetManager.ResolveFilePath("Illustrations" + Path.DirectorySeparatorChar.ToString() + "rm_nightsky_glow.png"));
            this.nightskyGlow = new Texture2D(512, 512, TextureFormat.RGBA32, false, false);
            nightskyGlow.LoadImage(array);
            nightskyGlow.wrapMode = TextureWrapMode.Repeat;
            nightskyGlow.filterMode = FilterMode.Bilinear;
            Shader.SetGlobalTexture("_RM_NightSky_glow", nightskyGlow);

            GroundCreatureController.Enable();
            CicadaController.EnableCicada();
            LizardController.EnableLizard();
            ScavengerController.EnableScavenger();
            NoodleController.EnableNoodle();
            EggbugController.EnableEggbug();
            MeadowPlayerController.Enable();
            LanternMouseController.EnableMouse();

            On.RoomCamera.Update += RoomCamera_Update; // init meadow hud

            IL.HUD.Map.GetSaveState += Map_GetSaveState; // support non-slug owner

            On.RegionGate.ctor += RegionGate_ctor;
            On.RegionGate.PlayersInZone += RegionGate_PlayersInZone1;
            On.RegionGate.PlayersStandingStill += RegionGate_PlayersStandingStill1;
            On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide1;

            On.RainWorldGame.AllowRainCounterToTick += RainWorldGame_AllowRainCounterToTick; // timer stuck
            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld; // timer stuck past cycle start

            IL.ShelterDoor.ctor += ShelterDoor_ctor; // don't you player[0] me, tutorial
            On.ShelterDoor.Close += ShelterDoor_Close; // door stuck

            On.AbstractCreature.ChangeRooms += AbstractCreature_ChangeRooms; // displayer follow creature

            On.Room.LoadFromDataString += Room_LoadFromDataString1; // places of spawning items

            On.Menu.FastTravelScreen.Singal += FastTravelScreen_Singal;

            // open gate
            new Hook(typeof(RegionGate).GetProperty("MeetRequirement").GetGetMethod(), this.RegionGate_MeetRequirement);
            new Hook(typeof(WaterGate).GetProperty("EnergyEnoughToOpen").GetGetMethod(), this.RegionGate_EnergyEnoughToOpen);
            new Hook(typeof(ElectricGate).GetProperty("EnergyEnoughToOpen").GetGetMethod(), this.RegionGate_EnergyEnoughToOpen);

            On.WormGrass.IsTileAccessible += WormGrass_IsTileAccessible; // always accessible
            On.WormGrass.WormGrassPatch.InteractWithCreature += WormGrassPatch_InteractWithCreature;
            IL.WormGrass.WormGrassPatch.InteractWithCreature += WormGrassPatch_InteractWithCreature1;
            On.WormGrass.Worm.ctor += Worm_ctor; // only cosmetic worms

            IL.ScavengerOutpost.ctor += ScavengerOutpost_ctor;

            On.ShortcutGraphics.GenerateSprites += ShortcutGraphics_GenerateSprites; // creature pipe indicators
            On.ShortcutGraphics.Draw += ShortcutGraphics_Draw;

            On.World.SpawnGhost += World_SpawnGhost;

            On.CreatureTemplate.CreatureRelationship_CreatureTemplate += CreatureTemplate_CreatureRelationship_CreatureTemplate;

            On.ShortcutHandler.ShortCutVessel.ctor += ShortCutVessel_ctor; // faster vessels
        }

        private void ShortCutVessel_ctor(On.ShortcutHandler.ShortCutVessel.orig_ctor orig, ShortcutHandler.ShortCutVessel self, RWCustom.IntVector2 pos, Creature creature, AbstractRoom room, int wait)
        {
            orig(self, pos, creature, room, wait);
            if (OnlineManager.lobby?.gameMode is MeadowGameMode && self.wait != 0) self.wait = Mathf.Min(100, Mathf.FloorToInt(self.wait * 0.4f + 5f));
        }

        private CreatureTemplate.Relationship CreatureTemplate_CreatureRelationship_CreatureTemplate(On.CreatureTemplate.orig_CreatureRelationship_CreatureTemplate orig, CreatureTemplate self, CreatureTemplate crit)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 2f); // intense ignore helps with looking
            }
            return orig(self, crit);
        }

        private void WormGrassPatch_InteractWithCreature1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel skip = null;
            c.GotoNext(MoveType.After,
                i => i.MatchLdfld<CreatureTemplate>("type"),
                i => i.MatchLdsfld<CreatureTemplate.Type>("Slugcat"),
                i => i.MatchCallOrCallvirt("ExtEnum`1<CreatureTemplate/Type>", "op_Inequality"),
                i => i.MatchBrfalse(out skip)
                );
            c.EmitDelegate(() =>
            {
                if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
                {
                    return false;
                }
                return true;
            });
            c.Emit(OpCodes.Brfalse, skip);
        }

        private void WormGrassPatch_InteractWithCreature(On.WormGrass.WormGrassPatch.orig_InteractWithCreature orig, WormGrass.WormGrassPatch self, WormGrass.WormGrassPatch.CreatureAndPull creatureAndPull)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                creatureAndPull.bury = 0f;
                creatureAndPull.pull = 0f;
            }
            orig(self, creatureAndPull);
        }

        private bool WormGrass_IsTileAccessible(On.WormGrass.orig_IsTileAccessible orig, WormGrass self, RWCustom.IntVector2 tile, CreatureTemplate crit)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return true;
            }
            return orig(self, tile, crit);
        }

        private void World_SpawnGhost(On.World.orig_SpawnGhost orig, World self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return; // no ghosts
            }
            orig(self);
        }

        private void ShortcutGraphics_Draw(On.ShortcutGraphics.orig_Draw orig, ShortcutGraphics self, float timeStacker, Vector2 camPos)
        {
            orig(self, timeStacker, camPos);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                if (ModManager.MSC) // get out of the way
                {
                    for (int k = 0; k < self.entranceSprites.GetLength(0); k++)
                    {
                        if (self.entranceSprites[k, 0] != null && self.room.shortcuts[k].shortCutType == ShortcutData.Type.NPCTransportation)
                        {
                            self.entranceSprites[k, 0].isVisible = true;
                        }
                    }
                }
            }
        }

        private void ShortcutGraphics_GenerateSprites(On.ShortcutGraphics.orig_GenerateSprites orig, ShortcutGraphics self)
        {
            orig(self);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                for (int l = 0; l < self.room.shortcuts.Length; l++)
                {
                    if (self.room.shortcuts[l].shortCutType == ShortcutData.Type.NPCTransportation)
                    {
                        self.entranceSprites[l, 0]?.RemoveFromContainer(); // remove safari one
                        self.entranceSprites[l, 0] = new FSprite("Pebble10", true);
                        self.entranceSprites[l, 0].rotation = RWCustom.Custom.AimFromOneVectorToAnother(new Vector2(0f, 0f), -RWCustom.IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[l].StartTile)));
                        self.entranceSpriteLocations[l] = self.room.MiddleOfTile(self.room.shortcuts[l].StartTile) + RWCustom.IntVector2.ToVector2(self.room.ShorcutEntranceHoleDirection(self.room.shortcuts[l].StartTile)) * 15f;
                        if ((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) || (self.room.water && self.room.waterInFrontOfTerrain && self.room.PointSubmerged(self.entranceSpriteLocations[l] + new Vector2(0f, 5f))))
                        {
                            self.camera.ReturnFContainer((ModManager.MMF && MoreSlugcats.MMF.cfgShowUnderwaterShortcuts.Value) ? "GrabShaders" : "Items").AddChild(self.entranceSprites[l, 0]);
                        }
                        else
                        {
                            self.camera.ReturnFContainer("Shortcuts").AddChild(self.entranceSprites[l, 0]);
                            self.camera.ReturnFContainer("Water").AddChild(self.entranceSprites[l, 1]);
                        }
                    }
                }
            }
        }

        private void ScavengerOutpost_ctor(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                ILLabel skip = null;
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(2),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchLdfld<AbstractRoom>("firstTimeRealized"),
                    i => i.MatchBrfalse(out skip)
                    );
                c.EmitDelegate(() => !(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode));
                c.Emit(OpCodes.Brfalse, skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void FastTravelScreen_Singal(On.Menu.FastTravelScreen.orig_Singal orig, Menu.FastTravelScreen self, Menu.MenuObject sender, string message)
        {
            if (OnlineManager.lobby?.gameMode is MeadowGameMode mgm)
            {
                if (message == "HOLD TO START")
                {
                    self.initiateCharacterFastTravel = true;
                    MeadowProgression.progressionData.currentCharacterProgress.saveLocation = new WorldCoordinate(self.selectedShelter, -1, -1, 0);
                }
                if (message == "BACK")
                {
                    self.manager.RequestMainProcessSwitch(RainMeadow.Ext_ProcessID.MeadowMenu);
                }
            }
            orig(self, sender, message);
        }

        private void Worm_ctor(On.WormGrass.Worm.orig_ctor orig, WormGrass.Worm self, WormGrass wormGrass, WormGrass.WormGrassPatch patch, Vector2 basePos, float reachHeight, float iFac, float lengthFac, bool cosmeticOnly, System.Random rnd)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                orig(self, wormGrass, patch, basePos, reachHeight, iFac, lengthFac, true, rnd);
                return;
            }

            orig(self, wormGrass, patch, basePos, reachHeight, iFac, lengthFac, cosmeticOnly, rnd);
        }

        public delegate bool orig_RegionGateBool(RegionGate self);
        public bool RegionGate_MeetRequirement(orig_RegionGateBool orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return true;
            }
            return orig(self);
        }

        public bool RegionGate_EnergyEnoughToOpen(orig_RegionGateBool orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return true;
            }
            return orig(self);
        }


        private void RegionGate_ctor(On.RegionGate.orig_ctor orig, RegionGate self, Room room)
        {
            orig(self, room);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                self.unlocked = true;
            }
        }

        private bool RegionGate_AllPlayersThroughToOtherSide1(On.RegionGate.orig_AllPlayersThroughToOtherSide orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (mgm.avatars[0].creature.pos.room == self.room.abstractRoom.index && (!self.letThroughDir || mgm.avatars[0].creature.pos.x < self.room.TileWidth / 2 + 3) && (self.letThroughDir || mgm.avatars[0].creature.pos.x > self.room.TileWidth / 2 - 4))
                {
                    return false;
                }
                return true;
            }
            return orig(self);
        }

        private bool RegionGate_PlayersStandingStill1(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (mgm.avatars[0].realizedCreature != null && CreatureController.creatureControllers.TryGetValue(mgm.avatars[0].realizedCreature, out var c))
                {
                    return c.touchedNoInputCounter > 20;
                }
            }
            return orig(self);
        }

        private int RegionGate_PlayersInZone1(On.RegionGate.orig_PlayersInZone orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (mgm.avatars[0].realizedCreature != null && CreatureController.creatureControllers.TryGetValue(mgm.avatars[0].realizedCreature, out var c))
                {
                    return self.DetectZone(c.creature.abstractCreature);
                }
            }
            return orig(self);
        }

        public static ConditionalWeakTable<Room, string> line5 = new();
        private void Room_LoadFromDataString1(On.Room.orig_LoadFromDataString orig, Room self, string[] lines)
        {
            orig(self, lines);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm && RoomSession.map.TryGetValue(self.abstractRoom, out var rs))
            {
                line5.Add(self, lines[5]);
            }
        }

        private void AbstractCreature_ChangeRooms(On.AbstractCreature.orig_ChangeRooms orig, AbstractCreature self, WorldCoordinate newCoord)
        {
            orig(self, newCoord);
            if (OnlineManager.lobby != null && OnlinePhysicalObject.map.TryGetValue(self, out var oe))
            {
                if (OnlineManager.lobby.gameMode is MeadowGameMode && self.realizedCreature is Creature c && EmoteDisplayer.map.TryGetValue(c, out var displayer))
                {
                    displayer.ChangeRooms(newCoord);
                }
            }
        }

        private void OverWorld_LoadFirstWorld(On.OverWorld.orig_LoadFirstWorld orig, OverWorld self)
        {
            orig(self);
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                self.activeWorld.rainCycle.timer = 800;
                MeadowMusic.NewWorld(self.activeWorld);
            }
        }

        private void ShelterDoor_ctor(ILContext il)
        {
            // tutorials (big if block at the end) shoulnd't happen in meadow-mode
            // most of them use player[0], no good
            // we add
            // if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode) return;
            // after the expedition check that does the same thing
            var c = new ILCursor(il);
            c.Index = c.Instrs.Count - 1; // from bottom
            ILLabel noExpedition = null;
            c.GotoPrev(MoveType.AfterLabel,
                i=>i.MatchLdsfld<ModManager>("Expedition"),
                i=>i.MatchBrfalse(out noExpedition)
                );
            c.GotoLabel(noExpedition); // code after return (expedition is off)
            var skip = c.DefineLabel();
            c.EmitDelegate(() => OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode);
            c.Emit(OpCodes.Brfalse, skip);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(skip);
        }

        private void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            orig(self);
        }

        private bool RainWorldGame_AllowRainCounterToTick(On.RainWorldGame.orig_AllowRainCounterToTick orig, RainWorldGame self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return false;
            }
            return orig(self);
        }


        private void Map_GetSaveState(ILContext il)
        {
            try
            {
                //( ... || this.hud.owner.GetOwnerType() == HUD.OwnerType.RegionOverview || [new condition])

                var c = new ILCursor(il);
                ILLabel dorun = null;
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdsfld<HUD.HUD.OwnerType>("RegionOverview"),
                    i => i.MatchCall(out _),
                    i => i.MatchBrtrue(out dorun)
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((HUD.Map map) => map.hud.owner.GetOwnerType() == CreatureController.controlledCreatureHudOwner);
                c.Emit(OpCodes.Brtrue, dorun);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode meadowGameMode)
            {
                if (self.hud == null && self.followAbstractCreature?.realizedObject is Creature owner)
                {
                    RainMeadow.Debug("followed creature is " + owner);
                    if (owner != meadowGameMode.avatars[0].realizedCreature) { RainMeadow.Error($"Camera owner != avatar {owner} {meadowGameMode.avatars[0]}"); }

                    self.hud = new HUD.HUD(new FContainer[]
                    {
                        self.ReturnFContainer("HUD"),
                        self.ReturnFContainer("HUD2")
                    }, self.game.rainWorld, CreatureController.creatureControllers.GetValue(owner, (c) => throw new InvalidProgrammerException("Not controlled creature: " + c)));

                    var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
                    self.hud.AddPart(new HUD.TextPrompt(self.hud)); // game assumes this never null
                    self.hud.AddPart(new HUD.Map(self.hud, new HUD.Map.MapData(self.game.world, self.game.rainWorld))); // game assumes this too :/
                    self.hud.AddPart(new MeadowProgressionHud(self.hud));
                    self.hud.AddPart(new MeadowEmoteHud(self.hud, self, owner));
                    self.hud.AddPart(new MeadowHud(self.hud, self, owner));
                }
            }
            orig(self);
        }
    }
}
