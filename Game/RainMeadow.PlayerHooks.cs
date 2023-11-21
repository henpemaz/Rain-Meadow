namespace RainMeadow;

public partial class RainMeadow
{
    public static bool sSpawningPersonas;
    public void PlayerHooks()
    {
        On.RainWorldGame.SpawnPlayers_bool_bool_bool_bool_WorldCoordinate += RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate; // Personas are set as non-transferable

        On.Player.ctor += Player_ctor;
        On.Player.Die += PlayerOnDie;
        On.Player.Grabability += PlayerOnGrabability;
        On.Player.AddFood += Player_AddFood;
        On.Player.AddQuarterFood += Player_AddQuarterFood;

        On.SlugcatStats.ctor += SlugcatStatsOnctor;
        On.AbstractCreature.ctor += AbstractCreature_ctor;
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

        if (OnlineManager.lobby != null)
        {
            if (!OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
            if (!onlineEntity.isMine) return;

            if (!OnlineManager.lobby.isOwner && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                OnlineManager.lobby.owner.InvokeRPC(RPCs.AddFood, add);
            }
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

    // Personas are set as non-transferable
    private AbstractCreature RainWorldGame_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate(On.RainWorldGame.orig_SpawnPlayers_bool_bool_bool_bool_WorldCoordinate orig, RainWorldGame self, bool player1, bool player2, bool player3, bool player4, WorldCoordinate location)
    {
        if (OnlineManager.lobby != null)
        {
            sSpawningPersonas = true;
            AbstractCreature ac = OnlineManager.lobby.gameMode.SpawnPersona(self, location);
            if (ac == null) ac = orig(self, player1, player2, player3, player4, location);
            sSpawningPersonas = false;
            if (OnlineManager.lobby.gameMode.personaSettings != null && OnlineCreature.map.TryGetValue(ac, out var onlinePersona))
            {
                OnlineManager.lobby.gameMode.personaSettings.BindEntity(onlinePersona);
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
            // remote player
            if (OnlinePhysicalObject.map.TryGetValue(self.abstractPhysicalObject, out var ent) && self.playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer)
            {
                self.controller = new OnlineController(ent, self);
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

    private void SlugcatStatsOnctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
    {
        orig(self, slugcat, malnourished);

        if (OnlineManager.lobby == null) return;
        if (slugcat != Ext_SlugcatStatsName.OnlineSessionPlayer && slugcat != Ext_SlugcatStatsName.OnlineSessionRemotePlayer) return;

        if (OnlineManager.lobby.gameMode is StoryGameMode or ArenaCompetitiveGameMode or FreeRoamGameMode)
        {
            self.throwingSkill = 1;
        }
    }
}