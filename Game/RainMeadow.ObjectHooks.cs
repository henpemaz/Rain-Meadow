using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        internal static int splashObjectCount = 0;
        internal static int splashTickCount = 0;

        private void ObjectHooks()
        {
            IL.Room.Update += Room_Update;
            IL.Water.Update += Water_Update;
            On.Water.Update += Water_Update2;
        }

        private void Water_Update2(On.Water.orig_Update orig, Water self)
        {
            if (splashTickCount >= 40)
            { //40 ticks per second, good metric, very primtive but works
                splashTickCount = 0;
                splashObjectCount = 0;
            }
            orig(self);
            splashTickCount += 1;
        }

        // дLimits particles to about 7-10 per splash, as to not lag everyone because some random creature decided
        // to glitch out near a pond
        private void Water_Update(ILContext il)
        {
            try
            {
                int numIndex = 0;
                int tmpIndex1 = 0;
                ILLabel label1 = null;
                var c = new ILCursor(il);
                var skip = il.DefineLabel();
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchBleUn(out label1),
                    i => i.MatchLdloc(out tmpIndex1),
                    i => i.MatchLdflda<BodyChunk>("vel"),
                    i => i.MatchLdfld<UnityEngine.Vector2>("y"),
                    i => i.MatchStloc(out numIndex)
                );
                c.MoveAfterLabels();
                c.EmitDelegate(() => OnlineManager.lobby != null);
                c.Emit(OpCodes.Brfalse, skip);
                c.Emit(OpCodes.Ldloc, numIndex);
                c.EmitDelegate((float value) =>
                {
                    float minVelY = -50.0F;
                    float maxVelY = 50.0F;
                    if (splashObjectCount >= 80 || value <= minVelY || value >= maxVelY)
                    { //dont spawn any splashes after N splashes over 40 ticks OR has too much velocity
                        return 0.0F;
                    }
                    splashObjectCount += (int)Mathf.Abs(num3 * ((num3 < 0f) ? 0.25f : 0.55f));
                    return Math.Max(Math.Min(value, 10.0F), -20.0F);
                });
                c.Emit(OpCodes.Stloc, numIndex);
                c.MarkLabel(skip);
            }
            catch (Exception e)
            {
                Logger.LogError(e);
            }
        }

        private void Room_Update(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel skip = null;
            int locuad = -1;

            // just closing up don't want to stray matches
            c.GotoNext(MoveType.After, // this.updateIndex = this.updateList.Count - 1; right before while(
                i => i.MatchLdcI4(1),
                i => i.MatchSub(),
                i => i.MatchStfld<Room>("updateIndex")
                );

            // surround this update call with a trycatch if in lobby, otherwise run vanilla
            // if ([...] & !flag){
            //     uad.Update(this.game.evenUpdate);
            // }
            // becomes
            // if ([...] & !flag && !HandledByMeadow(uad)){
            //     uad.Update(this.game.evenUpdate);
            // }
            c.GotoNext(MoveType.Before,
                i => i.MatchBrtrue(out skip),
                i => i.MatchLdloc(out locuad), // IL_09CD
                i => i.MatchLdarg(0),
                i => i.MatchLdfld<Room>("game"),
                i => i.MatchLdfld<RainWorldGame>("evenUpdate"),
                i => i.MatchCallvirt<UpdatableAndDeletable>("Update")
                );

            c.Index++;
            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locuad);
            c.EmitDelegate((Room room, UpdatableAndDeletable uad) =>
            {
                if (OnlineManager.lobby != null)
                {
                    try
                    {
                        uad.Update(room.game.evenUpdate);
                    }
                    catch (Exception e)
                    {
                        RainMeadow.Error($"Object update error for object {(uad is PhysicalObject po ? $"{po} - {po.abstractPhysicalObject.ID}" : uad)} in room {room.abstractRoom.name}");
                        RainMeadow.Error(e);
                    }
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brtrue, skip);

            // else if (updatableAndDeletable is PhysicalObject && !flag) IL_09FC
            // same logic as above we surround the whole thing in a trycatch for online lobbies
            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(locuad),
                i => i.MatchIsinst<PhysicalObject>(),
                i => i.MatchBrfalse(out skip),
                i => i.MatchLdloc(out _),
                i => i.MatchBrtrue(out skip)
                );

            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locuad);
            c.EmitDelegate((Room room, UpdatableAndDeletable uad) =>
            {
                if (OnlineManager.lobby != null)
                {
                    try
                    {
                        if ((uad as PhysicalObject).graphicsModule != null)
                        {
                            (uad as PhysicalObject).graphicsModule.Update();
                            (uad as PhysicalObject).GraphicsModuleUpdated(true, room.game.evenUpdate);
                        }
                        else
                        {
                            (uad as PhysicalObject).GraphicsModuleUpdated(false, room.game.evenUpdate);
                        }
                    }
                    catch (Exception e)
                    {
                        RainMeadow.Error($"Object post-update error for object {uad} in room {room.abstractRoom.name}");
                        RainMeadow.Error(e);
                    }
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brtrue, skip);

        }
    }
}
