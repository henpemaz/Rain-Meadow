
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
            IL.WindRect.Update += WindRect_Update_ApplyWindToCosmetics;
        }

        public static ConditionalWeakTable<GraphicsModule, CosmeticManager.IMeadowCosmetic> cosmeticed_avatars = new();

        
        private void WindRect_Update_ApplyWindToCosmetics(ILContext il)
        {
            try
            {
                ILCursor cursor = new(il);
                cursor.GotoNext(MoveType.After,
                    x => x.MatchStloc(7),
                    x => x.MatchBr(out _)
                );
                cursor.MoveAfterLabels();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_2);
                cursor.Emit(OpCodes.Ldloc_S, (byte)7);
                cursor.Emit(OpCodes.Ldloc_0);

                cursor.EmitDelegate((WindRect windRect, List<UpdatableAndDeletable> updateList, int i, float windForce) =>
                {
                    if (updateList[i] is Player player 
                        && windRect.WindAffectsPoint(player.mainBodyChunk.pos)
                        && cosmeticed_avatars.TryGetValue(player.graphicsModule, out var cosmetic))
                    {
                        cosmetic.ApplyWind(windForce * Vector2.right);
                    } 
                });
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        void PlayerGraphics_InitiateSpritesCosmetics(ILContext cursor)
        {
            try
            {
                ILCursor c = new(cursor);

                // inserted right after 
                // this.gownIndex = num - 1;
                int numofsprites_loc = default;
                c.GotoNext(
                    MoveType.Before,
                    x => x.MatchLdarg(1),
                    x => x.MatchLdloc(0),
                    x => x.MatchNewarr<FSprite>(),
                    x => x.MatchStfld<RoomCamera.SpriteLeaser>(nameof(RoomCamera.SpriteLeaser.sprites))
                );

                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, numofsprites_loc);
                c.EmitDelegate((PlayerGraphics self, ref int numofsprites) =>
                {
                    try
                    {
                        if (OnlineManager.lobby != null)
                        {
                            if (!cosmeticed_avatars.TryGetValue(self, out var old_cosmetic))
                            {
                                if (self.player.abstractCreature.GetOnlineCreature() is OnlineCreature critter && critter.isAvatar)
                                {
                                    if (critter.TryGetData<SlugcatCustomization>(out var customization) && customization.cosmetic != null && CosmeticManager.AvailableCosmetics(critter.owner.id).Contains(customization.cosmetic))
                                    {
                                        string skin = customization.cosmeticSkin ?? "solid";
                                        if (!CosmeticManager.AvailableCosmeticSkins(critter.owner.id).Contains(skin)) skin = "solid";

                                        var real_skin = CosmeticManager.ParseCosmeticSkin(skin);
                                        if (real_skin != null)
                                        {
                                            var new_cosmetic = CosmeticManager.ParseCosmetic(customization.cosmetic, self, numofsprites, real_skin);
                                            if (new_cosmetic != null)
                                            {
                                                RainMeadow.Debug(numofsprites);
                                                numofsprites += new_cosmetic.totalSprites;
                                                cosmeticed_avatars.Add(self, new_cosmetic);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                numofsprites += old_cosmetic.totalSprites;
                            }
                        }
                    }
                    catch (Exception except)
                    {
                        RainMeadow.Error(except);
                    }
                });


                c.Index += 4;
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_1);
                c.Emit(OpCodes.Ldarg_2);
                c.EmitDelegate((PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam) =>
                {
                    try
                    {
                        if (OnlineManager.lobby != null)
                        {
                            if (cosmeticed_avatars.TryGetValue(self, out var cosmetic))
                            {
                                RainMeadow.Debug(sLeaser.sprites.Length);
                                cosmetic.InitiateSprites(sLeaser, rCam);
                            }
                        }
                    }
                    catch (Exception except)
                    {
                        RainMeadow.Error(except);
                    }
                });

            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        void PlayerGraphics_UpdateCosmetics(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            try
            {
                if (OnlineManager.lobby != null)
                {
                    if (cosmeticed_avatars.TryGetValue(self, out var cosmetic))
                    {
                        cosmetic.Update();
                    }
                }
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        void PlayerGraphics_ResetCosmetics(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            try
            {
                if (OnlineManager.lobby != null)
                {
                    if (cosmeticed_avatars.TryGetValue(self, out var cosmetic))
                    {
                        cosmetic.Reset();
                    }
                }
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        void PlayerGraphics_DrawSpritesCosmetics(On.PlayerGraphics.orig_DrawSprites orig,
                PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser,
                RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);

            try
            {
                if (OnlineManager.lobby != null)
                {
                    if (cosmeticed_avatars.TryGetValue(self, out var cosmetic))
                    {
                        cosmetic.DrawSprites(sLeaser, rCam, timeStacker, camPos);
                    }
                }
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

        public void PlayerGraphics_AddToContainerCosmetics(On.PlayerGraphics.orig_AddToContainer orig, global::PlayerGraphics self, global::RoomCamera.SpriteLeaser sLeaser, global::RoomCamera rCam, global::FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);

            try
            {
                if (OnlineManager.lobby != null)
                {
                    if (cosmeticed_avatars.TryGetValue(self, out var cosmetic))
                    {
                        cosmetic.AddToContainer(sLeaser, rCam, newContatiner);
                    }
                }
            }
            catch (Exception except)
            {
                RainMeadow.Error(except);
            }
        }

    }
}
