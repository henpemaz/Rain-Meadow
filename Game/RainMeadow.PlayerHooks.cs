using System;
using System.Linq;
using UnityEngine;

namespace RainMeadow;

public partial class RainMeadow
{
    public static bool sSpawningAvatar;
    public void PlayerHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate; // Personas are set as non-transferable

        On.Player.ctor += Player_ctor;
        On.Player.Die += PlayerOnDie;
        On.Player.Grabability += PlayerOnGrabability;
        On.Player.AddFood += Player_AddFood;
        On.Player.AddQuarterFood += Player_AddQuarterFood;
        On.Mushroom.BitByPlayer += Mushroom_BitByPlayer;

        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites1;

        On.AbstractCreature.ctor += AbstractCreature_ctor;

        On.Player.Update += Player_Update1;
    }

    private void Player_Update1(On.Player.orig_Update orig, Player self, bool eu)
    {
        if (OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe))
        {
            if (RainMeadow.tracing && oe.id.originalOwner == 2)
            {

                RainMeadow.Trace($"player debug 1 {oe}");
                RainMeadow.Trace($"{oe.id}");
                RainMeadow.Trace($"{oe.owner}");
                RainMeadow.Trace($"{oe.owner.inLobbyId}");
                RainMeadow.Trace($"animation: {self.animation}");
                RainMeadow.Trace($"animationFrame: {self.animationFrame}");
                RainMeadow.Trace($"bodyMode: {self.bodyMode}");
                RainMeadow.Trace($"CollideWithObjects: {self.CollideWithObjects}");
                RainMeadow.Trace($"CollideWithSlopes: {self.CollideWithSlopes}");
                RainMeadow.Trace($"CollideWithTerrain: {self.CollideWithTerrain}");
                RainMeadow.Trace($"collisionLayer: {self.collisionLayer}");
                RainMeadow.Trace($"corridorDrop: {self.corridorDrop}");
                RainMeadow.Trace($"diveForce: {self.diveForce}");
                RainMeadow.Trace($"goIntoCorridorClimb: {self.goIntoCorridorClimb}");
                RainMeadow.Trace($"gravity: {self.gravity}");
                RainMeadow.Trace($"initSlideCounter: {self.initSlideCounter}");
                RainMeadow.Trace($"input: {self.input[0].x}");
                RainMeadow.Trace($"input: {self.input[0].y}");
                RainMeadow.Trace($"rollCounter: {self.rollCounter}");
                RainMeadow.Trace($"shootUpCounter: {self.shootUpCounter}");
                RainMeadow.Trace($"slideCounter: {self.slideCounter}");
                RainMeadow.Trace($"standing: {self.standing}");
                RainMeadow.Trace($"stopRollingCounter: {self.stopRollingCounter}");
                RainMeadow.Trace($"straightUpOnHorizontalBeam: {self.straightUpOnHorizontalBeam}");
                RainMeadow.Trace($"timeSinceInCorridorMode: {self.timeSinceInCorridorMode}");
                RainMeadow.Trace($"upperBodyFramesOffGround: {self.upperBodyFramesOffGround}");
                RainMeadow.Trace($"upperBodyFramesOnGround: {self.upperBodyFramesOnGround}");
                RainMeadow.Trace($"verticalCorridorSlideCounter: {self.verticalCorridorSlideCounter}");
                RainMeadow.Trace($"wallSlideCounter: {self.wallSlideCounter}");
                RainMeadow.Trace($"wantToJump: {self.wantToJump}");
                RainMeadow.Trace($"WANTTOSTAND: {self.WANTTOSTAND}");
                RainMeadow.Trace($"enteringShortCut.HasValue: {self.enteringShortCut.HasValue}");
                RainMeadow.Trace($"enteringShortCut.Value: {(self.enteringShortCut.HasValue ? self.enteringShortCut : "null")}");
                RainMeadow.Trace($"stun: {self.stun}");
                RainMeadow.Trace($"dead: {self.dead}");
                RainMeadow.Trace($"inShortcut: {self.inShortcut}");
            }
        }
        orig(self, eu);

        if (OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe2))
        {
            if (RainMeadow.tracing && oe2.id.originalOwner == 2)
            {
                RainMeadow.Trace($"player debug 2 {oe}");
                RainMeadow.Trace($"animation: {self.animation}");
                RainMeadow.Trace($"animationFrame: {self.animationFrame}");
                RainMeadow.Trace($"bodyMode: {self.bodyMode}");
                RainMeadow.Trace($"CollideWithObjects: {self.CollideWithObjects}");
                RainMeadow.Trace($"CollideWithSlopes: {self.CollideWithSlopes}");
                RainMeadow.Trace($"CollideWithTerrain: {self.CollideWithTerrain}");
                RainMeadow.Trace($"collisionLayer: {self.collisionLayer}");
                RainMeadow.Trace($"corridorDrop: {self.corridorDrop}");
                RainMeadow.Trace($"diveForce: {self.diveForce}");
                RainMeadow.Trace($"goIntoCorridorClimb: {self.goIntoCorridorClimb}");
                RainMeadow.Trace($"gravity: {self.gravity}");
                RainMeadow.Trace($"initSlideCounter: {self.initSlideCounter}");
                RainMeadow.Trace($"input: {self.input[0].x}");
                RainMeadow.Trace($"input: {self.input[0].y}");
                RainMeadow.Trace($"rollCounter: {self.rollCounter}");
                RainMeadow.Trace($"shootUpCounter: {self.shootUpCounter}");
                RainMeadow.Trace($"slideCounter: {self.slideCounter}");
                RainMeadow.Trace($"standing: {self.standing}");
                RainMeadow.Trace($"stopRollingCounter: {self.stopRollingCounter}");
                RainMeadow.Trace($"straightUpOnHorizontalBeam: {self.straightUpOnHorizontalBeam}");
                RainMeadow.Trace($"timeSinceInCorridorMode: {self.timeSinceInCorridorMode}");
                RainMeadow.Trace($"upperBodyFramesOffGround: {self.upperBodyFramesOffGround}");
                RainMeadow.Trace($"upperBodyFramesOnGround: {self.upperBodyFramesOnGround}");
                RainMeadow.Trace($"verticalCorridorSlideCounter: {self.verticalCorridorSlideCounter}");
                RainMeadow.Trace($"wallSlideCounter: {self.wallSlideCounter}");
                RainMeadow.Trace($"wantToJump: {self.wantToJump}");
                RainMeadow.Trace($"WANTTOSTAND: {self.WANTTOSTAND}");
                RainMeadow.Trace($"enteringShortCut.HasValue: {self.enteringShortCut.HasValue}");
                RainMeadow.Trace($"enteringShortCut.Value: {(self.enteringShortCut.HasValue ? self.enteringShortCut : "null")}");
                RainMeadow.Trace($"stun: {self.stun}");
                RainMeadow.Trace($"dead: {self.dead}");
                RainMeadow.Trace($"inShortcut: {self.inShortcut}");

                RainMeadow.Trace(Environment.StackTrace);
            }
        }
    }

    private void PlayerGraphics_DrawSprites1(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
    {
        if(OnlineManager.lobby != null)
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

    private void Player_AddQuarterFood(On.Player.orig_AddQuarterFood orig, Player self)
    {
        orig(self);

        if (OnlineManager.lobby != null)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
            if (!onlineEntity.isMine) return;

            if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                OnlineManager.lobby.owner.InvokeRPC(RPCs.AddQuarterFood);
            }
        }
    }

    private void Player_AddFood(On.Player.orig_AddFood orig, Player self, int add)
    {
        orig(self, add);

        if (OnlineManager.lobby != null) {
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
            if (!onlineEntity.isMine) return;

            if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                OnlineManager.lobby.owner.InvokeRPC(RPCs.AddFood, (short)add);
            }
        }
    }

    private void Mushroom_BitByPlayer(On.Mushroom.orig_BitByPlayer orig, Mushroom self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            OnlineManager.lobby.owner.InvokeRPC(RPCs.AddMushroomCounter);
        }
    }

    private void AbstractCreature_ctor(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, creatureTemplate, realizedCreature, pos, ID);
        if (OnlineManager.lobby != null)
        {
            if (creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat && self.state == null) // please, have a state like all other creatures PLEASE
            {
                self.state = new PlayerState(self, 0, Ext_SlugcatStatsName.OnlineSessionRemotePlayer, false);
            }
            if (self.state == null) { Error($"Missing state for {self} of type {creatureTemplate}"); }
        }
    }

    // Avatars are set as non-transferable
    private AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
    {
        if (OnlineManager.lobby != null)
        {
            // this could probably be reworked to be a single callback to gamemode, instead of 2
            sSpawningAvatar = true;
            AbstractCreature ac = OnlineManager.lobby.gameMode.SpawnAvatar(self, location);
            if (ac == null) ac = orig(self, player1, player2, player3, player4, location);
            sSpawningAvatar = false;

            if(OnlineCreature.map.TryGetValue(ac, out var onlineCreature))
            {
                OnlineManager.lobby.gameMode.SetAvatar(onlineCreature as OnlineCreature);
            }
            else
            {
                throw new InvalidProgrammerException($"Can't find OnlineCreature for {ac}");
            }

            return ac;
        }
        return orig(self, player1, player2, player3, player4, location);
    }

    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if (OnlineManager.lobby != null)
        {
            if(OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var ent))
            {
                // remote player
                if (!ent.isMine)
                {
                    self.controller = new OnlineController(ent, self);
                }
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

        if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.isMine) return;
        if (isStoryMode(out var story))
        {
            story.storyClientSettings.isDead = true;
        }
        orig(self);
    }

    private Player.ObjectGrabability PlayerOnGrabability(On.Player.orig_Grabability orig, Player self, PhysicalObject obj)
    {
        if (OnlineManager.lobby != null)
        {
            if (self.playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer)
            {
                return Player.ObjectGrabability.CantGrab;
            }
        }
        return orig(self, obj);
    }

    private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {

        if (isStoryMode(out var storyGameMode))
        {

            if (OnlineManager.lobby == null) return;

            if ((storyGameMode.clientSettings as StoryClientSettings).playingAs == Ext_SlugcatStatsName.OnlineStoryWhite)
            {
                slugcat = SlugcatStats.Name.White;
            }
            else if ((storyGameMode.clientSettings as StoryClientSettings).playingAs == Ext_SlugcatStatsName.OnlineStoryYellow)
            {
                slugcat = SlugcatStats.Name.Yellow;
            }
            else if ((storyGameMode.clientSettings as StoryClientSettings).playingAs == Ext_SlugcatStatsName.OnlineStoryRed)
            {
                slugcat = SlugcatStats.Name.Red;
            }

            orig(self, slugcat, malnourished);

            // Override for all players
            if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryWhite)
            {
                self.maxFood = 7;
                self.foodToHibernate = 4;
            }
            else if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryYellow)
            {
                self.maxFood = 5;
                self.foodToHibernate = 3;
            }
            else if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryRed)
            {
                self.maxFood = 9;
                self.foodToHibernate = 6;
            }


        }

        else
        {

            orig(self, slugcat, malnourished);

            if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode)
            {
                self.throwingSkill = 1;
            }
        }


    }
}