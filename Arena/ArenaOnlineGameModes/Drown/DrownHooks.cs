using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Reflection;
using Drown;
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

namespace RainMeadow
{
    public partial class RainMeadow
    {
        public void DrownHooks()
        {
            On.Menu.Menu.ctor += Menu_ctor;
            On.HUD.TextPrompt.AddMessage_string_int_int_bool_bool += TextPrompt_AddMessage_string_int_int_bool_bool;
            On.Spear.Spear_makeNeedle += Spear_Spear_makeNeedle;
            IL.Player.ClassMechanicsSaint += Player_ClassMechanicsSaint;
            On.ArenaGameSession.PlayersStillActive += ArenaGameSession_PlayersStillActiveDrown;
            On.Player.checkInput += Player_checkInputDrown;
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
            if (RainMeadow.isArenaMode(out var arena) && DrownMode.isDrownMode(arena, out var drown))
            {
                bool teamWork = !self.GameTypeSetup.spearsHitPlayers;

                var count = 0;
                foreach (var p in arena.arenaSittingOnlineOrder)
                {
                    OnlinePlayer? pl = ArenaHelpers.FindOnlinePlayerByLobbyId(p);
                    if (pl != null)
                    {
                        OnlineManager.lobby.clientSettings.TryGetValue(pl, out var cs);
                        if (cs != null)
                        {

                            cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                            if (clientSettings != null)
                            {

                                if ((teamWork ? clientSettings.teamScore : clientSettings.score) >= drown.respCost && !drown.openedDen) // We can still respawn
                                {
                                    count++;
                                }
                            }
                        }
                    }
                }

                if (teamWork && (self.Players.FindAll(x => x.realizedCreature != null && x.realizedCreature.State.alive).Count > 0))
                {
                    return orig(self, addToAliveTime, dontCountSandboxLosers);
                }

                if (self.Players.FindAll(x => x.realizedCreature != null && x.realizedCreature.State.alive).Count == 0)
                {
                    if (count > 0)
                    {
                        return count;
                    }
                }

            }
            return orig(self, addToAliveTime, dontCountSandboxLosers);
        }

        private void Menu_ctor(On.Menu.Menu.orig_ctor orig, Menu.Menu self, ProcessManager manager, ProcessManager.ProcessID ID)
        {
            if (RainMeadow.isArenaMode(out var arena) && arena != null && self is ArenaOnlineLobbyMenu)
            {
                if (!arena.registeredGameModes.ContainsKey(DrownMode.Drown.value))
                {
                    arena.registeredGameModes.Add(DrownMode.Drown.value, new DrownMode());
                }
            }
            orig(self, manager, ID);
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
                            OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                            if (cs != null)
                            {
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

                                cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                                if (clientSettings != null)
                                {
                                    clientSettings.score += sitting.gameTypeSetup.killScores[index];
                                }
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
                OnlineManager.lobby.clientSettings.TryGetValue(OnlineManager.mePlayer, out var cs);
                if (cs != null)
                {

                    cs.TryGetData<ArenaDrownClientSettings>(out var clientSettings);
                    if (clientSettings != null)
                    {
                        clientSettings.score = clientSettings.score - drown.spearCost;
                    }
                }
            }

        }


        private void TextPrompt_AddMessage_string_int_int_bool_bool(On.HUD.TextPrompt.orig_AddMessage_string_int_int_bool_bool orig, HUD.TextPrompt self, string text, int wait, int time, bool darken, bool hideHud)
        {
            if (RainMeadow.isArenaMode(out var arena) && arena.externalArenaGameMode == arena.registeredGameModes.FirstOrDefault(kvp => kvp.Key == DrownMode.Drown.value).Value)
            {
                text = text + $" - Press {RainMeadow.rainMeadowOptions.OpenStore.Value} to access the store";
                orig(self, text, wait, time, darken, hideHud);
            }
            else
            {
                orig(self, text, wait, time, darken, hideHud);
            }

        }
    }
}
