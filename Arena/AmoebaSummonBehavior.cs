using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using IL.RWCustom;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RainMeadow.Arena.ArenaOnlineGameModes.ArenaChallengeModeNS;
using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RainMeadow.UI;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow;

public class AmoebaSummonBehavior(VoidSpawn owner) : VoidSpawn.Behavior(owner)
{
    public static int GetPriority(ArenaOnlineGameMode arena, VoidSpawn voidSpawn, Player? player)
    {
        if (player == null || player.dead)
            return 0;
        if (player.abstractCreature.rippleLayer != voidSpawn.abstractPhysicalObject.rippleLayer)
            return 1;
        int additionalPoints = 0;
        if (player.abstractCreature.GetOnlineObject(out var opo))
        {
            foreach (
                ArenaSitting.ArenaPlayer arenaPlayer in player
                    .room
                    .game
                    .GetArenaGameSession
                    .arenaSitting
                    .players
            )
            {
                if (
                    arenaPlayer.playerNumber
                    == ArenaHelpers.FindOnlinePlayerNumber(arena, opo!.owner)
                )
                {
                    additionalPoints = arenaPlayer.allKills.Count;
                    break;
                }
            }
        }
        return 2 + additionalPoints;
    }

    public override Vector2 SwimTowards
    {
        get
        {
            if (!RainMeadow.isArenaMode(out var arena)) return base.SwimTowards;
            VoidSpawn voidSpawn = this.owner;
            // RainMeadow.Debug($"{voidSpawn} choosing a behavior...");
            

            // 1. If pointing, go toward the point.
            if (arena.amoebaControl && Input.GetKey(RainMeadow.rainMeadowOptions.PointingKey.Value))
            {
                Vector2 pointingVector = Pointing.GetOnlinePointingVector();
                var controller = RWCustom
                    .Custom.rainWorld.options.controls[0]
                    .GetActiveController();
                // RainMeadow.Debug($"(1) Following point !");
                this.wasCircling = false;
                if (controller is Rewired.Joystick)
                {
                    Vector2 lastPosition = this.owner
                        .abstractPhysicalObject
                        .realizedObject
                        .bodyChunks[0]
                        .pos;
                    Vector2 nextPosition = lastPosition + pointingVector * pointingWeight;
                    return nextPosition;
                }
                else
                {
                    return pointingVector;
                }
            }
            

            // 2. If spear hit is on, and a target player was found, go toward it.
            Player? ownerPlayer = null;
            float minDistance = 0f;
            Player? foundPlayer = null;

            int foundPlayerPriority = 0;
            foreach (AbstractCreature player in voidSpawn.room.game.GetArenaGameSession.Players)
            {
                if (player.realizedCreature is not Player realizedPlayer)
                    continue;

                if (realizedPlayer.room == null
                    || realizedPlayer.room.abstractRoom.index != voidSpawn.room.abstractRoom.index)
                    continue;

                if (player.IsLocal(out var oe))
                {
                    ownerPlayer = player.realizedCreature as Player;
                    continue;
                }
                if (!voidSpawn.room.game.GetArenaGameSession.arenaSitting.gameTypeSetup.spearsHitPlayers)
                    continue;
                
                if (TeamBattleMode.IsTeamBattleMode(out var tb))
                {
                    ArenaTeamClientSettings? playerTeam =
                        ArenaHelpers.GetDataSettings<ArenaTeamClientSettings>(oe!.owner);
                    if (playerTeam != null && playerTeam.team == arena.arenaTeamClientSettings.team)
                        continue;
                }

                if (player.realizedCreature != null && player.realizedCreature.State.dead)
                    continue;

                int playerPriority = GetPriority(arena, voidSpawn, realizedPlayer);
                float distance = Vector2.Distance(
                    voidSpawn.firstChunk.pos,
                    realizedPlayer.mainBodyChunk.pos
                );

                if (
                    foundPlayer == null
                    || playerPriority > foundPlayerPriority
                    || (playerPriority == foundPlayerPriority && distance < minDistance)
                )
                {
                    foundPlayer = realizedPlayer;
                    foundPlayerPriority  = GetPriority(arena, voidSpawn, foundPlayer);
                    minDistance = distance;
                }
            }
            
            if (foundPlayer != null) 
            {
                // RainMeadow.Debug($"(2) Attacking Player !");
                this.wasCircling = false;
                return foundPlayer.mainBodyChunk.pos;
            }
            

            // 3. If a hostile creature is found, go toward it.
            Creature? foundCreature = null;
            int foundAggroScore = 0;
            foreach (AbstractCreature abstractCreature in voidSpawn.room.abstractRoom.creatures)
            {
                if (abstractCreature.realizedCreature is not Creature creature)
                    continue;
                if (creature is Player)
                    continue;

                if (creature.room == null
                    || creature.room.abstractRoom.index != voidSpawn.room.abstractRoom.index)
                    continue;

                if (creature.State.dead)
                    continue;

                float distance = Vector2.Distance(
                    voidSpawn.firstChunk.pos,
                    creature.mainBodyChunk.pos
                );
                int aggression = voidSpawn.abstractPhysicalObject.rippleLayer != creature.abstractCreature.rippleLayer
                    ? 0 // can't really aggro if you are not in the same realm
                    : (ownerPlayer is not null 
                        ? (int)(abstractCreature.abstractAI?.RealAI?.CurrentPlayerAggression(ownerPlayer.abstractCreature) * 10 ?? 0)
                        : 5); // aggression is from 0 to 1, make it an int from 0 to 10

                if (foundCreature == null
                    || aggression > foundAggroScore
                    || (aggression == foundAggroScore && distance < minDistance)
                )
                {
                    foundCreature = creature;
                    foundAggroScore = aggression;
                    minDistance = distance;
                }
            }

            if (foundCreature != null) 
            {
                // RainMeadow.Debug($"(3) Attacking Creature !");
                if (minDistance <= stunDistance) voidSpawn.playerProximityTime = 10;
                this.wasCircling = false;
                return foundCreature.mainBodyChunk.pos;
            }

            
            // 4. If an owner is found, circle around it
            if (ownerPlayer is not null)
            {
                if (!this.wasCircling && UnityEngine.Random.value > 0.5f) rotationDir = !rotationDir;
                this.wasCircling = true;
                Vector2 ownerDir = ownerPlayer.mainBodyChunk.pos - voidSpawn.firstChunk.pos;
                Vector2 perpendicularDir = new(ownerDir.y, ownerDir.x);
                if (rotationDir) perpendicularDir.x *= -1; else perpendicularDir.y *= -1;

                Vector2 finalDir = perpendicularDir.normalized * rotationVectorWeight + ownerDir.normalized * (ownerDir.magnitude - aroundOwnerDistance);

                // RainMeadow.Debug($"(4) Circling owner !");
                return voidSpawn.firstChunk.pos + finalDir.normalized * pointingWeight;
            }

            // 4. Else, default behaviour
            // RainMeadow.Debug($"(5) Floating away...");
            this.wasCircling = false;
            return new Vector2(voidSpawn.mainBody[0].pos.x, voidSpawn.mainBody[1].pos.y);
        }
    }

    public const float rotationVectorWeight = 75;
    public const float aroundOwnerDistance = 100;
    public const float pointingWeight = 400;
    public const float stunDistance = 50;
    public const int stunTime = 40 * 2;
    public bool rotationDir;
    public bool wasCircling = false;
}