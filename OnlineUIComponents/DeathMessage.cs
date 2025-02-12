using MoreSlugcats;
using System;
using System.Linq;

namespace RainMeadow;

public static class DeathMessage
{
    public static void EnvironmentalRPC(Player player, DeathType cause)
    {
        var opo = player.abstractPhysicalObject.GetOnlineObject();
        if (opo == null) return;
        foreach(var op in OnlineManager.players)
        {
            op.InvokeRPC(RPCs.KillFeedEnvironment, opo, (int)cause);
        }
    }
    public static void PvPRPC(Player killer, Creature target, int context)
    {
        var kopo = killer.abstractPhysicalObject.GetOnlineObject();
        var topo = target.abstractPhysicalObject.GetOnlineObject();
        if (kopo == null || topo == null) return;
        foreach (var op in OnlineManager.players)
        {
            op.InvokeRPC(RPCs.KillFeedPvP, kopo, topo, context);
        }
    }
    public static void CvPRPC(Creature killer, Player target)
    {
        var kopo = killer.abstractPhysicalObject.GetOnlineObject();
        var topo = target.abstractPhysicalObject.GetOnlineObject();
        if (kopo == null || topo == null) return;
        foreach (var op in OnlineManager.players)
        {
            op.InvokeRPC(RPCs.KillFeedCvP, kopo, topo);
        }
    }
    public static bool ShouldShowDeath(OnlinePhysicalObject opo)
    {
        if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is RainWorldGame game)
        {
            var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                if (onlineHud.killFeed.Contains(opo.id))
                {
                    return false;
                }
            }
            return true;
        }
        return false;
    }
    public static void EnvironmentalDeathMessage(AbstractCreature abstractCreature, DeathType cause)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            var opo = abstractCreature.GetOnlineObject();
            if (opo == null || abstractCreature.creatureTemplate.type != CreatureTemplate.Type.Slugcat || !ShouldShowDeath(opo))
            {
                return;
            }
            var t = opo.owner.id.name;
            switch (cause)
            {
                default:
                    ChatLogManager.LogMessage("", $"{t} died.");
                    break;
                case DeathType.Rain:
                    ChatLogManager.LogMessage("", $"{t} was crushed by the rain.");
                    break;
                case DeathType.Abyss:
                    ChatLogManager.LogMessage("", $"{t} fell into the abyss.");
                    break;
                case DeathType.Drown:
                    if (abstractCreature.realizedCreature != null && abstractCreature.realizedCreature.grabbedBy.Count > 0)
                    {
                        ChatLogManager.LogMessage("", $"{t} was drowned by {abstractCreature.realizedCreature.grabbedBy[0].grabber.Template.name}.");
                        break;
                    }
                    ChatLogManager.LogMessage("", $"{t} drowned.");
                    break;
                case DeathType.FallDamage:
                    ChatLogManager.LogMessage("", $"{t} hit the ground too hard.");
                    break;
                case DeathType.Oracle:
                    ChatLogManager.LogMessage("", $"{t} was killed through unknown means.");
                    break;
                case DeathType.Burn:
                    ChatLogManager.LogMessage("", $"{t} tried to swim in burning liquid.");
                    break;
                case DeathType.PyroDeath:
                    ChatLogManager.LogMessage("", $"{t} spontaneously combusted.");
                    break;
                case DeathType.Freeze:
                    ChatLogManager.LogMessage("", $"{t} froze to death.");
                    break;
                case DeathType.WormGrass:
                    ChatLogManager.LogMessage("", $"{t} was swallowed by the grass.");
                    break;
                case DeathType.WallRot:
                    ChatLogManager.LogMessage("", $"{t} was swallowed by the walls.");
                    break;
                case DeathType.Electric:
                    ChatLogManager.LogMessage("", $"{t} was electrocuted.");
                    break;
                case DeathType.DeadlyLick:
                    ChatLogManager.LogMessage("", $"{t} licked the power.");
                    break;
                case DeathType.Coalescipede:
                    ChatLogManager.LogMessage("", $"{t} was consummed by the swarm.");
                    break;
            }
            var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                onlineHud.killFeed.Add(opo.id);
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }
    public static void PlayerKillPlayer(AbstractCreature killer, AbstractCreature target, int context)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            var opo = target.GetOnlineObject();
            if (target == null || killer == null || !ShouldShowDeath(opo))
            {
                return;
            }
            var k = killer.GetOnlineObject().owner.id.name;
            var t = target.GetOnlineObject().owner.id.name;
            switch(context)
            {
                default:
                    ChatLogManager.LogMessage("", $"{t} was slain by {k}.");
                    break;
                case 1:
                    ChatLogManager.LogMessage("", $"{t} was ascended by {k}.");
                    break;
            }
            
            var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                onlineHud.killFeed.Add(opo.id);
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void CreatureKillPlayer(Creature killer, Player target)
    {
        if (killer is Player)
        {
            return;
        }

        try
        {
            var k = killer.Template.name;
            var opo = target.abstractPhysicalObject.GetOnlineObject();
            var t = opo.owner.id.name;
            if (!ShouldShowDeath(opo)) return;
            if (killer.Template.TopAncestor().type == CreatureTemplate.Type.Centipede)
            {
                ChatLogManager.LogMessage("", $"{t} was zapped by a {k}.");
            } 
            else
            {
                ChatLogManager.LogMessage("", $"{t} was slain by a {k}.");
            }

            var onlineHuds = target.room.game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                onlineHud.killFeed.Add(opo.id);
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void PlayerKillCreature(AbstractCreature killer, AbstractCreature target, int context)
    {
        if (target.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
        {
            PlayerKillPlayer(killer, target, context);
            return;
        }
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            var k = killer.GetOnlineObject().owner.id.name;
            var t = target.creatureTemplate.name;
            var opo = target.GetOnlineObject();
            if (!ShouldShowDeath(opo)) return;
            if (target.realizedCreature != null && target.realizedCreature.TotalMass > 0.2f) ChatLogManager.LogMessage("", $"{t} was slain by {k}.");

            if (opo != null)
            {
                var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

                foreach (var onlineHud in onlineHuds)
                {
                    onlineHud.killFeed.Add(opo.id);
                }
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void PlayerDeathEvent(Player player, Type sourceType, object source)
    {
        if (OnlineManager.lobby == null || OnlineManager.lobby.gameMode is MeadowGameMode) return;
        if (player.dead) return;
        switch(source)
        {
            case ZapCoil:
                EnvironmentalRPC(player, DeathType.Electric);
                break;
            case WormGrass.WormGrassPatch:
                EnvironmentalRPC(player, DeathType.WormGrass);
                break;
            case SSOracleBehavior:
                EnvironmentalRPC(player, DeathType.Oracle);
                break;
            case DaddyCorruption.EatenCreature:
                EnvironmentalRPC(player, DeathType.WallRot);
                break;
            case Player.Tongue:
                EnvironmentalRPC(player, DeathType.DeadlyLick);
                break;
        }
    }

    public static void CreatureDeath(Creature crit)
    {
        if (crit.killTag != null && crit.killTag.realizedCreature != null)
        {
            if (crit.killTag.realizedCreature is Player && !RainMeadow.isArenaMode(out var _))
            {
                PvPRPC(crit.killTag.realizedCreature as Player, crit, 0);
            }
            else if (crit is Player)
            {
                //CreatureKillPlayer(crit.killTag.realizedCreature, crit as Player);
                CvPRPC(crit.killTag.realizedCreature, crit as Player);
            }
        }
        else
        {
            // (try to) Determine the cause of death if it wasn't from a kill.
            // Will probably be way better to have this information sent by the client that actually died for accuracy but this should work good enough for now.
            if (crit is Player player)
            {
                if (player.drown >= 1f)
                {
                    EnvironmentalRPC(player, DeathType.Drown);
                    return;
                }
                if (player.Hypothermia >= 1f)
                {
                    EnvironmentalRPC(player, DeathType.Freeze);
                    return;
                }
                if (player.rainDeath > 1f)
                {
                    EnvironmentalRPC(player, DeathType.Rain);
                    return;
                }

                if (ModManager.MSC && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                {
                    if (player.airInLungs <= Player.PyroDeathThreshold(player.room.game))
                    {
                        EnvironmentalRPC(player, DeathType.PyroDeath);
                        return;
                    }
                }

                if (player.Submersion > 0.2f && player.room.waterObject != null && player.room.waterObject.WaterIsLethal && !player.abstractCreature.lavaImmune)
                {
                    EnvironmentalRPC(player, DeathType.Burn);
                    return;
                }

                if (player.grabbedBy.Count > 0)
                {
                    float spiders = 0f;
                    for (int i = 0; i < player.grabbedBy.Count; i++)
                    {
                        if (player.grabbedBy[i].grabber is Spider)
                        {
                            spiders+= player.grabbedBy[i].grabber.TotalMass;
                        }
                    }
                    if (spiders >= player.TotalMass)
                    {
                        EnvironmentalRPC(player, DeathType.Coalescipede);
                    }
                }
            }
        }
    }

    public enum DeathType
    {
        Invalid,
        Rain,
        Abyss,
        Drown,
        FallDamage,
        Oracle,
        Burn,
        PyroDeath,
        Freeze,
        WormGrass,
        WallRot,
        Electric,
        DeadlyLick,
        Coalescipede
    }
}
