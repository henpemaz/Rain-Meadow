using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void ObjectHooks()
        {
            IL.Room.Update += Room_Update;

            IL.ScavengerOutpost.ctor += ScavengerOutpost_ctor1; ;
        }

        private void ScavengerOutpost_ctor1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel skip = null;

            c.GotoNext(MoveType.Before,
                i => i.MatchLdsfld<Futile>(nameof(Futile.atlasManager)),
                i => i.MatchLdstr("outpostSkulls"),
                i => i.MatchCallvirt<FAtlasManager>(nameof(FAtlasManager.DoesContainAtlas)),
                i => i.MatchBrtrue(out skip));

            c.GotoPrev(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchNewobj<List<ScavengerOutpost.PearlString>>(),
                i => i.MatchStfld<ScavengerOutpost>(nameof(ScavengerOutpost.pearlStrings)));

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate((ScavengerOutpost self, Random.State state) =>
            {
                if (OnlineManager.lobby != null && (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var session) || !session.isOwner))
                {
                    // need to run this last bit here due to some weird error when trying to define a label.
                    Random.state = state;
                    if (!Futile.atlasManager.DoesContainAtlas("outpostSkulls"))
                    {
                        Futile.atlasManager.LoadAtlas("Atlases/outPostSkulls");
                    }
                    return false;
                }
                return true;
            });
            c.Emit(OpCodes.Brfalse, skip);
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
