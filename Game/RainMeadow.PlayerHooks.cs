namespace RainMeadow;

partial class RainMeadow
{
    public void PlayerHooks()
    {
        On.Player.ctor += Player_ctor;
        On.Player.Die += PlayerOnDie;
        
        On.SlugcatStats.ctor += SlugcatStatsOnctor;
    }
    
    private void Player_ctor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);
        if(OnlineManager.lobby != null)
        {
            // remote player
            if (OnlineEntity.map.TryGetValue(self.abstractPhysicalObject, out var ent) && self.playerState.slugcatCharacter == Ext_SlugcatStatsName.OnlineSessionRemotePlayer)
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

        if (!OnlineEntity.map.TryGetValue(self.abstractPhysicalObject, out var onlineEntity)) throw new InvalidProgrammerException("Player doesn't have OnlineEntity counterpart!!");
        if (!onlineEntity.owner.isMe) return;
        orig(self);
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