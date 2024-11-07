using Mono.Cecil.Cil;
using MonoMod.Cil;
using RainMeadow.Arena.Nightcat;
using System;
using System.Drawing;
using System.Linq;
using UnityEngine;

namespace RainMeadow;

public partial class RainMeadow
{
    public static bool sSpawningAvatar;
    public void PlayerHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate; // Personas are set as non-transferable

        On.SlugcatStats.ctor += SlugcatStats_ctor;

        On.Player.ctor += Player_ctor;
        On.Player.GetInitialSlugcatClass += Player_GetInitialSlugcatClass;
        IL.Player.Update += Player_Update;
        On.Player.Update += Player_Update1;
        On.Player.Die += PlayerOnDie;
        On.Player.Destroy += Player_Destroy;
        On.Player.Grabability += PlayerOnGrabability;
        IL.Player.GrabUpdate += Player_GrabUpdate;
        IL.Player.SwallowObject += Player_SwallowObject;
        On.Player.Regurgitate += Player_Regurgitate;
        On.Player.ThrowObject += Player_ThrowObject;
        On.Player.CanIPickThisUp += Player_CanIPickThisUp;
        On.Player.SpitUpCraftedObject += Player_SpitUpCraftedObject;
        IL.Player.Collide += Player_Collide;
        On.Player.SlugSlamConditions += Player_SlugSlamConditions;
        IL.Player.ClassMechanicsArtificer += Player_ClassMechanicsArtificer;
        On.Player.CanMaulCreature += Player_CanMaulCreature;
        On.Player.AddFood += Player_AddFood;
        On.Player.AddQuarterFood += Player_AddQuarterFood;
        On.Player.SubtractFood += Player_SubtractFood;
        On.Player.FoodInRoom_bool += Player_FoodInRoom;
        On.Mushroom.BitByPlayer += Mushroom_BitByPlayer;
        On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites1;

        On.AbstractCreature.ctor += AbstractCreature_ctor;
        On.Player.ShortCutColor += Player_ShortCutColor;
        On.Player.checkInput += Player_checkInput;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites2;

    }


    private void PlayerGraphics_DrawSprites2(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig(self, sLeaser, rCam, timeStacker, camPos);


        //if (isArenaMode(out var arena) && self.player.SlugCatClass == SlugcatStats.Name.Night)
        //{

        //    Nightcat.NightcatImplementation(arena, self, sLeaser, rCam, timeStacker, camPos);
        //}


    }

    private void Player_checkInput(On.Player.orig_checkInput orig, Player self)
    {
        orig(self);
        if (OnlineManager.lobby != null)
        {
            if (self.room.world.game.cameras[0].hud.textPrompt.pausedMode || ChatHud.chatButtonActive)
            {
                PlayerMovementOverride.StopPlayerMovement(self);
            }

            if (isArenaMode(out var arena))
            {
                if (arena.countdownInitiatedHoldFire)
                {
                    PlayerMovementOverride.HoldFire(self);
                }

                ArenaHelpers.OverideSlugcatClassAbilities(self, arena);

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
        if (isStoryMode(out var gameMode) && self.abstractCreature.IsLocal())
            gameMode.storyClientData.readyForWin = false;
        orig(self, eu);
        if (isStoryMode(out var _) && !self.inShortcut && OnlineManager.players.Count > 4)
        {
            if (self.room.abstractRoom.shelter || self.room.IsGateRoom())
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
                    OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.ReinforceKarma);
                }
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeOnceRPC(RPCs.PlayReinforceKarmaAnimation);
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

        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(RPCs.ChangeFood, (short)(newFood - origFood));
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

        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(RPCs.ChangeFood, (short)(newFood - origFood));
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

        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            var newFood = state.foodInStomach * 4 + state.quarterFoodPoints;
            if (newFood != origFood) OnlineManager.lobby.owner.InvokeRPC(RPCs.ChangeFood, (short)(newFood - origFood));
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

            OnlineManager.lobby.owner.InvokeOnceRPC(RPCs.AddMushroomCounter);
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
        }
    }

    private void Player_GetInitialSlugcatClass(On.Player.orig_GetInitialSlugcatClass orig, Player self)
    {
        orig(self);

        if (OnlineManager.lobby != null)
        {
            if (self.SlugCatClass == SlugcatStats.Name.Night)
            {
                self.slugcatStats.throwingSkill = 1; // don't let them push you around
                // Nightcat.ResetSneak(self);
            }

            if (self.abstractPhysicalObject.GetOnlineObject(out var oe) && oe.TryGetData<SlugcatCustomization>(out var customization))
            {
                self.SlugCatClass = customization.playingAs;
            }
            else
            {
                RainMeadow.Error("player entity not found for " + self + " " + self.abstractCreature);
            }
        }
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
                        oeInStomach.realized = false; // don't release ownership
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
                    self.objectInStomach.pos.room = -1; // signal not-in-a-room
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
            if (self.objectInStomach != null) self.objectInStomach.pos = self.abstractCreature.pos; // so it picks up in room.addentity hook, otherwise skipped
        }
        orig(self);
    }

    private void Player_ThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
    {
        if (!self.abstractPhysicalObject.IsLocal()) return;
        orig(self, grasp, eu);
    }

    // TODO: toggleable friendly steal
    private bool Player_CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
    {
        if (isStoryMode(out _) && obj.grabbedBy.Any(x => x.grabber is Player)) return false;
        return orig(self, obj);
    }

    private void Player_SpitUpCraftedObject(On.Player.orig_SpitUpCraftedObject orig, Player self)
    {
        if (!self.abstractPhysicalObject.IsLocal()) return;
        orig(self);
    }

    // TODO: toggleable friendly fire
    private void Player_Collide(ILContext il)
    {
        try
        {
            // if (!(otherObject as Creature).dead && (otherObject as Creature).abstractCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC && !(ModManager.CoopAvailable && flag4))
            //becomes
            // if (!(isStoryMode(out _) && otherObject is Player) && !(otherObject as Creature).dead && (otherObject as Creature).abstractCreature.creatureTemplate.type != MoreSlugcatsEnums.CreatureTemplateType.SlugNPC && !(ModManager.CoopAvailable && flag4))
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
            c.EmitDelegate((PhysicalObject otherObject) => isStoryMode(out var story) && !story.friendlyFire && otherObject is Player);
            c.Emit(OpCodes.Brtrue, skip);
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }

    private bool Player_SlugSlamConditions(On.Player.orig_SlugSlamConditions orig, Player self, PhysicalObject otherObject)
    {
        if (isStoryMode(out var story) && !story.friendlyFire)
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
            c.EmitDelegate(() => isStoryMode(out var story) && !story.friendlyFire);
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
        if (isStoryMode(out var story) && !story.friendlyFire)
        {
            if (crit is Player) return false;
        }
        if (isArenaMode(out var arena) && arena.countdownInitiatedHoldFire)
        {
            if (crit is Player) return false;
        }
        return orig(self, crit);
    }

    // this WHOLE hook might be obsolete, todo check
    private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {
        if (isStoryMode(out var storyGameMode))
        {
            slugcat = storyGameMode.avatarSettings.playingAs;
        }

        orig(self, slugcat, malnourished);

        if (OnlineManager.lobby == null) return;
        if (slugcat != Ext_SlugcatStatsName.OnlineSessionPlayer) return;


    }
}
