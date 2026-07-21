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
        if (player == null) return;
        var opo = player.abstractPhysicalObject.GetOnlineObject();
        if (opo == null) return;
        OnlinePhysicalObject? blame = null;
        if (player.killTag?.GetOnlineObject() != null)
        {
            blame = player.killTag?.GetOnlineObject();
        }
        foreach(var op in OnlineManager.players)
        {
            op.InvokeRPC(RPCs.KillFeedEnvironment, opo, (int)cause, blame);
        }
    }
    public static void PvPRPC(Player killer, Creature target, int context)
    {
        if (killer == null) return;
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
        if (killer == null) return;
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
    public static void EnvironmentalDeathMessage(OnlinePhysicalObject opo, DeathType cause, OnlinePhysicalObject? blame = null)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            if (opo == null || !ShouldShowDeath(opo))
            {
                return;
            }
            var t = opo.owner.id.DisplayName;
            string k = "";
            if (blame != null) k = blame.owner.id.DisplayName;

            switch (cause)
            {
                default:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("died."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Rain:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was crushed by the rain."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Abyss:
                    if (string.IsNullOrEmpty(k))
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("fell into the abyss."), ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    else
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate($"fell into the abyss thanks to") + " " + k + ".", ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    break;
                case DeathType.Drown:
                    if (!string.IsNullOrEmpty(k))
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was drowned by") + " " + k + ".", ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    if ((opo.apo as AbstractCreature).realizedCreature != null && (opo.apo as AbstractCreature).realizedCreature.grabbedBy.Count > 0)
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was drowned by a") + " " + Utils.Translate((opo.apo as AbstractCreature).realizedCreature.grabbedBy[0].grabber.Template.name) + ".", ChatLogManager.SystemMessageType.CreatureDeath);
                        break;
                    }
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("drowned."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.FallDamage:
                    if (string.IsNullOrEmpty(k))
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("hit the ground too hard."), ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    else
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("hit the ground too hard thanks to") + " " + k + ".", ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    break;
                case DeathType.Oracle:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was killed through unknown means."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Burn:
                    if (string.IsNullOrEmpty(k))
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("tried to swim in burning liquid."), ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    else
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("tried to swim in burning liquid to escape") + " " + k + ".", ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    break;
                case DeathType.PyroDeath:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("spontaneously combusted."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Freeze:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("froze to death."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.WormGrass:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was swallowed by the grass."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.WallRot:
                    if (string.IsNullOrEmpty(k))
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was swallowed by the walls."), ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    else
                    {
                        ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was swallowed by the walls thanks to") + " " + k + ".", ChatLogManager.SystemMessageType.CreatureDeath);
                    }
                    break;
                case DeathType.Electric:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was electrocuted."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.DeadlyLick:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("licked the power."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Coalescipede:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was consumed by the swarm."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.UnderwaterShock:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was electrocuted in the water."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.SoloExplosion:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("blew up."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;

                // WATCHER

                case DeathType.Sandstorm:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("asphyxiated."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Poison:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("died from poison."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Lightning:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was struck by lightning."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Locust:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was consumed by locusts."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Ripple:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("swam with the ripples."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Pomegranate:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("experienced the force of fresh fruit."), ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case DeathType.Fire:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was burnt to a crisp."), ChatLogManager.SystemMessageType.CreatureDeath);
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
    public static void PlayerKillPlayer(OnlinePhysicalObject killer, OnlinePhysicalObject target, PvPContext context)
    {
        try
        {
            if (RWCustom.Custom.rainWorld.processManager.currentMainLoop is not RainWorldGame game) return;
            if (target == null || killer == null || !ShouldShowDeath(target))
            {
                return;
            }
            var k = killer.owner.id.DisplayName;
            var t = target.owner.id.DisplayName;
            switch(context)
            {
                default:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was slain by") + $" {k}.", ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case PvPContext.Saint:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was ascended by") + $" {k}.", ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case PvPContext.Explosion:
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was blown up by") + $" {k}.", ChatLogManager.SystemMessageType.CreatureDeath);
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
            var t = target.owner.id.DisplayName;
            if (!ShouldShowDeath(target)) return;
            if ((killer.apo as AbstractCreature).creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Centipede)
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was zapped by a") + " " + Utils.Translate(k) + ".", ChatLogManager.SystemMessageType.CreatureDeath);
            } 
            else
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was slain by a") + " " + Utils.Translate(k) + ".", ChatLogManager.SystemMessageType.CreatureDeath);
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

    public static void PlayerKillCreature(OnlinePhysicalObject killer, OnlinePhysicalObject target, PvPContext context)
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
            var k = killer.owner.id.DisplayName;
            var t = (target.apo as AbstractCreature).creatureTemplate.name;
            var realized = (target.apo as AbstractCreature).realizedCreature;
            if (!ShouldShowDeath(target)) return;
            switch (context)
            {
                default:
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate("was slain by") + $" {k}.", ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case PvPContext.Saint:
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate("was ascended by") + $" {k}.", ChatLogManager.SystemMessageType.CreatureDeath);
                    break;
                case PvPContext.Explosion:
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate("was blown up by") + $" {k}.", ChatLogManager.SystemMessageType.CreatureDeath);
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
                // TODO: add method to get context beyond default & saint
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
                    if (player.room.roomRain == null && player.room.sandstorm != null)
                    {
                        EnvironmentalRPC(player, DeathType.Sandstorm);
                    }
                    else
                    {
                        EnvironmentalRPC(player, DeathType.Rain);
                    }
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
                    case Explosion:
                        EnvironmentalRPC(player, DeathType.SoloExplosion);
                        break;
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
        SoloExplosion,
        Sandstorm,
        Poison,
        Lightning,
        Locust,
        Ripple,
        Pomegranate,
        Fire,
        UnderwaterShock,
    }

    public enum PvPContext
    {
        Default,
        Saint,
        Explosion,
    }
}
