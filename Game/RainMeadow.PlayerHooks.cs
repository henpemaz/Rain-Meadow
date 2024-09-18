using System;

namespace RainMeadow;

public partial class RainMeadow
{
    public static bool sSpawningAvatar;
    public void PlayerHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate; // Personas are set as non-transferable

        On.SlugcatStats.ctor += SlugcatStats_ctor;

        On.Player.ctor += Player_ctor;
        On.Player.Die += PlayerOnDie;
        On.Player.Grabability += PlayerOnGrabability;
        On.Player.SwallowObject += Player_SwallowObject;
        On.Player.Regurgitate += Player_Regurgitate;
        On.Player.AddFood += Player_AddFood;
        On.Player.AddQuarterFood += Player_AddQuarterFood;
        On.Player.FoodInRoom_bool += Player_FoodInRoom;
        On.Mushroom.BitByPlayer += Mushroom_BitByPlayer;
        On.KarmaFlower.BitByPlayer += KarmaFlower_BitByPlayer;
        On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites1;

        On.AbstractCreature.ctor += AbstractCreature_ctor;
    }

    private void KarmaFlower_BitByPlayer(On.KarmaFlower.orig_BitByPlayer orig, KarmaFlower self, Creature.Grasp grasp, bool eu)
    {
        orig(self, grasp, eu);
        if (OnlineManager.lobby != null && OnlineManager.lobby.gameMode is StoryGameMode)
        {
            if (self.bites < 1) {
                if (!OnlineManager.lobby.isOwner)
                {
                    OnlineManager.lobby.owner.InvokeRPC(RPCs.ReinforceKarma);
                }
                foreach (OnlinePlayer player in OnlineManager.players)
                {
                    if (!player.isMe)
                    {
                        player.InvokeRPC(RPCs.PlayReinforceKarmaAnimation);
                    }
                }
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

    private int Player_FoodInRoom(On.Player.orig_FoodInRoom_bool orig, Player self, bool eatAndDestroy)
    {
        if (OnlineManager.lobby != null  && OnlineManager.lobby.gameMode is StoryGameMode) {
            if (self.dead) {
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
        if (onlineEntity == null) // Handle falling out of world gracefully
        {
            orig(self);
            return;
        }
        if (!onlineEntity.isMine) return;
        if (isStoryMode(out var story))
        {
            story.storyClientSettings.isDead = true;
        }
        if (OnlineManager.lobby.gameMode is MeadowGameMode) return; // do not run
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

    private void Player_SwallowObject(On.Player.orig_SwallowObject orig, Player self, int grasp)
    {
        if (OnlineManager.lobby != null)
        {
            if (OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe)
                && !oe.isMine)
            {
                return;
            }
        }
        orig(self, grasp);
    }

    private void Player_Regurgitate(On.Player.orig_Regurgitate orig, Player self)
    {
        if (OnlineManager.lobby != null)
        {
            if (OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var oe)
                && !oe.isMine)
            {
                return;
            }
        }
        orig(self);
    }

    private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {

        if (isStoryMode(out var storyGameMode))
        {
            slugcat = (storyGameMode.clientSettings as StoryClientSettings).playingAs;
        }
        orig(self, slugcat, malnourished);

        if (OnlineManager.lobby == null) return;
        if (slugcat != Ext_SlugcatStatsName.OnlineSessionPlayer && slugcat != Ext_SlugcatStatsName.OnlineSessionRemotePlayer) return;

        if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode or FreeRoamGameMode)
        {
            self.throwingSkill = 1;
        }
    }
}
