using Mono.Cecil.Cil;
using MonoMod.Cil;
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

            IL.ScavengerOutpost.ctor += ScavengerOutpost_ctor1;
            IL.ScavengerOutpost.Update += ScavengerOutpost_Update;

            //IL.MoreSlugcats.HangingPearlString.ctor += HangingPearlString_ctor;
        }

        //private void HangingPearlString_ctor(ILContext il)
        //{
        //    throw new NotImplementedException();
        //}

        private void ScavengerOutpost_Update(ILContext il)
        {
            var c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(1),
                i => i.MatchCall<UpdatableAndDeletable>(nameof(UpdatableAndDeletable.Update)));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((ScavengerOutpost self) =>
            {
                if (OnlineManager.lobby != null && self.pearlStrings.Count == 0 && RoomSession.map.TryGetValue(self.room.abstractRoom, out var session) && session.isOwner)
                {
                    // late Spawn and initiate pearl strings
                    Random.State state = Random.state;
                    Random.InitState((self.placedObj.data as PlacedObject.ScavengerOutpostData).pearlsSeed);
                    int length = Random.Range(5, 15);
                    for (int i = 0; i < length; i++)
                    {
                        var pearlString = new ScavengerOutpost.PearlString(self.room, self, 20f + Mathf.Lerp(20f, 150f, UnityEngine.Random.value) * Custom.LerpMap(length, 5f, 15f, 1f, 0.1f));
                        self.room.AddObject(pearlString);
                        self.pearlStrings.Add(pearlString);

                        pearlString.Initiate();
                    }
                    Random.state = state;
                }
            });
        }

        private void ScavengerOutpost_ctor1(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel skip = null;

            c.GotoNext(MoveType.After,
                i => i.MatchLdloc(0),
                i => i.MatchCall<Random.State>("set_state"));

            skip = c.DefineLabel();

            c.GotoPrev(MoveType.After,
                i => i.MatchLdarg(0),
                i => i.MatchNewobj<List<ScavengerOutpost.PearlString>>(),
                i => i.MatchStfld<List<ScavengerOutpost.PearlString>>(nameof(ScavengerOutpost.pearlStrings)));

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((ScavengerOutpost self) =>
            {
                if (OnlineManager.lobby != null && (!RoomSession.map.TryGetValue(self.room.abstractRoom, out var session) || !session.isOwner))
                {
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
