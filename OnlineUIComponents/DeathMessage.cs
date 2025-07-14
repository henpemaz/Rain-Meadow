using MoreSlugcats;
using RainMeadow.Game;
using System;
using System.Collections.Generic;
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
            op.InvokeRPC(RPCs.KillFeedEnvironment, opo, cause);
        }
    }

    /// <summary>
    /// Player kills creature (or other player)
    /// </summary>
    /// <param name="killer">Player</param>
    /// <param name="target">Creature</param>
    /// <param name="context">0 = Normal kill, 1 = Ascension</param>
    public static void PvPRPC(Player killer, Creature target, DeathType context)
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

    /// <summary>
    /// Creature kills player
    /// </summary>
    /// <param name="killer">Creature</param>
    /// <param name="target">Player</param>
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
            return onlineHuds.Where((e) => e.killFeed.Contains(opo.id)).Count() == 0;
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
            // Known causer + there is a custom message for that, show "died from <thing> by <person>" message
            if (opo.apo is AbstractCreature ac && ac.realizedCreature != null && ac.realizedCreature.grabbedBy.Count > 0 && DeathType.targetMessages.TryGetValue(cause, out var targetMsg))
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate(targetMsg) + " " + Utils.Translate(ac.realizedCreature.grabbedBy[0].grabber.Template.name) + ".");
            }
            // No custom message for known causer/no known causer, show generic "died from <thing>" message
            else if (DeathType.messages.TryGetValue(cause, out var msg))
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate(msg));
            }
            else
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("died."));
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
    public static void PlayerKillPlayer(OnlinePhysicalObject killer, OnlinePhysicalObject target, DeathType context)
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
            if (DeathType.targetMessages.TryGetValue(context, out var msg))
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate(msg) + $" {k}.");
            }
            else
            {
                ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was slain by") + $" {k}.");
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
            if (killer.apo is AbstractCreature kac)
            {
                var k = kac.creatureTemplate.name;
                var t = target.owner.id.name;
                if (!ShouldShowDeath(target)) return;
                if (kac.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Centipede)
                {
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was zapped by a") + " " + Utils.Translate(k) + ".");
                }
                else if (kac.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.DropBug)
                {
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was ambushed by a") + " " + Utils.Translate(k) + ".");
                }
                else if (kac.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.PoleMimic || kac.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.TentaclePlant || kac.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.MirosBird || (ModManager.Watcher && kac.creatureTemplate.TopAncestor().type == Watcher.WatcherEnums.CreatureTemplateType.BigMoth))
                {
                    ChatLogManager.LogSystemMessage(t + " " + Utils.Translate("was taken by a") + " " + Utils.Translate(k) + ".");
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
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void PlayerKillCreature(OnlinePhysicalObject killer, OnlinePhysicalObject target, DeathType context)
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
            if (target.apo is AbstractCreature tac)
            {
                var k = killer.owner.id.name;
                var t = tac.creatureTemplate.name;
                var realized = tac.realizedCreature;
                if (!ShouldShowDeath(target)) return;
                if (DeathType.targetMessages.TryGetValue(context, out var msg))
                {
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate(msg) + $" {k}.");
                }
                else
                {
                    ChatLogManager.LogSystemMessage(Utils.Translate(t) + " " + Utils.Translate("was slain by") + $" {k}.");
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
                PvPRPC(crit.killTag.realizedCreature as Player, crit, DeathType.Kill);
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
                            spiders += player.grabbedBy[i].grabber.TotalMass;
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

    public class DeathType(string value, bool register = false) : ExtEnum<DeathType>(value, register)
    {
        public static DeathType Unknown = new("Unknown", true);
        public static DeathType Rain = new("Rain", true);
        public static DeathType Abyss = new("Abyss", true);
        public static DeathType Drown = new("Drown", true);
        public static DeathType FallDamage = new("FallDamage", true);
        public static DeathType Oracle = new("Oracle", true);
        public static DeathType Burn = new("Burn", true);
        public static DeathType PyroDeath = new("PyroDeath", true);
        public static DeathType Freeze = new("Freeze", true);
        public static DeathType WormGrass = new("WormGrass", true);
        public static DeathType WallRot = new("WallRot", true);
        public static DeathType Electric = new("Electric", true);
        public static DeathType DeadlyLick = new("DeadlyLick", true);
        public static DeathType Coalescipede = new("Coalescipede", true);
        public static DeathType Sandstorm = new("Sandstorm", true);
        public static DeathType Poison = new("Poison", true);
        public static DeathType Lightning = new("Lightning", true);
        public static DeathType Locust = new("Locust", true);
        public static DeathType Ripple = new("Ripple", true);
        public static DeathType Pomegranate = new("Pomegranate", true);
        public static DeathType Fire = new("Fire", true);
        public static DeathType UnderwaterShock = new("UnderwaterShock", true);

        // Non-environmental kills (PVP)
        public static DeathType Kill = new("Kill", true);
        public static DeathType Ascencion = new("Ascencion", true);

        public static Dictionary<DeathType, string> messages = new()
        {
            { Unknown, "died." },
            { Rain, "was crushed by the rain." },
            { Abyss, "fell into the abyss." },
            { Drown, "drowned." },
            { FallDamage, "hit the ground too hard." },
            { Oracle, "was killed through unknown means." },
            { Burn, "tried to swim in burning liquid." },
            { PyroDeath, "spontaneously combusted." },
            { Freeze, "froze to death." },
            { WormGrass, "was swallowed by the grass." },
            { WallRot, "was swallowed by the walls." },
            { Electric, "was electrocuted." },
            { DeadlyLick, "licked the power." },
            { Coalescipede, "was consumed by the swarm." },
            { UnderwaterShock, "was electrocuted in the water." },
            { Sandstorm, "was obliterated by the sandstorm." },
            { Poison, "died from poison." },
            { Lightning, "was struck by lightning." },
            { Locust, "was consumed by locusts." },
            { Ripple, "swam with the ripples." },
            { Pomegranate, "experienced the force of fresh fruit." },
            { Fire, "was burnt to a crisp." },
        };
        // "Drowned-by" messages; i.e where the killer is known (also used for PVP deaths)
        public static Dictionary<DeathType, string> targetMessages = new()
        {
            { Abyss, "was taken into the abyss by" },
            { Drown, "was drowned by" },
            { Kill, "was slain by" },
            { Ascencion, "was ascended by" },
        };
    }
}
