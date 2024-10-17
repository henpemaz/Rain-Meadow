using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
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
                        RainMeadow.Debug("color became " + originalBodyColor);
                    }
                });
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }
    }
}
