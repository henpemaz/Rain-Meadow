using MoreSlugcats;
using System;

namespace RainMeadow;

public static class DeathMessage
{
    public static void EnvironmentalDeathMessage(Player player, DeathType cause)
    {
        try
        {
            if (player == null || player.dead)
            {
                return;
            }
            var t = player.abstractPhysicalObject.GetOnlineObject().owner.id.name;
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
                    if (player.grabbedBy.Count > 0)
                    {
                        ChatLogManager.LogMessage("", $"{t} was drowned by {player.grabbedBy[0].grabber.Template.name}.");
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
            }
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }
    public static void PlayerKillPlayer(Player killer, Player target)
    {
        try
        {
            var k = killer.abstractPhysicalObject.GetOnlineObject().owner.id.name;
            var t = target.abstractPhysicalObject.GetOnlineObject().owner.id.name;
            ChatLogManager.LogMessage("", $"{t} was slain by {k}.");
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
            var t = target.abstractPhysicalObject.GetOnlineObject().owner.id.name;
            ChatLogManager.LogMessage("", $"{t} was slain by a {k}.");
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void PlayerKillCreature(Player killer, Creature target)
    {
        if (target is Player)
        {
            PlayerKillPlayer(killer, (Player)target);
            return;
        }
        try
        {
            var k = killer.abstractPhysicalObject.GetOnlineObject().owner.id.name;
            var t = target.Template.name;
            ChatLogManager.LogMessage("", $"{t} was slain by {k}.");
        }
        catch (Exception e)
        {
            RainMeadow.Error("Error displaying death message. " + e);
        }
    }

    public static void CreatureDeath(Creature crit)
    {
        if (crit.killTag != null && crit.killTag.realizedCreature != null)
        {
            if (crit.killTag.realizedCreature is Player && !RainMeadow.isArenaMode(out var _))
            {
                PlayerKillCreature(crit.killTag.realizedCreature as Player, crit);
            }
            else if (crit is Player)
            {
                CreatureKillPlayer(crit.killTag.realizedCreature, crit as Player);
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
                    EnvironmentalDeathMessage(player, DeathType.Drown);
                    return;
                }
                if (player.Hypothermia >= 1f)
                {
                    EnvironmentalDeathMessage(player, DeathType.Freeze);
                    return;
                }

                if (ModManager.MSC && player.SlugCatClass == MoreSlugcatsEnums.SlugcatStatsName.Artificer)
                {
                    if (player.airInLungs <= Player.PyroDeathThreshold(player.room.game))
                    {
                        EnvironmentalDeathMessage(player, DeathType.PyroDeath);
                        return;
                    }
                }

                if (player.Submersion > 0.2f && player.room.waterObject != null && player.room.waterObject.WaterIsLethal && !player.abstractCreature.lavaImmune)
                {
                    EnvironmentalDeathMessage(player, DeathType.Burn);
                    return;
                }
               
                // If nothing works we'll just say they died.
                EnvironmentalDeathMessage(player, DeathType.Invalid);
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
    }
}
