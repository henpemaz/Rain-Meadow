using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void MeadowHooks()
        {
            CicadaController.EnableCicada();
            LizardController.EnableLizard();
            ScavengerController.EnableScavenger();
            NoodleController.EnableNoodle();
            EggbugController.EnableEggbug();

            AbstractMeadowCollectible.Enable();

            On.RoomCamera.Update += RoomCamera_Update; // init meadow hud

            IL.HUD.Map.ctor += Map_OwnerFixup; // support non-slug owner
            IL.HUD.Map.CreateDiscoveryTextureFromVisitedRooms += Map_OwnerFixup; // support non-slug owner

            On.RegionGate.ctor += RegionGate_ctor;
            On.RegionGate.PlayersInZone += RegionGate_PlayersInZone1;
            On.RegionGate.PlayersStandingStill += RegionGate_PlayersStandingStill;
            On.RegionGate.AllPlayersThroughToOtherSide += RegionGate_AllPlayersThroughToOtherSide1;

            On.RainWorldGame.AllowRainCounterToTick += RainWorldGame_AllowRainCounterToTick; // timer stuck
            On.ShelterDoor.Close += ShelterDoor_Close; // door stuck
            On.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld; // timer stuck past cycle start

            On.AbstractCreature.ChangeRooms += AbstractCreature_ChangeRooms; // displayer follow creature

            On.Room.LoadFromDataString += Room_LoadFromDataString1; // places of spawning items

            // open gate
            new Hook(typeof(RegionGate).GetProperty("MeetRequirement").GetGetMethod(), this.RegionGate_MeetRequirement);
            new Hook(typeof(WaterGate).GetProperty("EnergyEnoughToOpen").GetGetMethod(), this.RegionGate_EnergyEnoughToOpen);
            new Hook(typeof(ElectricGate).GetProperty("EnergyEnoughToOpen").GetGetMethod(), this.RegionGate_EnergyEnoughToOpen);

            On.Creature.Die += Creature_Die; // do not die!

            On.WormGrass.Worm.ctor += Worm_ctor; // only cosmetic worms

        }

        private void Worm_ctor(On.WormGrass.Worm.orig_ctor orig, WormGrass.Worm self, WormGrass wormGrass, WormGrass.WormGrassPatch patch, Vector2 basePos, float reachHeight, float iFac, float lengthFac, bool cosmeticOnly)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                orig(self, wormGrass, patch, basePos, reachHeight, iFac, lengthFac, true);
                return;
            }

            orig(self, wormGrass, patch, basePos, reachHeight, iFac, lengthFac, cosmeticOnly);
        }

        private void Creature_Die(On.Creature.orig_Die orig, Creature self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return;
            }
            orig(self);
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
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if (mgm.avatar.creature.pos.room == self.room.abstractRoom.index && (!self.letThroughDir || mgm.avatar.creature.pos.x < self.room.TileWidth / 2 + 3) && (self.letThroughDir || mgm.avatar.creature.pos.x > self.room.TileWidth / 2 - 4))
                {
                    return false;
                }
                return true;
            }
            return orig(self);
        }

        private bool RegionGate_PlayersStandingStill(On.RegionGate.orig_PlayersStandingStill orig, RegionGate self)
        {
            if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode mgm)
            {
                if(CreatureController.creatureControllers.TryGetValue(mgm.avatar.creature, out var c))
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
                if (CreatureController.creatureControllers.TryGetValue(mgm.avatar.creature, out var c))
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
            }
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
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode)
            {
                return false;
            }
            return orig(self);
        }

        private void Map_OwnerFixup(ILContext il)
        {
            try
            {
                //else if (this.hud.owner.GetOwnerType() != HUD.OwnerType.RegionOverview)
                //{
                //    saveState = (this.hud.owner as SleepAndDeathScreen).saveState;
                //}
                // becomes
                //else if (this.hud.owner.GetOwnerType() == HUD.OwnerType.SleepScreen || this.hud.owner.GetOwnerType() == HUD.OwnerType.DeathScreen)
                //{
                //    if (this.hud.owner.GetOwnerType() == MeadowCustomization.creatureControllerHudOwner)
                //        this.hud.rainWorld.progression.currentSaveState;
                //    else 
                //        saveState = (this.hud.owner as SleepAndDeathScreen).saveState;
                //}

                var c = new ILCursor(il);
                var loc = il.Body.Variables.First(v=>v.VariableType.Name == "SaveState").Index;
                ILLabel vanilla = il.DefineLabel();
                ILLabel skipToEnd = null;
                MethodReference op_Ineq;
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdsfld<HUD.HUD.OwnerType>("RegionOverview"),
                    i => i.MatchCall(out op_Ineq),
                    i => i.MatchBrfalse(out skipToEnd)
                    );
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((HUD.Map map) => map.hud.owner.GetOwnerType() != CreatureController.controlledCreatureHudOwner);
                c.Emit(OpCodes.Brtrue, vanilla);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit<HUD.HudPart>(OpCodes.Ldfld, "hud");
                c.Emit<HUD.HUD>(OpCodes.Ldfld, "rainWorld");
                c.Emit<RainWorld>(OpCodes.Ldfld, "progression");
                c.Emit<PlayerProgression>(OpCodes.Ldfld, "currentSaveState");
                c.Emit(OpCodes.Stloc, loc);
                c.Emit(OpCodes.Br, skipToEnd);
                c.MarkLabel(vanilla);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void RoomCamera_Update(On.RoomCamera.orig_Update orig, RoomCamera self)
        {
            if(OnlineManager.lobby != null && OnlineManager.lobby.gameMode is MeadowGameMode meadowGameMode)
            {
                if(self.hud == null && self.followAbstractCreature?.realizedObject is Creature owner)
                {
                    if(owner != meadowGameMode.avatar.realizedCreature) { RainMeadow.Error($"Camera owner != avatar {owner} {meadowGameMode.avatar}"); }

                    self.hud = new HUD.HUD(new FContainer[]
                    {
                        self.ReturnFContainer("HUD"),
                        self.ReturnFContainer("HUD2")
                    }, self.room.game.rainWorld, owner is Player player? player : CreatureController.creatureControllers.TryGetValue(owner.abstractCreature, out var controller) ? controller : throw new InvalidProgrammerException("Not player nor controlled creature"));

                    var mgm = OnlineManager.lobby.gameMode as MeadowGameMode;
                    self.hud.AddPart(new HUD.TextPrompt(self.hud)); // game assumes this never null
                    self.hud.AddPart(new HUD.Map(self.hud, new HUD.Map.MapData(self.room.world, self.room.game.rainWorld))); // game assumes this too :/
                    self.hud.AddPart(new EmoteHandler(self.hud, self, owner));
                    self.hud.AddPart(new MeadowHud(self.hud, self, owner));
                }
            }
            orig(self);
        }
    }
}
