﻿using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    // Support character customization (WIP)
    public partial class RainMeadow
    {
        public static ConditionalWeakTable<Creature, AvatarData> creatureCustomizations = new();

        public void CustomizationHooks()
        {
            IL.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette;
            IL.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites;

            // SlugcatCustomization stuff
            On.PlayerGraphics.InitiateSprites += PlayerGraphicsOnInitiateSprites;
            On.PlayerGraphics.ApplyPalette += PlayerGraphics_ApplyPalette_SlugcatCustomization;
            On.PlayerGraphics.DrawSprites += PlayerGraphics_DrawSprites_SlugcatCustomization;
            On.PlayerGraphics.CustomColorSafety += PlayerGraphics_CustomColorSafety_SlugcatCustomization;
            On.PlayerGraphics.CustomColorsEnabled += PlayerGraphics_CustomColorsEnabled_SlugcatCustomization;

            // for cosmetics such as the capes.
            CosmeticHooks();
        }

        // eyecolor is overwritten every frame for some stupid reason
        private void PlayerGraphics_DrawSprites(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                //VariableDefinition[] colors = il.Body.Variables.Where(v => v.VariableType.Resolve().FullName == typeof(Color).FullName).ToArray();
                //var color1 = colors[0];
                //var color2 = colors[1];

                foreach (var colorVar in il.Body.Variables.Where(v => v.VariableType.Resolve().FullName == typeof(Color).FullName).Take(2))
                {
                    c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchLdarg(1),
                    i => i.MatchLdfld<RoomCamera.SpriteLeaser>("sprites"),
                    i => i.MatchLdcI4(9),
                    i => i.MatchLdelemRef(),
                    i => i.MatchLdloc(colorVar.Index),
                    i => i.MatchCallOrCallvirt<FSprite>("set_color")
                    );
                    c.MoveAfterLabels();
                    c.Emit(OpCodes.Ldarg_0);
                    c.Emit(OpCodes.Ldloca, colorVar.Index);
                    c.EmitDelegate((PlayerGraphics self, ref Color originalEyeColor) =>
                    {
                        if (RainMeadow.creatureCustomizations.TryGetValue(self.player, out var customization))
                        {
                            customization.ModifyEyeColor(ref originalEyeColor);
                        }
                    });
                }
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // bodycolor is set here
        private void PlayerGraphics_ApplyPalette(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchStloc(0)
                    );
                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldloca, 0);
                c.EmitDelegate((PlayerGraphics self, ref Color originalBodyColor) =>
                {
                    if (RainMeadow.creatureCustomizations.TryGetValue(self.player, out var customization))
                    {
                        customization.ModifyBodyColor(ref originalBodyColor);
                        RainMeadow.Trace("color became " + originalBodyColor);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        // SlugcatCustomization stuff
        private static SlugcatCustomization? hackySlugcatCustomization;  // HACK: CustomColorSafety has no ref to player so we use this

        // To explain further, basically try store a value into this field before orig in the relevant methods, then restore to null after orig
        // Allows it to be read when the PlayerGraphics static methods are called

        private void PlayerGraphicsOnInitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sleaser, RoomCamera rcam)
        {
            var cachedCustomColors = PlayerGraphics.customColors;

            try
            {
                creatureCustomizations.TryGetValue(self.player, out var customization);
                hackySlugcatCustomization = customization as SlugcatCustomization;

                if (hackySlugcatCustomization is not null)
                {
                    PlayerGraphics.customColors = hackySlugcatCustomization.currentColors;
                }

                orig(self, sleaser, rcam);
            }
            finally
            {
                hackySlugcatCustomization = null;
                PlayerGraphics.customColors = cachedCustomColors;
            }
        }

        private void PlayerGraphics_ApplyPalette_SlugcatCustomization(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            var cachedCustomColors = PlayerGraphics.customColors;

            try
            {
                creatureCustomizations.TryGetValue(self.player, out var customization);
                hackySlugcatCustomization = customization as SlugcatCustomization;

                if (hackySlugcatCustomization is not null)
                {
                    PlayerGraphics.customColors = hackySlugcatCustomization.currentColors;
                }

                orig(self, sLeaser, rCam, palette);
            }
            finally
            {
                hackySlugcatCustomization = null;
                PlayerGraphics.customColors = cachedCustomColors;
            }
        }

        private void PlayerGraphics_DrawSprites_SlugcatCustomization(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, UnityEngine.Vector2 camPos)
        {
            var cachedCustomColors = PlayerGraphics.customColors;

            try
            {
                creatureCustomizations.TryGetValue(self.player, out var customization);
                hackySlugcatCustomization = customization as SlugcatCustomization;

                if (hackySlugcatCustomization is not null)
                {
                    PlayerGraphics.customColors = hackySlugcatCustomization.currentColors;
                }

                orig(self, sLeaser, rCam, timeStacker, camPos);
            }
            finally
            {
                hackySlugcatCustomization = null;
                PlayerGraphics.customColors = cachedCustomColors;
            }
        }


        // Statics, read hackySlugcatCustomization here
        private bool PlayerGraphics_CustomColorsEnabled_SlugcatCustomization(On.PlayerGraphics.orig_CustomColorsEnabled orig)
        {
            if (hackySlugcatCustomization is not null)
            {
                return true;
            }
            return orig();
        }

        private Color PlayerGraphics_CustomColorSafety_SlugcatCustomization(On.PlayerGraphics.orig_CustomColorSafety orig, int staticColorIndex)
        {
            if (hackySlugcatCustomization is not null)
            {
                return hackySlugcatCustomization.GetColor(staticColorIndex);
            }
            return orig(staticColorIndex);
        }
    }
}
