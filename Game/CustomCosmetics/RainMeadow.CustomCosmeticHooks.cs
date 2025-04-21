
using System;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        // custom capes and such
        private void CosmeticHooks()
        {
            On.PlayerGraphics.Update += PlayerGraphics_UpdateCosmetics;
            On.PlayerGraphics.Reset += PlayerGraphics_ResetCosmetics;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSpritesCosmetics;
            On.PlayerGraphics.AddToContainer += PlayerGraphics_AddToContainerCosmetics;
            IL.PlayerGraphics.InitiateSprites += PlayerGraphics_InitiateSpritesCosmetics;
        }

        void PlayerGraphics_InitiateSpritesCosmetics(ILContext cursor) {
            try {
                ILCursor c = new(cursor);

                // inserted right after 
                // num += this.mudSpriteCount;
                int numofsprites_loc = default;

                
                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdloc(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(out _), // mudsprite
                    x => x.MatchAdd(),
                    x => x.MatchStloc(out numofsprites_loc)
                );

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, numofsprites_loc);
                c.EmitDelegate((PlayerGraphics self, ref int numofsprites) => {
                    try {
                        if (OnlineManager.lobby != null) {
                            if (!self.player.isNPC && self.player.abstractCreature.GetOnlineCreature() is OnlineCreature critter) {
                                Color? cape = SlugcatCape.HasCape(critter.owner.id);
                                if (cape.HasValue) {
                                    numofsprites += SlugcatCape.totalSprites;
                                    if (!SlugcatCape.cloaked_slugcats.TryGetValue(self, out _)) {
                                        new SlugcatCape(self, numofsprites - 1, cape.Value);
                                    }
                                }
                            }
                        }
                    } catch (Exception except) {
                        RainMeadow.Error(except);
                    }
                });

                c.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(1),
                    x => x.MatchLdloc(0),
                    x => x.MatchNewarr<FSprite>(), // mudsprite
                    x => x.MatchStfld(out _)
                );


                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate((PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) => {
                    try {
                        if (OnlineManager.lobby != null) {
                            if (SlugcatCape.cloaked_slugcats.TryGetValue(self, out var cape)) {
                                cape.InitiateSprites(sLeaser, rCam);
                            }
                        }
                    } catch (Exception except) {
                        RainMeadow.Error(except);
                    }
                });

            } catch(Exception except) {
                RainMeadow.Error(except); 
            }
        }

        void PlayerGraphics_UpdateCosmetics(On.PlayerGraphics.orig_Update orig, PlayerGraphics self) {
            orig(self);
            try {                
                if (OnlineManager.lobby != null) {
                    if (SlugcatCape.cloaked_slugcats.TryGetValue(self, out var cape)) {
                        cape.Update();
                    }
                }
            } catch (Exception except) {
                RainMeadow.Error(except);
            }
        }

        void PlayerGraphics_ResetCosmetics(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self) {
            orig(self);
            try {                
                if (OnlineManager.lobby != null) {
                    if (SlugcatCape.cloaked_slugcats.TryGetValue(self, out var cape)) {
                        cape.Reset();
                    }
                }
            } catch (Exception except) {
                RainMeadow.Error(except);
            }
        }

        void PlayerGraphics_DrawSpritesCosmetics(On.PlayerGraphics.orig_DrawSprites orig, 
                PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, 
                RoomCamera rCam, float timeStacker, Vector2 camPos) {  
            orig(self, sLeaser, rCam, timeStacker, camPos);

            try {
                if (OnlineManager.lobby != null) {
                    if (SlugcatCape.cloaked_slugcats.TryGetValue(self, out var cape)) {
                        cape.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                    }
                }
            } catch (Exception except) {
                RainMeadow.Error(except);
            }
        }

        public void PlayerGraphics_AddToContainerCosmetics(On.PlayerGraphics.orig_AddToContainer orig, global::PlayerGraphics self, global::RoomCamera.SpriteLeaser sLeaser, global::RoomCamera rCam, global::FContainer newContatiner) {  
            orig(self, sLeaser, rCam, newContatiner);

            try {
                if (OnlineManager.lobby != null) {
                    if (SlugcatCape.cloaked_slugcats.TryGetValue(self, out var cape)) {
                        cape.AddToContainer(sLeaser, rCam, newContatiner);
                    }
                }
            } catch (Exception except) {
                RainMeadow.Error(except);
            }
        }

    }
}
