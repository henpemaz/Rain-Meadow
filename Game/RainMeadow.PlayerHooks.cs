using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;
using UnityEngine.PlayerLoop;
using System.Collections.Generic;

namespace RainMeadow;

public partial class RainMeadow
{
    public static bool sSpawningAvatar;
    public void PlayerHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate; // Personas are set as non-transferable

        On.Player.ctor += Player_ctor;
        On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass;
        new Hook(typeof(Player).GetProperty("slugcatStats").GetGetMethod(), this.Player_slugcatStats);
        new Hook(typeof(Player).GetProperty("slugcatStats").GetGetMethod(), this.Player_slugcatStatsGourmandBack);
        IL.Player.Update += Player_Update;
        On.Player.Update += Player_Update1;
        On.Player.Die += PlayerOnDie;
        On.Player.Destroy += Player_Destroy;
        On.Player.Grabability += PlayerOnGrabability;
        On.Player.GrabUpdate += Player_GrabUpdate1;
        IL.Player.GrabUpdate += Player_GrabUpdate;
        IL.Player.GrabUpdate += Player_GrabUpdate_FixSpearmasterNeedles;
        IL.Player.SwallowObject += Player_SwallowObject;
        On.Player.Regurgitate += Player_Regurgitate;
        On.Player.ThrowObject += Player_ThrowObject;
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
        On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
        IL.Player.Collide += Player_Collide;
        On.Player.SlugSlamConditions += Player_SlugSlamConditions;
        IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
        IL.Player.PyroDeath += PhysicalObject_Explode;
        On.Player.CanMaulCreature += Player_CanMaulCreature;
        On.Player.AddFood += Player_AddFood;
        On.Player.AddQuarterFood += Player_AddQuarterFood;
        On.Player.SubtractFood += Player_SubtractFood;
        On.Player.FoodInRoom_bool += Player_FoodInRoom;
        On.Mushroom.BitByPlayer += Mushroom_BitByPlayer;
        On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites1;

        On.SlugcatHand.Update += SlugcatHand_Update;
        On.Player.GetHeldItemDirection += Player_GetHeldItemDirection;
        On.AbstractCreature.ctor += AbstractCreature_ctor;
        On.Player.ShortCutColor += Player_ShortCutColor;
        On.Player.checkInput += Player_checkInput;
        On.Weapon.HitSomethingWithoutStopping += Weapon_HitSomethingWithoutStopping;
        IL.Player.ThrowObject += Player_ThrowObject1;
        On.Player.SlugOnBack.Update += SlugOnBack_Update;
        On.PlayerCarryableItem.PickedUp += PlayerCarryableItem_PickedUp;
        IL.Player.checkInput += Player_checkInput_IgnoreIfCarryingSlugNPC;

        On.SlugcatStats.HiddenOrUnplayableSlugcat += SlugcatStatsOnHiddenOrUnplayableSlugcat;
        On.PlayerGraphics.DefaultSlugcatColor += PlayerGraphics_DefaultSlugcatColor;
        On.SlugcatHand.EngageInMovement += SlugcatHand_EngageInMovement;

        On.Player.GrabUpdate += Player_GrabUpdatePiggyBack;
        On.Player.CanIPutDeadSlugOnBack += Player_CanIPutDeadSlugOnBack;
        On.Player.CanEatMeat += Player_CanEatMeat;
        new Hook(typeof(Player).GetProperty("rippleLevel").GetGetMethod(), this.Player_SetRippleLevel);
        new Hook(typeof(Player).GetProperty("CanLevitate").GetGetMethod(), this.Player_SetLevitate);
        new Hook(typeof(Player).GetProperty("camoLimit").GetGetMethod(), this.Player_SetCamoDuration);
        new Hook(typeof(Player).GetProperty("maxRippleLevel").GetGetMethod(), this.Player_SetRippleLevel);
        new Hook(typeof(Watcher.CamoMeter).GetProperty("Unlocked").GetGetMethod(), this.CamoMeter_SetCamoMeter);
        new Hook(typeof(Watcher.CamoMeter).GetProperty("ForceShow").GetGetMethod(), this.CamoMeter_SetCamoMeter);
        new Hook(typeof(Player).GetProperty("CanSpawnDynamicWarpPoints").GetGetMethod(), this.Player_CanSpawnDynamicWarpPoints);

        On.Player.TickLevitation += (On.Player.orig_TickLevitation orig, Player self, bool levitateUp) =>
        {
            WatcherOverrideForLevitation = true;
            orig(self, levitateUp);
            WatcherOverrideForLevitation = false;
        };
        On.Player.MovementUpdate += (On.Player.orig_MovementUpdate orig, Player self, bool eu) =>
        {
            WatcherOverrideRippleLevel = true;
            orig(self, eu);
            WatcherOverrideRippleLevel = false;
        };
        On.Player.WatcherUpdate += (On.Player.orig_WatcherUpdate orig, Player self) =>
        {
            WatcherOverrideRippleLevel = true;
            orig(self);
            WatcherOverrideRippleLevel = false;
        };
        On.Player.CamoUpdate += Player_CamoUpdate;



        // get tired and slam other players if we have gourmand our back
        IL.Player.SlugSlamConditions += GourmandOnBackMechanics;
        IL.Player.ClassMechanicsGourmand += GourmandOnBackMechanics;

        // use saints toungue if we have saint on our back.
        On.Player.UpdateMSC += Player_UpdateMSC;
        IL.Player.SlugOnBack.GraphicsModuleUpdated += SlugOnBack_GraphicsModuleUpdated;

