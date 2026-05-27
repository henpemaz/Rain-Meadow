using System;
using System.Linq;
using System.Runtime.InteropServices;
using Drown;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine.SocialPlatforms.Impl;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public void DrownHooks()
        {
            On.HUD.TextPrompt.AddMessage_string_int_int_bool_bool += TextPrompt_AddMessage_string_int_int_bool_bool;
            On.Spear.Spear_makeNeedle += Spear_Spear_makeNeedle;
            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
            On.ArenaGameSession.PlayersStillActive += ArenaGameSession_PlayersStillActiveDrown;
            On.Player.checkInput += Player_checkInputDrown;
            On.ArenaGameSession.ScoreOfPlayer += ArenaGameSession_ScoreOfPlayerDrown;
        }



        public int ArenaGameSession_ScoreOfPlayerDrown(On.ArenaGameSession.orig_ScoreOfPlayer orig, ArenaGameSession self, Player player, bool inHands)
        {
            int score = orig(self, player, inHands);
            if (isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var d))
            {
                d.timerPoints = score;
            }
            return score;
        }

        private void Player_checkInputDrown(On.Player.orig_checkInput orig, Player self)
        {
            orig(self);
            if (RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var _) && self.IsLocal())
            {
                if (self.controller is null && self.room.world.game.cameras[0]?.hud is HUD.HUD hud
                    && (hud.parts.OfType<StoreHUD>().Any(x => x.active == true)))
                {
                    GameplayOverrides.StopPlayerMovement(self);
                }
            }
        }

        private int ArenaGameSession_PlayersStillActiveDrown(On.ArenaGameSession.orig_PlayersStillActive orig, ArenaGameSession self, bool addToAliveTime, bool dontCountSandboxLosers)
        {
            // 1. ALWAYS run orig first. 
            int activeCount = orig(self, addToAliveTime, dontCountSandboxLosers);

            if (RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var drown))
            {
                int canRespawnCount = 0;
                bool teamWork = !self.GameTypeSetup.spearsHitPlayers;

                // If dens are opened, nobody can respawn. Let native logic decide the end state.
                if (drown.openedDen || self.sessionEnded)
                {
                    return activeCount;
                }

                if (self.arenaSitting.players != null)
                {
                    for (int i = 0; i < self.arenaSitting.players.Count; i++)
                    {
                        bool isConsideredActiveByVanilla = false;

                        if (i < self.Players.Count && self.Players[i] != null)
                        {
                            var absCrit = self.Players[i];

                            // Base check: are they alive at all?
                            if (absCrit.state.alive)
                            {
                                isConsideredActiveByVanilla = true;

                                // Check if they are physically spawned and in danger
                                if (absCrit.realizedCreature is Player player)
                                {
                                    if (player.dangerGrasp != null || player.slatedForDeletetion)
                                    {
                                        isConsideredActiveByVanilla = false;
                                    }
                                }
                            }
                        }

                        // Check if they have the resources to come back.
                        if (!isConsideredActiveByVanilla)
                        {
                            int score = teamWork ? drown.teamPoints : self.arenaSitting.players[i].score;
                            if (score >= drown.respCost)
                            {
                                canRespawnCount++;
                            }
                        }
                    }
                }

                return activeCount + canRespawnCount;
            }

            return activeCount;
        }

        private void Player_ClassMechanicsSaint(ILContext il)
        {

            try
            {
                var c = new ILCursor(il);
                ILLabel skip = il.DefineLabel();
                c.GotoNext(
                     i => i.MatchLdloc(18),
                     i => i.MatchIsinst<Creature>(),
                     i => i.MatchCallvirt<Creature>("Die")
                     );
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloc, 18);
                c.EmitDelegate((Player self, PhysicalObject po) =>
                {
                    if (self.IsLocal() && RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var drown))
                    {
                        try
                        {
                            Creature creat = (po as Creature);
                            creat.SetKillTag(self.abstractCreature);
                            ArenaSitting sitting = self.room.game.GetArenaGameSession.arenaSitting;
                            IconSymbol.IconSymbolData iconSymbolData = CreatureSymbol.SymbolDataFromCreature(creat.abstractCreature);
                            int arenaPlayer = ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer);

                            int index = MultiplayerUnlocks.SandboxUnlockForSymbolData(iconSymbolData).Index;
                            if (index >= 0)
                            {
                                sitting.players[arenaPlayer].AddSandboxScore(sitting.gameTypeSetup.killScores[index]);
                            }
                            else
                            {
                                sitting.players[arenaPlayer].AddSandboxScore(0);
                            }

                        }
                        catch (Exception e)
                        {
                            RainMeadow.Error($"Error with Saint ascending: {e}");
                        }
                    }
                });

            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }

        }

        private void Spear_Spear_makeNeedle(On.Spear.orig_Spear_makeNeedle orig, Spear self, int type, bool active)
        {
            orig(self, type, active);
            if (!self.IsLocal())
            {
                return;
            }
            if (RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var drown))
            {
                ArenaSitting sitting = self.room.game.GetArenaGameSession.arenaSitting;
                sitting.players[ArenaHelpers.FindOnlinePlayerNumber(arena, OnlineManager.mePlayer)].score -= drown.spearCost;
            }

        }


        private void TextPrompt_AddMessage_string_int_int_bool_bool(On.HUD.TextPrompt.orig_AddMessage_string_int_int_bool_bool orig, HUD.TextPrompt self, string text, int wait, int time, bool darken, bool hideHud)
        {
            if (RainMeadow.isArenaMode(out var arena) && arena.externalArenaGameMode == arena.registeredGameModes.FirstOrDefault(kvp => kvp.Key == DrownMode.Drown.value).Value)
            {
                text = text + $" - Press {RainMeadow.rainMeadowOptions.DrownStoreKey.Value} to access the store";
                orig(self, text, wait, time, darken, hideHud);
            }
            else
            {
                orig(self, text, wait, time, darken, hideHud);
            }

        }
    }
}
