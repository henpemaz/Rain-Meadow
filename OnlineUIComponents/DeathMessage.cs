using MoreSlugcats;
using RainMeadow.Game;
using System;
using System.Linq;
using Watcher;

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
        if (target is not Player && target.TotalMass < 0.2f) return;
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
    public static void EnvironmentalDeathMessage(OnlinePhysicalObject opo, DeathType cause)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            if (opo == null || !ShouldShowDeath(opo))
            {
                return;
            }
            var t = opo.owner.id.name;
            switch (cause)
            {
                default:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("died."));
                    break;
                case DeathType.Rain:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was crushed by the rain."));
                    break;
                case DeathType.Abyss:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("fell into the abyss."));
                    break;
                case DeathType.Drown:
                    if ((opo.apo as AbstractCreature).realizedCreature != null && (opo.apo as AbstractCreature).realizedCreature.grabbedBy.Count > 0)
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was drowned by") + " " + Utils.Translate((opo.apo as AbstractCreature).realizedCreature.grabbedBy[0].grabber.Template.name) + ".");
                        break;
                    }
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("drowned."));
                    break;
                case DeathType.FallDamage:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("hit the ground too hard."));
                    break;
                case DeathType.Oracle:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was killed through unknown means."));
                    break;
                case DeathType.Burn:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("tried to swim in burning liquid."));
                    break;
                case DeathType.PyroDeath:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("spontaneously combusted."));
                    break;
                case DeathType.Freeze:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("froze to death."));
                    break;
                case DeathType.WormGrass:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was swallowed by the grass."));
                    break;
                case DeathType.WallRot:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was swallowed by the walls."));
                    break;
                case DeathType.Electric:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was electrocuted."));
                    break;
                case DeathType.DeadlyLick:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("licked the power."));
                    break;
                case DeathType.Coalescipede:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was consumed by the swarm."));
                    break;
                case DeathType.UnderwaterShock:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was electrocuted in the water."));
                    break;

                // WATCHER

                case DeathType.Sandstorm:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was obliterated by the sandstorm."));
                    break;
                case DeathType.Poison:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("died from poison."));
                    break;
                case DeathType.Lightning:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was struck by lightning."));
                    break;
                case DeathType.Locust:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was consumed by locusts."));
                    break;
                case DeathType.Ripple:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("swam with the ripples."));
                    break;
                case DeathType.Pomegranate:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("experienced the force of fresh fruit."));
                    break;
                case DeathType.Fire:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was burnt to a crisp."));
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
    public static void PlayerKillPlayer(OnlinePhysicalObject killer, OnlinePhysicalObject target, int context)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            if (target == null || killer == null || !ShouldShowDeath(target))
            {
                return;
            }
            var k = killer.owner.id.name;
            var t = target.owner.id.name;
            switch(context)
            {
                default:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was slain by") + $" {k}.");
                    break;
                case 1:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was ascended by") + $" {k}.");
                    break;
            }
            
            var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                onlineHud.killFeed.Add(target.id);
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void CreatureKillPlayer(OnlinePhysicalObject killer, OnlinePhysicalObject target)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            var k = (killer.apo as AbstractCreature).creatureTemplate.name;
            var t = target.owner.id.name;
            if (!ShouldShowDeath(target)) return;
            if ((killer.apo as AbstractCreature).creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Centipede)
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was zapped by a") + " " + Utils.Translate(k) + ".");
            } 
            else
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was slain by a") + " " + Utils.Translate(k) + ".");
            }

            var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

            foreach (var onlineHud in onlineHuds)
            {
                onlineHud.killFeed.Add(target.id);
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void PlayerKillCreature(OnlinePhysicalObject killer, OnlinePhysicalObject target, int context)
    {
        /* don't think we need this anymore...
        if (target.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
        {
            PlayerKillPlayer(killer, target, context);
            return;
        }
        */
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            var k = killer.owner.id.name;
            var t = (target.apo as AbstractCreature).creatureTemplate.name;
            var realized = (target.apo as AbstractCreature).realizedCreature;
            if (!ShouldShowDeath(target)) return;
            switch (context)
            {
                default:
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate("was slain by") + $" {k}.");
                    break;
                case 1:
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate("was ascended by") + $" {k}.");
                    break;
            }

            if (target != null)
            {
                var onlineHuds = game.cameras[0].hud.parts.OfType<PlayerSpecificOnlineHud>();

                foreach (var onlineHud in onlineHuds)
                {
                    onlineHud.killFeed.Add(target.id);
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
            case ARKillRect:
                EnvironmentalRPC(player, DeathType.Invalid);
                break;
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
            case LocustSystem.Swarm:
                EnvironmentalRPC(player, DeathType.Locust);
                break;
            case Player:
                if (ModManager.Watcher && player.rippleDeathTime > 80)
                {
                    EnvironmentalRPC(player, DeathType.Ripple);
                }
                break;
        }
    }

    public static void CreatureDeath(Creature crit)
    {
        if (crit.killTag != null && crit.killTag.realizedCreature != null)
        {
            if (crit.killTag.realizedCreature is Player)
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
                if (player.injectedPoison / player.Template.instantDeathDamageLimit >= 1f)
                {
                    EnvironmentalRPC(player, DeathType.Poison);
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
                        return;
                    }
                }
                // If all else fails check the last cause of violence.
                DeathContextualizer.lastViolence.TryGetValue(player, out var violentAction);
                if (violentAction == null)
                {
                    return;
                }
                // thanks .NET for not supporting using type variables in switch statements
                switch (violentAction.caller)
                {
                    case Pomegranate:
                        EnvironmentalRPC(player, DeathType.Pomegranate);
                        break;
                    case FlameJet:
                        EnvironmentalRPC(player, DeathType.Fire);
                        break;
                    case LightningMaker.StrikeAOE:
                        EnvironmentalRPC(player, DeathType.Lightning);
                        break;
                    case UnderwaterShock:
                        EnvironmentalRPC(player, DeathType.UnderwaterShock);
                        break;
                    case ElectricDeath:
                        EnvironmentalRPC(player, DeathType.Electric);
                        break;
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
        Coalescipede,
        Sandstorm,
        Poison,
        Lightning,
        Locust,
        Ripple,
        Pomegranate,
        Fire,
        UnderwaterShock,
    }
}