        new Hook(typeof(Player).GetProperty(nameof(Player.CanPutSlugToBack)).GetGetMethod(), DisablePutOnBackWithSpearMasterAbility);
        new Hook(typeof(Player).GetProperty(nameof(Player.CanPutSpearToBack)).GetGetMethod(), DisablePutOnBackWithSpearMasterAbility);
        new Hook(typeof(Player).GetProperty(nameof(Player.CanRetrieveSlugFromBack)).GetGetMethod(), DisablePutOnBackWithSpearMasterAbility);
        new Hook(typeof(Player).GetProperty(nameof(Player.CanRetrieveSpearFromBack)).GetGetMethod(), DisablePutOnBackWithSpearMasterAbility);
        new Hook(typeof(Player).GetProperty(nameof(Player.CanRetrieveSpearFromBack)).GetGetMethod(), DisablePutOnBackWithSpearMasterAbility);

        
        // IL.Player.GrabUpdate += Player_SynchronizeSocialEventDrop;
        // IL.Player.TossObject += Player_SynchronizeSocialEventDrop;
        // IL.Player.ReleaseObject += Player_SynchronizeSocialEventDrop;
    }
    public bool BottomPlayerUsingSpearmasterAbility(Player spearmaster, out Player user)
    {
        user = null!;
        if (OnlineManager.lobby == null) return false;
        if (!ModManager.MSC) return false; 
        if (spearmaster.SlugCatClass != MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear) return false; 

        if (GetBottomPlayer(spearmaster, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Spear, out var bottom))
        {
            if (bottom.input[0].pckp && (bottom.input[0].y > 0))
            {
                user = bottom;
                return true;
            }
        }

        return false;
    }
    
    public bool DisablePutOnBackWithSpearMasterAbility(Func<Player, bool> orig, Player self)
    {
        bool orig_result = orig(self);
        if (OnlineManager.lobby != null) return orig_result;
        if (BottomPlayerUsingSpearmasterAbility(self, out _))
        {
            return false;
        }

        return orig_result;
    }

    // Used to override normal ripple level
    public static bool WatcherOverrideRippleLevel = false;
    // And for the "levitation calculation" ticks so we levitate like we had ripple lvl. 10
    public static bool WatcherOverrideForLevitation = false;

    private float Player_SetRippleLevel(Func<Player, float> orig, Player self)
    {
        if (isStoryMode(out var storyGameMode) && self.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher && storyGameMode.currentCampaign != Watcher.WatcherEnums.SlugcatStatsName.Watcher)
        {
            if (WatcherOverrideForLevitation) return 10f;
            if (WatcherOverrideRippleLevel) return 4f;
        }
        return orig(self);
    }
    private bool CamoMeter_SetCamoMeter(Func<Watcher.CamoMeter, bool> orig, Watcher.CamoMeter self)
    {
        if (isStoryMode(out var storyGameMode) && self.Player.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher && storyGameMode.currentCampaign != Watcher.WatcherEnums.SlugcatStatsName.Watcher)
        {
            return true;
        }
        return orig(self);
    }
    // This is funky. Can't seem to ever get it to only be true when airborne
    private bool Player_SetLevitate(Func<Player, bool> orig, Player self)
    {
        if (isStoryMode(out var storyGameMode) && self.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher && storyGameMode.currentCampaign != Watcher.WatcherEnums.SlugcatStatsName.Watcher)
        {
            return true;
        }
        return orig(self);
    }
    private float Player_SetCamoDuration(Func<Player, float> orig, Player self)
    {
        if (isStoryMode(out var storyGameMode) && self.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher && storyGameMode.currentCampaign != Watcher.WatcherEnums.SlugcatStatsName.Watcher)
        {
            return 1500f;
        }
        return orig(self);
    }
    private bool Player_CanSpawnDynamicWarpPoints(Func<Player, bool> orig, Player self)
    {
        if (isStoryMode(out var storyGameMode) && self.slugcatStats.name == Watcher.WatcherEnums.SlugcatStatsName.Watcher && storyGameMode.currentCampaign != Watcher.WatcherEnums.SlugcatStatsName.Watcher)
        {
            return false;
        }
        return orig(self);
    }

    private void Player_CamoUpdate(On.Player.orig_CamoUpdate orig, Player self)
    {
        if (self.isCamo && (!self.Consious || self.warpExhausionTime > 0))
        {
            self.ToggleCamo();
        }

        if (self.camoProgress < 1f && self.transitionRipple != null && self.room.fsRipple != null)
        {
            self.room.fsRipple.Destroy();
            self.room.fsRipple = null;
        }

        if (self.isCamo)
        {
            self.camoCharge = Mathf.Min(self.camoCharge + 1f, self.usableCamoLimit);
            self.inCamoTime++;
            self.outOfCamoTime = 0;
            if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.miscWorldSaveData.usedCamoAbility == 0)
            {
                self.room.game.GetStorySession.saveState.miscWorldSaveData.usedCamoAbility = 1;
            }

            if (self.camoCharge >= self.usableCamoLimit)
            {
                self.camoRechargePenalty = 400;
                self.Stun(80);
            }
        }
        else
        {
            self.inCamoTime = 0;
            self.outOfCamoTime++;
            if (self.camoRechargePenalty <= 0)
            {
                self.camoCharge = Mathf.Max(self.camoCharge - 1f, 0f);
            }
        }

        if (self.consumedRippleFood > 0)
        {
            self.consumedRippleFood--;
            self.camoCharge = Mathf.Max(self.camoCharge - 2f, 0f);
        }

        if (self.camoRechargePenalty > 0)
        {
            self.camoRechargePenalty--;
        }

        Watcher.WarpSpawningRipple obj = self.warpSpawningRipple;
        if (obj != null && obj.slatedForDeletetion)
        {
            self.warpSpawningRipple = null;
        }

        if (self.activateCamoTimer == 0 && self.watcherDynamicWarpInput && self.dynamicWarpCooldown <= 0)
        {
            self.activateDynamicWarpTimer++;
            Watcher.WarpSpawningRipple obj2 = self.warpSpawningRipple;
            if (obj2 == null || obj2.isFinished)
            {
                self.room.AddObject(self.warpSpawningRipple = new Watcher.WarpSpawningRipple(self.room, self.mainBodyChunk.pos, self.camoProgress, !self.KarmaIsReinforced));
            }

            if (self.activateDynamicWarpTimer >= self.activateDynamicWarpDuration)
            {
                self.SpawnDynamicWarpPoint();
                self.dynamicWarpCooldown = 200;
                self.activateDynamicWarpTimer = 0;
            }
        }
        else if (self.activateDynamicWarpTimer > 0)
        {
            self.activateDynamicWarpTimer = 0;
        }

        if (self.dynamicWarpCooldown > 0)
        {
            self.dynamicWarpCooldown--;
        }

        if (self.isCamo && self.camoProgress < 1f)
        {
            self.camoProgress += 0.01f;
        }
        else if (!self.isCamo && self.camoProgress > 0f)
        {
            self.camoProgress -= 0.01f;
        }

        if (self.rippleLevel >= 5f && self.rippleData != null)
        {
            self.rippleData.gameplayRippleAnimation = self.camoProgress;
        }

        self.TrySpawnTrailRipple();

        if (self.rippleLevel >= 5f && ((self.abstractCreature.rippleLayer == 0 && self.isCamo) || (self.abstractCreature.rippleLayer == 1 && !self.isCamo)))
        {
            self.ChangeRippleLayer(self.isCamo ? 1 : 0);
            if (self.rippleData != null && self.IsLocal())
            {
                self.rippleData.gameplayRippleActive = self.isCamo;
            }

            if (self.isCamo)
            {
                for (int i = 0; i < 5 * self.room.cameraPositions.Length; i++)
                {
                    self.room.MaterializeRippleSpawn(self.room.RandomPos(), Room.RippleSpawnSource.Dimension);
                }
            }
        }

        if (self.rippleData != null && self.IsLocal())
        {
            if (self.rippleLevel < 3f || !self.isCamo)
            {
                self.rippleData.TrailAmount = Mathf.Lerp(self.rippleData.TrailAmount, (self.rippleLevel < 2f) ? 0f : 0.01f, 0.01f);
            }
            else
            {
                self.rippleData.TrailAmount = Mathf.Lerp(self.rippleData.TrailAmount, (self.rippleLevel < 5f) ? 0.35f : 1f, 0.01f);
            }

            if (self.isCamo)
            {
                self.rippleData.trailPaletteAmount = Mathf.Lerp(self.rippleData.trailPaletteAmount, 0f, 0.09f);
            }
            else
            {
                self.rippleData.trailPaletteAmount = Mathf.Lerp(self.rippleData.trailPaletteAmount, 1f, 0.003f);
            }
        }

        if (self.isCamo && self.rippleLevel >= 3f && self.rippleLevel < 5f)
        {
            int num = Mathf.Min(80, self.lastPositions.Length - 1);
            int maxExclusive = Mathf.Min(40, self.lastPositions.Length - 1);
            if (Vector2.Distance(self.mainBodyChunk.pos, self.lastPositions[num]) > 50f && UnityEngine.Random.value < 0.002f)
            {
                int num2 = UnityEngine.Random.Range(0, maxExclusive);
                self.room.MaterializeRippleSpawn(self.lastPositions[num2], Room.RippleSpawnSource.PlayerTrail);
            }
        }
    }
    private void SlugOnBack_GraphicsModuleUpdated(ILContext ctx)
    {
        try
        {
            ILCursor c = new(ctx);

            c.GotoNext(MoveType.After,
                //                 41	0078	ldarg.0
                // 42	0079	ldc.i4.0
                // 43	007A	call	instance void Player/SlugOnBack::ChangeOverlap(bool)
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(0),
                x => x.MatchCall<Player.SlugOnBack>(nameof(Player.SlugOnBack.ChangeOverlap))
            );

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_2);
            c.EmitDelegate((Player.SlugOnBack self, bool eu) =>
            {
                if (OnlineManager.lobby != null && HasSlugcatClassOnBack(self.owner, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint, out Player saint_player))
                {
                    Player bottom;
                    if (!GetBottomPlayer(self.owner, null, out bottom))
                    {
                        bottom = self.owner;
                    }
                    if (saint_player!.tongue.Attached && self.owner.onBack == null && bottom.IsLocal())
                    {
                        Vector2 moveTo = Vector2.Lerp(self.owner.bodyChunks[0].pos, self.slugcat.bodyChunks[0].pos - Custom.DirVec(self.owner.bodyChunks[1].pos, self.owner.bodyChunks[0].pos) * 14f, 0.75f);
                        Vector2 moveTo2 = Vector2.Lerp(self.owner.bodyChunks[1].pos, self.slugcat.bodyChunks[1].pos - Custom.DirVec(self.owner.bodyChunks[1].pos, self.owner.bodyChunks[0].pos) * 14f, 0.75f);
                        self.owner.bodyChunks[0].MoveFromOutsideMyUpdate(eu, moveTo);
                        self.owner.bodyChunks[1].MoveFromOutsideMyUpdate(eu, moveTo2);
                        float mass_ratio = (self.owner.bodyChunks[0].mass / self.slugcat.bodyChunks[1].mass) / 2f;
                        var offset = self.slugcat.bodyChunks[1].vel - self.owner.bodyChunks[0].vel;
                        self.owner.bodyChunks[0].vel += offset;
                        self.owner.bodyChunks[1].vel += offset;
                        self.slugcat.bodyChunks[1].vel -= offset * mass_ratio;
                        return true;
                    }
                }
                return false;
            });

            c.Emit(OpCodes.Ret);
            c.Index = c.Index - 1;
            c.Emit(OpCodes.Brfalse, c.Next.Next);



        }
        catch (Exception except)
        {
            RainMeadow.Error(except);
        }

    }
    private void Player_UpdateMSC(On.Player.orig_UpdateMSC orig, Player self) {
        orig(self);
        if (OnlineManager.lobby != null && HasSlugcatClassOnBack(self, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Saint, out Player saint_player)) {
            if (self.tongue is not null && self.tongue.Attached) {
                self.tongue.Release();
            }


            if (self.IsLocal() && self.onBack == null) {
                if (!saint_player!.tongue.Attached) {
                    if (!MoreSlugcats.MMF.cfgOldTongue.Value && self.input[0].jmp && !self.input[1].jmp && !self.input[0].pckp && self.canJump <= 0 && self.bodyMode != Player.BodyModeIndex.Crawl && self.animation != Player.AnimationIndex.ClimbOnBeam && self.animation != Player.AnimationIndex.AntlerClimb && self.animation != Player.AnimationIndex.HangFromBeam 
                            && saint_player!.SaintTongueCheck() && 
                            self.bodyMode != Player.BodyModeIndex.CorridorClimb && !self.corridorDrop && 
                            self.bodyMode != Player.BodyModeIndex.ClimbIntoShortCut && self.bodyMode != Player.BodyModeIndex.WallClimb && 
                            self.bodyMode != Player.BodyModeIndex.Swimming && self.animation != Player.AnimationIndex.VineGrab &&
                            self.animation != Player.AnimationIndex.ZeroGPoleGrab)
                    {
                        Vector2 vector = new Vector2((float)self.flipDirection, 0.7f);
                        Vector2 normalized = vector.normalized;
                        if (self.input[0].y > 0)
                        {
                            normalized = new Vector2(0f, 1f);
                        }
                        normalized = (normalized + self.mainBodyChunk.vel.normalized * 0.2f).normalized;
                        saint_player!.tongue.Shoot(normalized);
                    }
                }


                if (saint_player!.tongue.Attached) {
                    if (self.input[0].jmp && !self.input[1].jmp && saint_player!.tongueAttachTime >= 2)
					{
                        float num = Mathf.Lerp(1f, 1.15f, self.Adrenaline);
                        if (self.grasps[0] != null && self.HeavyCarry(self.grasps[0].grabbed) && !(self.grasps[0].grabbed is Cicada))
                        {
                            num += Mathf.Min(Mathf.Max(0f, self.grasps[0].grabbed.TotalMass - 0.2f) * 1.5f, 1.3f);
                        }

                        self.bodyChunks[0].vel.y = 8f * num;
                        self.bodyChunks[1].vel.y = 7f * num;
                        self.jumpBoost = 8f;
						saint_player!.tongue.Release();
                    }


                    if (self.input[0].y > 0)
                    {
                        saint_player!.tongue.decreaseRopeLength(3f);
                    }

                    if (saint_player!.input[0].x == 0) {
                        saint_player!.input[0].x = self.input[0].x;
                    }

                    if (self.input[0].y < 0)
                    {
                        saint_player!.tongue.increaseRopeLength(3f);
                    }
                }
            }
        }
    }

    private void GourmandOnBackMechanics(ILContext ctx) {
        // generic hook for when somebody has gourmand on there back
        try {
            ILCursor c = new(ctx);

            // converts all 
            // this.SlugCatClass <some operator> MoreSlugcatsEnums.SlugcatStatsName.Gourmand)
            // this.SlugCatClass <some operator> MoreSlugcatsEnums.SlugcatStatsName.Gourmand) || this.slugonback.slugcat.SlugCatClass <some operator> MoreSlugcatsEnums.SlugcatStatsName.Gourmand)

            Mono.Cecil.MethodReference comparison = null!;
            int overriden_count = 0;
            while (c.TryGotoNext(MoveType.After, 
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.SlugCatClass)),
                x => x.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand)),
                x => x.MatchCall(out comparison)
            )) {
                overriden_count++;
                if (comparison.Name == "op_Equality") {
                    c.Emit(OpCodes.Ldarg, 0);
                    c.EmitDelegate(static (bool result, Player p) => {
                        return result || ((OnlineManager.lobby != null) && HasSlugcatClassOnBack(p, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand, out _));
                    });
                } else if (comparison.Name == "op_Inequality") {
                    c.Emit(OpCodes.Ldarg, 0);
                    c.EmitDelegate(static (bool result, Player p) => {
                        return result && !((OnlineManager.lobby != null) && HasSlugcatClassOnBack(p, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand, out _));
                    });
                } else {
                    RainMeadow.Error($"No comparison implementation for {comparison.Name}");
                }
            }

            RainMeadow.Debug($"Overriden {overriden_count} comparisons in {ctx.Method.Name}");
        } catch (Exception except) {
            RainMeadow.Error(except);
        }



        
    }

    public static bool HasSlugcatClassOnBack(Player player, SlugcatStats.Name name, out Player? onback)
    {
        onback = null;
        int i = 25;

        List<Player?> checked_players = new List<Player?> { null };

        while (player != null && ((--i) >= 0)) // builtin hard limit to prevent infinite loops.
        {
            if (player.slugOnBack is null) break;
            player = player.slugOnBack.slugcat;
            if (checked_players.Contains(player)) break;
            checked_players.Add(player);

            if (player.SlugCatClass == name)
            {
                onback = player;
            }
        }

        return onback != null;
    }

    public static bool GetBottomPlayer(Player player, SlugcatStats.Name? Ignoreclass, out Player bottom) {
        bottom = null;
        int i = 25;
        var tempBottom = player;
        while ((--i) >= 0)
        {
            if (tempBottom.onBack is null) break;
            tempBottom = tempBottom.onBack;
            // somebody is already acting as the backpack mechanic guy.
            if ((tempBottom.SlugCatClass == Ignoreclass) && (tempBottom.onBack != null))
            {
                return false;
            }
        }

        if (tempBottom != player)
        {
            bottom = tempBottom;
        }

        return bottom != null;
    }
    

    private Vector2 Player_GetHeldItemDirection(On.Player.orig_GetHeldItemDirection orig, Player self, int hand)
    {
        if (OnlineManager.lobby != null && self.handPointing == hand && self.graphicsModule is PlayerGraphics playerGraphics && self.grasps[hand].grabbed is Spear)
        {
            // scary math below
            var vector = Custom.DegToVec(Custom.AimFromOneVectorToAnother(self.firstChunk.pos, playerGraphics.hands[hand].pos));
            return Vector3.Slerp(vector, Custom.DegToVec(90f + (80f + Mathf.Cos((float)(self.animationFrame + (self.leftFoot ? 9 : 3)) / 12f * 2f * (float)Math.PI) * 4f * playerGraphics.spearDir) * playerGraphics.spearDir), Mathf.Abs(playerGraphics.spearDir));
        }
        return orig(self, hand);
    }

    private void SlugcatHand_Update(On.SlugcatHand.orig_Update orig, SlugcatHand self)
    {
        if (OnlineManager.lobby != null && self.owner.owner is Player player)
        {
            // Keep the pointing on as if it was a local update, this will be kept until handPointing is
            // no longer -1, remember this is networking and in some frames we may have non-updated
            // reachingForObject (see RealizedPlayerState.cs)
            if (player.graphicsModule is PlayerGraphics playerGraphics && player.handPointing != -1)
            {
                playerGraphics.hands[player.handPointing].reachingForObject = true;
            }
            orig(self);
        }
        else
        {
            orig(self);
        }
    }

    public bool SlugcatHand_EngageInMovement(On.SlugcatHand.orig_EngageInMovement orig, global::SlugcatHand self)
    {
        if (OnlineManager.lobby != null)
        {
            if (self.owner.owner is Player slugcat && !slugcat.isNPC && slugcat.onBack != null)
            {
                (self.owner as PlayerGraphics)!.airborneCounter = 0; // fix for weird hand movement when on back.
            }
        }

        return orig(self);
    }

    public bool Player_CanEatMeat(On.Player.orig_CanEatMeat orig, Player self, Creature crit)
    {
        if (OnlineManager.lobby != null)
        {
            if (self.standing && self.CanPutSlugToBack)
            {
                if (crit is Player p && p.dead && self.CanIPutDeadSlugOnBack(p))
                {
                    return false;
                }
            }
        }
        return orig(self, crit);
    }

    bool Player_CanIPutDeadSlugOnBack(On.Player.orig_CanIPutDeadSlugOnBack orig, Player self, Player pickUpCandidate)
    {
        if (OnlineManager.lobby != null)
        {
            if (pickUpCandidate == null || pickUpCandidate.isNPC) return false;
            return true;
        }

        return orig(self, pickUpCandidate);
    }

    Color PlayerGraphics_DefaultSlugcatColor(On.PlayerGraphics.orig_DefaultSlugcatColor orig, SlugcatStats.Name name)
    {
        Color orig_color = orig(name);
        if (OnlineManager.lobby != null)
        {
            if (ModManager.MSC && name == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Slugpup)
            {
                return new Color(0.467f, 0.867f, 0.812f);
            }
        }

        return orig_color;
    }


    // Player Quick Piggy Backing from Stick Together Co-Op by WillowWisp 
    private void Player_GrabUpdatePiggyBack(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        if (OnlineManager.lobby == null) return;
        if (!self.IsLocal()) return;
        if (!OnlineManager.lobby.gameMode.PlayersCanStack)
        {
            return;
        }
        if (isArenaMode(out var arena) && arena.countdownInitiatedHoldFire)
        {
            return;
        }

        float range = 26 + self.bodyChunks[1].rad;
        if (self.input[0].pckp && !self.input[1].pckp && self.onBack == null && self.room != null &&
            !self.isNPC && !self.pyroJumpped && !self.submerged && self.standing && self.lowerBodyFramesOffGround > 0)
        {
            foreach (PhysicalObject obj in self.room.physicalObjects[self.collisionLayer])
            {
                if (obj is Player other)
                {
                    if (other == self) continue;
                    if (other.slugOnBack == null) continue;
                    if (other.abstractCreature.GetAllConnectedObjects().Contains(self.abstractCreature)) continue;
                    if (other.isNPC) continue;
                    if (!Custom.DistLess(self.bodyChunks[1].pos, other.bodyChunks[0].pos, range)) continue;
                    if (!other.Consious) continue;
                    // if (other.onBack != null) continue; // this is pretty funny


                    var viable = false;
                    viable = viable || other.standing;
                    viable = viable || other.animation == Player.AnimationIndex.SurfaceSwim;
                    viable = viable || other.animation == Player.AnimationIndex.GrapplingSwing;
                    if (!viable) continue;
                    if (other.IsLocal())
                    {
                        other.slugOnBack?.SlugToBack(self);
                    }
                    else
                    {
                        var myobj = self.abstractCreature.GetOnlineCreature();
                        if (myobj is null)
                        {
                            RainMeadow.Error($"{self.abstractCreature} is trying to backpack but it has no online presence.");
                            continue;
                        }
                        var otherobj = other.abstractCreature.GetOnlineCreature();
                        if (otherobj is null)
                        {
                            RainMeadow.Error($"{self.abstractCreature} is trying to backpack {other.abstractCreature} but it has no online presence.");
                            continue;
                        }
                        otherobj.owner.InvokeRPC(otherobj.HopOnBack, myobj);
                    }
                    break;
                }
            }
        }
    }

    private void PlayerCarryableItem_PickedUp(On.PlayerCarryableItem.orig_PickedUp orig, PlayerCarryableItem self, Creature upPicker)
    {
        if (OnlineManager.lobby != null)
        {
            upPicker.abstractPhysicalObject.GetOnlineObject().didParry = false;
        }
        orig(self, upPicker);
    }

    // This is Abysmal and doesn't work 
    // void Player_SynchronizeSocialEventDrop(ILContext context) { 
    //     try {
    //         ILCursor cursor = new(context);
    //         int socialeventdrops_found = 0;
    //         while (cursor.TryGotoNext(MoveType.Before, 
    //             x => x.MatchCall(nameof(SocialEventRecognizer), nameof(SocialEventRecognizer.CreaturePutItemOnGround)))) {
    //             ++socialeventdrops_found;
    //             cursor.EmitDelegate(Player_CreaturePutItem);
    //             cursor.Emit(OpCodes.Br_S, 2); // Skip CreaturePutItemOnGround
    //         }

    //         RainMeadow.Debug($"{context.Method.Name}: Found {socialeventdrops_found} calls to {nameof(SocialEventRecognizer.CreaturePutItemOnGround)}");
    //     } catch (Exception except) {
    //         RainMeadow.Error(except);
    //     }
    // }


    private void Player_GrabUpdate1(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        // if (isArenaMode(out var _))
        if (OnlineManager.lobby != null)
        {
            if (self.grasps != null)
            {
                for (int i = 0; i < self.grasps.Length; i++)
                {
                    if (self.grasps[i] != null)
                    {
                        if (self.grasps[i].grabbed is Player pl && pl.input[0].thrw)
                        {
                            if (pl.isNPC) return;
                            self.grasps[i].Release();
                        }
                    }
                }
            }
        }
    }

    // Hide the Meadow mode slugcat so it doesn't appear in menus (e.g. arena)
    private bool SlugcatStatsOnHiddenOrUnplayableSlugcat(On.SlugcatStats.orig_HiddenOrUnplayableSlugcat orig, SlugcatStats.Name i)
    {
        if (i == Ext_SlugcatStatsName.OnlineSessionPlayer)
        {
            return true;
        }

        return orig(i);
    }

    private void SlugOnBack_Update(On.Player.SlugOnBack.orig_Update orig, Player.SlugOnBack self, bool eu)
    {
        orig(self, eu);
        if (OnlineManager.lobby != null && self.slugcat != null)
        {
            if (self.slugcat.isNPC) return;

            self.slugcat.standing = true; // SlugNPCs do this in there AI. but it looks right for all players.
            self.slugcat.animation = Player.AnimationIndex.GrapplingSwing; // jolly does this
            self.slugcat.immuneToFallDamage += 10;
            if (self.slugcat.input[0].jmp)
            {
                var slug = self.slugcat;
                self.owner.slugOnBack.DropSlug(); //NOTE: makes self.slugcat null!

                if (!slug.isNPC && slug.input[0].jmp && slug.IsLocal())
                {
                    slug.jumpChunk = self.owner.mainBodyChunk;
                    slug.JumpOnChunk();
                }
            }

            if (self.owner.input[0].pckp && (self.owner.input[0].y > 0) && self.HasASlug)
            {
                self.counter = 0;
            }
        }
    }

    private void Player_checkInput_IgnoreIfCarryingSlugNPC(ILContext context)
    {
        try
        {
            var cursor = new ILCursor(context);
            // if (this.controller != null)
            cursor.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.controller))
                // x => x.MatchBrfalse
            );

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate((Player.PlayerController controller, Player self) =>
            {
                if (OnlineManager.lobby != null && isStoryMode(out var _))
                {
                    if (controller is OnlineController && self.isNPC)
                    {
                        if (self.grabbedBy.FirstOrDefault(x => x.grabber is Player) is not null) return null;
                        if (self.onBack is not null) return null;
                    }
                }

                return controller;
            });

            // Delegate inserted after this.input[0] = RWInput.PlayerInput(num);


        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    // Sain't:  Let 1) Saint throw spears 2) at normal velocity if toggled
    private void Player_ThrowObject1(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = il.DefineLabel();
            var skip2 = il.DefineLabel();

            c.GotoNext(moveType: MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("SlugCatClass"),
                i => i.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>("Saint"),
                i => i.MatchCall("ExtEnum`1<SlugcatStats/Name>", "op_Equality"),
                i => i.MatchBrfalse(out skip)
                );
            c.EmitDelegate(() => isArenaMode(out var arena) && arena.sainot);
            c.Emit(OpCodes.Brtrue, skip);

            c.GotoNext(moveType: MoveType.After,
            i => i.MatchLdfld<Creature.Grasp>("grabbed"),
            i => i.MatchIsinst<Rock>(),
            i => i.MatchBrfalse(out skip2)
            );
            c.EmitDelegate(() => isArenaMode(out var arena) && arena.sainot);
            c.Emit(OpCodes.Brtrue, skip);

            c.MarkLabel(skip);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }

    }

    private void Weapon_HitSomethingWithoutStopping(On.Weapon.orig_HitSomethingWithoutStopping orig, Weapon self, PhysicalObject obj, BodyChunk chunk, PhysicalObject.Appendage appendage)
    {
        if (isStoryMode(out var _))
        {
            if (obj is Player)
            {
                if (self.thrownBy == (obj as Player) && obj.IsLocal() && self is Spear)
                {
                    return;
                }
            }
        }
        orig(self, obj, chunk, appendage);
    }

    private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        if (OnlineManager.lobby != null)
        {
            var onlineEntity = self.abstractCreature?.GetOnlineObject();
            if (onlineEntity is not null)
            {
                if (onlineEntity.isMine)
                { // If we own the player we don't need a controller
                    if (self.controller is OnlineController)
                    {
                        self.controller = null;
                    }

                }
                else
                {
                    if (self.controller is null)
                    { // If we don't own the player we need a controller
                        self.controller = new OnlineController(onlineEntity, self);
                    }

                    // If we're being held by a local player. they should request ownership of us
                    if (self.isNPC)
                    {
                        if (self.onBack is not null)
                        {
                            if (self.onBack.IsLocal() && onlineEntity.isTransferable && !onlineEntity.isPending)
                            {
                                try
                                {
                                    onlineEntity.Request();
                                }
                                catch (Exception except)
                                {
                                    RainMeadow.Debug(except);
                                }
                            }
                        }
                    }
                }
            }
        }

        orig(self);
        if (OnlineManager.lobby != null)
        {
            if (self.controller is null && self.room.world.game.cameras[0]?.hud is HUD.HUD hud
                && (hud.textPrompt?.pausedMode is true || hud.parts.OfType<ChatHud>().Any(x => x.chatInputActive) || (hud.parts.OfType<SpectatorHud>().Any(x => x.isActive) && RainMeadow.rainMeadowOptions.StopMovementWhileSpectateOverlayActive.Value)))
            {
                InputOverride.StopPlayerMovement(self);
            }

            if (isArenaMode(out var arena))
            {
                if (arena.countdownInitiatedHoldFire)
                {
                    InputOverride.HoldFire(self);
                }

                ArenaHelpers.OverideSlugcatClassAbilities(self, arena);
            }

            if (ModManager.MSC)
            {
                if (BottomPlayerUsingSpearmasterAbility(self, out _))
                {
                    self.input[0].pckp = true;
                }
            } 

            if (!self.isNPC)
            {
                Player? grabbingplayer = self.grabbedBy.FirstOrDefault(x => x.grabber is Player)?.grabber as Player;
                if (grabbingplayer != null)
                {
                    if (!self.input[0].AnyDirectionalInput && !self.input[0].jmp)
                    {
                        self.input[0].x = grabbingplayer.input[0].x;
                        self.input[0].y = grabbingplayer.input[0].y;
                        if (grabbingplayer.bodyMode == Player.BodyModeIndex.Crawl && self.standing)
                        {
                            self.input[0].y = -1;
                        }
                        if (grabbingplayer.bodyMode == Player.BodyModeIndex.Stand && !self.standing)
                        {
                            self.input[0].y = 1;
                        }

                        self.input[0].jmp = grabbingplayer.input[0].jmp;
                    }
                }

            }
        }
    }

    private void Player_Update(ILContext il)
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
            c.EmitDelegate((Player self) => self.abstractPhysicalObject.IsLocal());
            c.Emit(OpCodes.Brfalse, skip);
            c.Index += 6;
            c.MarkLabel(skip);

            // don't handle shelter for meadow and remote scugs
            c.Index = 0;
            ILLabel skipShelter = null;
            c.GotoNext(i => i.MatchCallOrCallvirt<ShelterDoor>("Close"));
            c.GotoPrev(MoveType.After,
                i => i.MatchCallOrCallvirt<AbstractRoom>("get_shelter"),
                i => i.MatchBrfalse(out skipShelter)
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) =>
            {
                if (OnlineManager.lobby != null)
                {
                    if (OnlineManager.lobby.gameMode is MeadowGameMode) // meadow crashes with msc assuming slugpupbars is there
                        return false;
                    //if (OnlineManager.lobby.gameMode is StoryGameMode storyGameMode && storyGameMode.readyForWin)
                    //    return true;
                    if (!self.abstractCreature.IsLocal()) // don't shelter if remote
                        return false;
                }
                return true;
            });
            c.Emit(OpCodes.Brfalse, skipShelter);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private void Player_Update1(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (OnlineManager.lobby != null && self.objectInStomach != null)
            self.objectInStomach.pos = self.abstractCreature.pos;
        if (isStoryMode(out var gameMode) && self.abstractCreature.IsLocal())
            gameMode.storyClientData.readyForWin = false;
        orig(self, eu);

        if (isStoryMode(out var story) && !self.inShortcut && OnlineManager.players.Count > 4)
        {
            if (self.room.abstractRoom.shelter || self.room.IsGateRoom())
            {
                if (!self.IsLocal() && self.collisionLayer != 0)
                {
                    self.room.ChangeCollisionLayerForObject(self, 0);

                }
            }
            else
            {
                if (!self.IsLocal() && self.collisionLayer != 1)
                {
                    self.room.ChangeCollisionLayerForObject(self, 1);

                }
            }
        }

        if (isArenaMode(out var arena) && !self.inShortcut)
        {
            if (arena.countdownInitiatedHoldFire)
            {
                if (self.collisionLayer != 0)
                {
                    self.room.ChangeCollisionLayerForObject(self, 0);
                }
            }
            else
            {
                if (self.collisionLayer != 1)
                {
                    self.room.ChangeCollisionLayerForObject(self, 1);
                }
            }
        }

    }

    private UnityEngine.Color Player_ShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
    {

        if (OnlineManager.lobby != null)
        {
            if (RainMeadow.creatureCustomizations.TryGetValue(self, out var custom))
            {
                var color = orig(self);
                custom.ModifyBodyColor(ref color);
                return color;
            }
        }
        return orig(self);
    }

    private void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            if (self.bites < 1)
            {
                if (!OnlineManager.lobby.isOwner)
                {
                    OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.ReinforceKarma);
                }
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeOnceRPC(StoryRPCs.PlayReinforceKarmaAnimation);
                    }
                }
            }
        }
    }

    private void PlayerGraphics_DrawSprites1(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
    {
        if (OnlineManager.lobby != null)
        {
            try
            {
                orig(self, sLeaser, rCam, timeStacker, camPos);
            }
            catch (System.Exception e)
            {
                RainMeadow.Error(e);
            }
        }
        else
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }

    }

    private void Player_GrabUpdate(ILContext il)
    {
        try
        {
            // remote spearmaster don't spawn spear
            var c = new ILCursor(il);
            var skip = il.DefineLabel();
            c.GotoNext(
                i => i.MatchLdloc(16),
                i => i.MatchLdfld<PlayerGraphics.TailSpeckles>("spearProg"),
                i => i.MatchLdcR4(1),
                i => i.MatchBneUn(out skip)
                );
            c.GotoNext(
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdfld<Room>("world"),
                i => i.MatchLdnull(),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<Creature>("get_mainBodyChunk"),
                i => i.MatchLdfld<BodyChunk>("pos"),
                i => i.MatchCallOrCallvirt<Room>("GetWorldCoordinate"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdfld<Room>("game"),
                i => i.MatchCallOrCallvirt<RainWorldGame>("GetNewID"),
                i => i.MatchLdcI4(0),
                i => i.MatchNewobj<AbstractSpear>()
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) => {
                if (BottomPlayerUsingSpearmasterAbility(self, out var bottom))
                {
                    return bottom.IsLocal();
                }


                return self.abstractPhysicalObject.IsLocal();
            });
            c.Emit(OpCodes.Brfalse, skip);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private void Player_GrabUpdate_FixSpearmasterNeedles(ILContext il)
    {
        try
        {
            // spearmaster needle set state before registering
            var c = new ILCursor(il);
            int loc = 0;
            c.GotoNext(moveType: MoveType.After,
                // AbstractSpear abstractSpear = new AbstractSpear(room.world, null, room.GetWorldCoordinate(base.mainBodyChunk.pos), room.game.GetNewID(), explosive: false);
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdfld<Room>("world"),
                i => i.MatchLdnull(),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdarg(0),
                i => i.MatchCall<Creature>("get_mainBodyChunk"),
                i => i.MatchLdfld<BodyChunk>("pos"),
                i => i.MatchCallvirt<Room>("GetWorldCoordinate"),
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchLdfld<Room>("game"),
                i => i.MatchCallvirt<RainWorldGame>("GetNewID"),
                i => i.MatchLdcI4(0),
                i => i.MatchNewobj<AbstractSpear>(),
                i => i.MatchStloc(out loc)
                );
            c.Emit(OpCodes.Ldloc, loc);
            c.EmitDelegate((AbstractSpear asp) =>
            {
                if (OnlineManager.lobby != null)
                {
                    asp.needle = true;
                }
            });
            c.GotoNext(moveType: MoveType.After,
                // room.abstractRoom.AddEntity(abstractSpear);
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<UpdatableAndDeletable>("room"),
                i => i.MatchCallvirt<Room>("get_abstractRoom"),
                i => i.MatchLdloc(19),
                i => i.MatchCallvirt<AbstractRoom>("AddEntity")
                );
            // unset again because RealizeInRoom will assume dead
            c.Emit(OpCodes.Ldloc, loc);
            c.EmitDelegate((AbstractSpear asp) =>
            {
                if (OnlineManager.lobby != null)
                {
                    asp.needle = false;
                }
            });

            c.GotoNext(MoveType.Before,
                x => x.MatchCall<Player>(nameof(Player.FreeHand)));
            var delegate_givespeartobottomplayer = (Player self) =>
            {
                
                if (OnlineManager.lobby != null)
                {
                    if (BottomPlayerUsingSpearmasterAbility(self, out var bottom))
                    {
                        return bottom;
                    }
                }
                return self;
            };
            c.GotoNext(MoveType.After,
                x => x.MatchCall<Player>(nameof(Player.FreeHand)));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 19);
            c.EmitDelegate((int freehand, Player p, AbstractSpear spear) =>
            {
                if (OnlineManager.lobby != null)
                {
                    Player bottomplayer = delegate_givespeartobottomplayer(p);
                    if (bottomplayer != p)
                    {
                        if (bottomplayer.IsLocal())
                        {
                            freehand = bottomplayer.FreeHand();
                            if (freehand != -1 && bottomplayer.CanIPickThisUp(spear.realizedObject))
                                bottomplayer.SlugcatGrab(spear.realizedObject, freehand);
                        }
                        return -1;
                    }   
                }

                return freehand;
                
            });
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    public static bool sUpdateFood = true;

    private void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
    {
        if (OnlineManager.lobby is null || !sUpdateFood)
        {
            orig(self);
            return;
        }
        if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.isMine) return;

        var state = (PlayerState)self.State;
        var origFood = state.foodInStomach * 4 + state.quarterFoodPoints;

        orig(self);

        if (self.isNPC) return;
        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(StoryRPCs.ChangeFood, (short)(newFood - origFood));
        }
    }

    private void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
    {
        if (OnlineManager.lobby is null || !sUpdateFood)
        {
            orig(self, add);
            return;
        }

        if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.isMine) return;

        var state = (PlayerState)self.State;
        var origFood = state.foodInStomach * 4 + state.quarterFoodPoints;

        orig(self, add);

        if (self.isNPC) return;
        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(StoryRPCs.ChangeFood, (short)(newFood - origFood));
        }
    }

    private void Player_SubtractFood(On.Player.orig_SubtractFood orig, Player self, int add)
    {
        if (OnlineManager.lobby is null || !sUpdateFood)
        {
            orig(self, add);
            return;
        }

        if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.isMine) return;

        var state = (PlayerState)self.State;
        var origFood = state.foodInStomach * 4 + state.quarterFoodPoints;

        orig(self, add);

        if (self.isNPC) return;
        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(StoryRPCs.ChangeFood, (short)(newFood - origFood));
        }
    }

    private int Player_FoodInRoom(On.Player.orig_FoodInRoom_bool orig, Player self, bool eatAndDestroy)
    {
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            if (self.dead)
            {
                return self.FoodInStomach;
            }
        }
        return orig(self, eatAndDestroy);
    }

    private void Mushroom_BitByPlayer(On.Mushroom.orig_BitByPlayer orig, Mushroom self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (OnlineManager.lobby != null && !OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            if (!OnlinePhysicalObject.map.TryGetValue((grasp.grabber as Player).abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
            if (!onlineEntity.isMine) return;

            OnlineManager.lobby.owner.InvokeOnceRPC(StoryRPCs.AddMushroomCounter);
        }
    }

    private void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, creatureTemplate, realizedCreature, pos, ID);
        if (OnlineManager.lobby != null)
        {
            if (creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat && self.state == null) // please, have a state like all other creatures PLEASE
            {
                self.state = new PlayerState(self, 0, Ext_SlugcatStatsName.OnlineSessionPlayer, false);
            }
            if (self.state == null) { Error($"Missing state for {self} of type {creatureTemplate}"); }
        }
    }

    // Avatars are set as non-transferable
    private AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
    {
        if (OnlineManager.lobby != null)
        {
            sSpawningAvatar = true;
            AbstractCreature ac = OnlineManager.lobby.gameMode.SpawnAvatar(self, location);
            if (ac == null) ac = orig(self, player1, player2, player3, player4, location);
            sSpawningAvatar = false;

            return ac;
        }
        return orig(self, player1, player2, player3, player4, location);
    }

    public static ConditionalWeakTable<Player, SlugcatStats> slugcatStatsPerPlayer = new();

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (OnlineManager.lobby != null)
        {
            if (!self.abstractPhysicalObject.IsLocal(out var oe))
            {
                self.controller = new OnlineController(oe, self); // remote player
            }
            else if (oe is null)
            {
                RainMeadow.Error("player entity not found for " + self + " " + self.abstractCreature);
            }
            if (oe is not null)
            {
                slugcatStatsPerPlayer.Add(self, new SlugcatStats(self.SlugCatClass, self.slugcatStats.malnourished));
                RainMeadow.Debug($"slugcatstats:{self.SlugCatClass} owner:{oe.owner}");
            }

            // Allow glow for any non-watcher in watcher campaign
            if (ModManager.Watcher && self.room.game.session is StoryGameSession storyGameSession && self.rippleLevel > 0f && self.room != null && self.AI == null)
            {
                storyGameSession.saveState.theGlow = true;
                self.glowing = storyGameSession.saveState.theGlow || self.room.game.setupValues.playerGlowing;
            }
        }
    }

    private void Player_GetInitialSlugcatClass(On.Player.orig_GetInitialSlugcatClass orig, Player self)
    {
        orig(self);

        if (OnlineManager.lobby != null)
        {
            if (self.isNPC) return;
            if (self.abstractPhysicalObject.GetOnlineObject(out var oe))
            {
                if (oe.TryGetData<SlugcatCustomization>(out var customization))
                {
                    self.SlugCatClass = customization.playingAs;
                }
                else
                {
                    RainMeadow.Debug("no SlugcatCustomization for " + oe);
                }
            }
            else
            {
                RainMeadow.Error("player entity not found for " + self + " " + self.abstractCreature);
            }
        }
    }

    private ConditionalWeakTable<SlugcatStats, GourmandDecorator> gourmand_decorators = new();
    class GourmandDecorator : SlugcatStats {
        SlugcatStats origin;
        public GourmandDecorator(SlugcatStats slugcatStats) : base(slugcatStats.name, slugcatStats.malnourished) {
            origin = slugcatStats;
            Update();
        }

        public void Update() {
            this.throwingSkill = origin.throwingSkill;
            this.generalVisibilityBonus = origin.generalVisibilityBonus;
            this.visualStealthInSneakMode = origin.visualStealthInSneakMode;
            this.lungsFac = origin.lungsFac;
            this.throwingSkill = origin.throwingSkill;
            this.malnourished = origin.malnourished;
            this.foodToHibernate = origin.foodToHibernate;
            this.maxFood = origin.maxFood;
            this.swimBoostCooldown = origin.swimBoostCooldown;
            this.swimBoostCost = origin.swimBoostCost;
            this.swimBoostForce = origin.swimBoostForce;
            this.swimBoostMinAir = origin.swimBoostMinAir;
            this.swimForceFac = origin.swimForceFac;

            this.bodyWeightFac = origin.bodyWeightFac + 1.35f;
            this.loudnessFac = origin.loudnessFac + 1.5f;
            this.poleClimbSpeedFac = 0.8f;
            this.corridorClimbSpeedFac = 0.86f;
            this.runspeedFac = 0.9f;
        }   
    }


    private SlugcatStats Player_slugcatStatsGourmandBack(Func<Player, SlugcatStats> orig, Player self) {
        SlugcatStats stats =  orig(self);
        if (OnlineManager.lobby != null && HasSlugcatClassOnBack(self, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Gourmand, out _)) {
            var gourmstats = gourmand_decorators.GetValue(stats, _stats => new GourmandDecorator(stats));
            gourmstats.Update();
            stats = gourmstats;
        }
        return stats;
    }
    private SlugcatStats Player_slugcatStats(Func<Player, SlugcatStats> orig, Player self)
    {
        if (OnlineManager.lobby != null)
        {
            if (!self.isNPC)
            {
                if (slugcatStatsPerPlayer.TryGetValue(self, out var slugcatStats))
                {
                    return slugcatStats;
                }
            }
        }

        return orig(self);
    }

    private void PlayerOnDie(On.Player.orig_Die orig, Player self)
    {
        if (OnlineManager.lobby == null)
        {
            orig(self);
            return;
        }

        if (OnlineManager.lobby.gameMode is MeadowGameMode) return; // do not run

        if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity))
        {
            if (isArenaMode(out var _))
            {
                RainMeadow.Error("Tried to get OnlineEntity counterpart. Die() may have been called earlier");
            }
            else
            {
                throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
            }
        }
        if (onlineEntity != null && !onlineEntity.isMine) return;
        RainMeadow.Debug($"%%% DIE {onlineEntity}");
        orig(self);
    }

    private void Player_Destroy(On.Player.orig_Destroy orig, Player self)
    {
        if (OnlineManager.lobby == null)
        {
            orig(self);
            return;
        }

        if (self.slugOnBack is not null)
        {
            self.slugOnBack.DropSlug();
        }

        OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe);
        RainMeadow.Debug($"%%% DESTROY {oe}");

        orig(self);
    }

    private Player.ObjectGrabability PlayerOnGrabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (!self.abstractPhysicalObject.IsLocal()) return Player.ObjectGrabability.CantGrab;
        return orig(self, obj);
    }

    private void Player_SwallowObject(ILContext il)
    {
        try
        {
            var c = new ILCursor(il);
            var skip = il.DefineLabel();
            c.GotoNext(moveType: MoveType.AfterLabel,
                i => i.MatchLdarg(0),
                i => i.MatchLdloc(0),
                i => i.MatchStfld<Player>("objectInStomach")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((Player self, AbstractPhysicalObject objectInStomach) =>
            {
                if (OnlineManager.lobby != null && self.abstractPhysicalObject.GetOnlineObject(out var oe))
                {
                    if (!oe.isMine) return false;
                    if (objectInStomach.GetOnlineObject(out var oeInStomach))
                        oeInStomach.realized = false;  // don't release ownership
                }
                return true;
            });
            c.Emit(OpCodes.Brfalse, skip);
            c.Index = il.Instrs.Count - 1;
            c.GotoPrev(moveType: MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Player>("objectInStomach"),
                i => i.MatchLdarg(0),
                i => i.MatchCallOrCallvirt<Creature>("get_abstractCreature"),
                i => i.MatchLdfld<AbstractWorldEntity>("pos"),
                i => i.MatchCallOrCallvirt<AbstractWorldEntity>("Abstractize")
                );
            // abstractize sets pos so we gotta unset it after
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((Player self) =>
            {
                if (OnlineManager.lobby != null && self.abstractPhysicalObject.GetOnlineObject(out var oe))
                {
                    // signal not-in-a-room
                    self.objectInStomach.InDen = true;
                    self.objectInStomach.pos.WashNode();
                }
            });
            c.MarkLabel(skip);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
    {
        if (OnlineManager.lobby != null && self.abstractPhysicalObject.GetOnlineObject(out var oe))
        {
            if (!oe.isMine) return; // prevent execution
            if (self.objectInStomach != null)
            {
                self.objectInStomach.pos = self.abstractCreature.pos; // so it picks up in room.addentity hook, otherwise skipped
                self.objectInStomach.InDen = false;
            }
        }
        orig(self);
    }

    private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (!self.abstractPhysicalObject.IsLocal()) return;
        if (isArenaMode(out var arena))
        {
            if (self.grasps[grasp] == null)
            {
                return;
            }

            if (ModManager.MSC && self.grasps[grasp].grabbed is Spear && self.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Sofanthiel && self.slugcatStats.throwingSkill == 0 && !arena.painCatThrows)
            {
                self.TossObject(grasp, eu);
                self.ReleaseGrasp(grasp);
                return;
            }
        }
        orig(self, grasp, eu);
    }

    private bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (!self.isNPC)
        {
            if (isStoryMode(out var story) && obj.grabbedBy.Any(x => x.grabber is Player grabbing_player && !grabbing_player.isNPC))

                if (story.itemSteal)
                {
                    return orig(self, obj);
                }
                else
                {
                    return false;
                }
            if (isArenaMode(out var arena))
            {
                if (obj.grabbedBy.Any(x => x.grabber is Player grabbing_player))
                {
                    if (arena.itemSteal)
                    {
                        return orig(self, obj);
                    }
                    else
                    {
                        return false;
                    }
                }
                if (obj is Player pl)
                {
                    if (pl.Stunned || pl.dead)
                    {
                        return orig(self, obj);
                    };
                }

            }
        }

        if (OnlineManager.lobby != null)
        {
            if (obj is Player p)
            {
                if (!OnlineManager.lobby.gameMode.PlayersCanHandhold && !p.isNPC)
                {
                    return false;
                }
            }

        }


        return orig(self, obj);
    }

    private void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (!self.abstractPhysicalObject.IsLocal()) return;
        orig(self);
    }

    private void Player_Collide(ILContext il)
    {
        try
        {
            // if (!(otherObject as Creature).dead && (otherObject as Creature).abstractCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC && !(ModManager.CoopAvailable && flag4))
            //becomes
            // if (!(otherObject.FriendlyFireSafetyCandidate()) && !(otherObject as Creature).dead && (otherObject as Creature).abstractCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC && !(ModManager.CoopAvailable && flag4))
            var c = new ILCursor(il);
            var skip = il.DefineLabel();
            c.GotoNext(
                i => i.MatchLdarg(1),
                i => i.MatchIsinst<Creature>(),
                i => i.MatchBrfalse(out _)
                );
            c.GotoNext(
                i => i.MatchLdsfld<ModManager>("MSC"),
                i => i.MatchBrfalse(out _)
                );
            c.GotoNext(
                i => i.MatchLdarg(1),
                i => i.MatchIsinst<Creature>(),
                i => i.MatchCallOrCallvirt<Creature>("get_dead"),
                i => i.MatchBrtrue(out skip)
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((PhysicalObject otherObject) => otherObject.FriendlyFireSafetyCandidate());
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private bool Player_SlugSlamConditions(On.Player.orig_SlugSlamConditions orig, Player self, PhysicalObject otherObject)
    {
        if (otherObject.FriendlyFireSafetyCandidate())
        {
            if (otherObject is Player) return false;
        }
        if (isArenaMode(out var arena) && arena.countdownInitiatedHoldFire)
        {
            if (otherObject is Player) return false;
        }


        // check if it's somebody from below
        int i = 25;
        for (Player? bottom = self.onBack; bottom != null; bottom = bottom.onBack)
        {
            if (i-- <= 0) break;
            if (bottom == otherObject)
            {
                return false;
            }
        }

        // check if somebody from above aswell.
        i = 25;
        for (Player? top = self.slugOnBack?.slugcat; top != null; top = self.slugOnBack?.slugcat)
        {
            if (i-- <= 0) break;
            if (top == otherObject)
            {
                return false;
            }
        }


        return orig(self, otherObject);
    }

    private void Player_ClassMechanicsArtificer(ILContext il)
    {
        try
        {
            // if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer || (ExpeditionGame.explosivejump && !this.isSlugpup))
            // becomes
            // if (this.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer || HasSlugcatClassOnBack() || ...
            var c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                // 10	0017	ldarg.0
                // 11	0018	ldfld	class SlugcatStats/Name Player::SlugCatClass
                // 12	001D	ldsfld	class SlugcatStats/Name MoreSlugcats.MoreSlugcatsEnums/SlugcatStatsName::Artificer
                // 13	0022	call	bool class ExtEnum`1<class SlugcatStats/Name>::op_Equality(class ExtEnum`1<!0>, class ExtEnum`1<!0>)
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<Player>(nameof(Player.SlugCatClass)),
                x => x.MatchLdsfld<MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName>(nameof(MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer)),
                x => x.MatchCall(out _)
            );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((bool isarti, Player self) =>
            {
                return isarti || HasSlugcatClassOnBack(self, MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer, out _);
            });


            // bool flag3 = !ModManager.CoopAvailable || Custom.rainWorld.options.friendlyFire || !(room.physicalObjects[m][n] is Player player) || player.isNPC;
            //becomes
            // bool flag3 = (!isStoryMode(out _) && (!ModManager.CoopAvailable || Custom.rainWorld.options.friendlyFire)) || !(room.physicalObjects[m][n] is Player player) || player.isNPC;
            var skip = il.DefineLabel();
            c.GotoNext(MoveType.AfterLabel,
                i => i.MatchLdsfld<ModManager>("CoopAvailable"),
                i => i.MatchBrfalse(out _),
                i => i.MatchLdsfld("RWCustom.Custom", "rainWorld"),
                i => i.MatchLdfld<RainWorld>("options"),
                i => i.MatchLdfld<Options>("friendlyFire"),
                i => i.MatchBrtrue(out _)
                );
            c.EmitDelegate(() => (isStoryMode(out var story) && !story.friendlyFire) || (isArenaMode(out var arena) && (arena.countdownInitiatedHoldFire || arena.disableArtiStun)));
            c.Emit(OpCodes.Brtrue, skip);
            c.Index += 6;
            c.MarkLabel(skip);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private bool Player_CanMaulCreature(On.Player.orig_CanMaulCreature orig, Player self, Creature crit)
    {
        if (crit.FriendlyFireSafetyCandidate()) return false;
        if (isArenaMode(out var arena))
        {
            if (arena.disableMaul && crit is Player)
            {
                return false;
            }
        }
        return orig(self, crit);
    }
}
