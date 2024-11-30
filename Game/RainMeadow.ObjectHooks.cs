using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void ObjectHooks()
        {
            IL.Room.Update += Room_Update;
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
