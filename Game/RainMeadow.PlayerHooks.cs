using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Linq;
using MonoMod.RuntimeDetour;
using System.Runtime.CompilerServices;
using RWCustom;
using UnityEngine;

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
        On.Player.SlugOnBack.DropSlug += Player_JumpOffOfBack;
        On.Player.CanIPutDeadSlugOnBack += Player_CanIPutDeadSlugOnBack;
        On.Player.CanEatMeat += Player_CanEatMeat;
        On.Player.ThrownSpear += Player_ThrownSpear;

        // IL.Player.GrabUpdate += Player_SynchronizeSocialEventDrop;
        // IL.Player.TossObject += Player_SynchronizeSocialEventDrop;
        // IL.Player.ReleaseObject += Player_SynchronizeSocialEventDrop;
    }

    private void Player_ThrownSpear(On.Player.orig_ThrownSpear orig, Player self, Spear spear)
    {
        if (self.slugcatStats.throwingSkill == 0)
        {
            RainMeadow.Debug("WEAK+");
        }
        RainMeadow.Debug("PRESPEARDMG" + spear.spearDamageBonus);
        if (self.IsLocal())
        {
            foreach (var player in OnlineManager.players)
            {
                if (!player.isMe)
                {
                    player.InvokeRPC(RPCs.ThrownSpear, self.abstractPhysicalObject.GetOnlineObject(), spear.abstractPhysicalObject.GetOnlineObject());
                }
            }
        }
        orig(self, spear);
        RainMeadow.Debug("POST" + spear.spearDamageBonus);
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

    private void Player_JumpOffOfBack(On.Player.SlugOnBack.orig_DropSlug orig, Player.SlugOnBack self)
    {
        var slugcat = self.slugcat;
        orig(self);

        if (OnlineManager.lobby == null) return;
        if (slugcat == null) return;

        if (slugcat.IsLocal())
        {
            if (!slugcat.isNPC && slugcat.input[0].jmp)
            {
                slugcat.jumpChunk = self.owner.mainBodyChunk;
                slugcat.JumpOnChunk();
            }
        }

    }

    // Player Quick Piggy Backing from Stick Together Co-Op by WillowWisp 
    private void Player_GrabUpdatePiggyBack(On.Player.orig_GrabUpdate orig, Player self, bool eu)
    {
        orig(self, eu);
        if (OnlineManager.lobby == null) return;
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
                if (obj is Player other && other.IsLocal())
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

                    other.slugOnBack?.SlugToBack(self);
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
            if (self.slugcat.input[0].jmp) self.owner.slugOnBack.DropSlug(); //NOTE: makes self.slugcat null!
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
        if (self.IsLocal())
        {
            RainMeadow.Debug("dmg track " + self.playerState.permanentDamageTracking);

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
            c.EmitDelegate((Player self) => self.abstractPhysicalObject.IsLocal());
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

    // TODO: toggleable friendly steal
    private bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (!self.isNPC)
        {
            if (isStoryMode(out _) && obj.grabbedBy.Any(x => x.grabber is Player grabbing_player && !grabbing_player.isNPC)) return false;
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
        return orig(self, otherObject);
    }

    private void Player_ClassMechanicsArtificer(ILContext il)
    {
        try
        {
            // bool flag3 = !ModManager.CoopAvailable || Custom.rainWorld.options.friendlyFire || !(room.physicalObjects[m][n] is Player player) || player.isNPC;
            //becomes
            // bool flag3 = (!isStoryMode(out _) && (!ModManager.CoopAvailable || Custom.rainWorld.options.friendlyFire)) || !(room.physicalObjects[m][n] is Player player) || player.isNPC;
            var c = new ILCursor(il);
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
