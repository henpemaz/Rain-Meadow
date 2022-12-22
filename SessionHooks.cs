using MonoMod.Cil;
using System.Linq;
using System;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    public static class SessionHooks
    {
        public static void Apply()
        {
            On.RainWorld.Start += RainWorld_Start;
        }

        private static void RainWorld_Start(On.RainWorld.orig_Start orig, RainWorld self)
        {
            orig(self);
            //On.RainWorld.Start -= RainWorld_Start;

            IL.RainWorldGame.ctor += RainWorldGame_ctor;
            IL.OverWorld.LoadFirstWorld += OverWorld_LoadFirstWorld;
            IL.World.ctor += World_ctor;
            IL.RainWorldGame.Update += RainWorldGame_Update;

            UnityEngine.Debug.LogError("OnlineSession registered");
        }

        private static void RainWorldGame_Update(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.Goto(il.Instrs[il.Instrs.Count - 1]);
                ILLabel notstory = null;
                c.GotoPrev(MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_IsStorySession"),
                    i => i.MatchBrfalse(out notstory),
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(0) || i.MatchDup(),
                    i => i.MatchLdfld<RainWorldGame>("updateAbstractRoom")
                    );

                c.Goto(notstory.Target);
                ILLabel skip = null;
                c.Prev.MatchBr(out skip);

                c.MoveAfterLabels();
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame game) => { return game.isOnlineSession(); });
                ILLabel theelse = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, theelse);

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame game) => { game.getOnlineSession().Update(); });

                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(theelse);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void World_ctor(ILContext il)
        {
            try
            {
                var c = new ILCursor(il);
                c.Goto(il.Instrs[il.Instrs.Count - 1]);
                c.GotoPrev(MoveType.After,
                    i => i.MatchStfld<World>("rainCycle")
                    );
                ILLabel skip = c.IncomingLabels.First();
                c.GotoPrev(MoveType.After, i => i.MatchBr(out var lab) && lab.Target == skip.Target);
                c.MoveAfterLabels();


                c.Emit(OpCodes.Ldarg_1);
                c.EmitDelegate((RainWorldGame game) => { return game.isOnlineSession(); });
                ILLabel theelse = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, theelse);


                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldc_R4, (float)(15f));
                c.Emit(OpCodes.Newobj, typeof(RainCycle).GetConstructor(new Type[] { typeof(World), typeof(float) }));
                c.Emit<World>(OpCodes.Stfld, "rainCycle");

                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(theelse);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void OverWorld_LoadFirstWorld(ILContext il)
        {
            try
            {
                var isVanilla = il.Method.Parameters[0].ParameterType.Name == "OverWorld";

                var c = new MonoMod.Cil.ILCursor(il);
                c.Goto(il.Instrs[il.Instrs.Count - 1]);
                c.GotoPrev(
                    i => i.MatchLdfld<StoryGameSession>("saveState"),
                    i => i.MatchLdfld<SaveState>("denPosition")
                    );
                ILLabel skip = null;
                c.GotoPrev(i => i.MatchBr(out skip));
                c.Index++;
                c.MoveAfterLabels();

                c.Emit(isVanilla ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                c.EmitDelegate((OverWorld self) => { return self.game.manager.menuSetup.startGameCondition == OnlineSession.EnumExt_OnlineSession.Online; });
                ILLabel story = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, story);
                c.Emit(OpCodes.Ldstr, "HI_C04");
                c.Emit(isVanilla ? OpCodes.Stloc_0 : OpCodes.Stloc_1); // CRS moment
                c.Emit(OpCodes.Ldc_I4_0);
                c.Emit(isVanilla ? OpCodes.Stloc_1 : OpCodes.Stloc_0);


                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(story);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private static void RainWorldGame_ctor(ILContext il)
        {
            try
            {
                // part 1 - create right session type
                var c = new MonoMod.Cil.ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchNewobj<StoryGameSession>(),
                    i => i.MatchStfld<RainWorldGame>("session")
                    );
                var skip = c.IncomingLabels.Last();

                c.GotoPrev(i => i.MatchBr(skip));
                c.Index++;
                // we're right before story block here hopefully
                c.MoveAfterLabels();

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate((RainWorldGame self) => { return self.manager.menuSetup.startGameCondition == OnlineSession.EnumExt_OnlineSession.Online; });
                ILLabel story = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, story);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Newobj, typeof(OnlineSession).GetConstructor(new Type[] { typeof(RainWorldGame) }));
                c.Emit<RainWorldGame>(OpCodes.Stfld, "session");
                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(story);

                // part 2 - player create
                ILLabel notstory = null;
                ILLabel create = il.DefineLabel();
                c.GotoNext(moveType: MoveType.Before,
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt<RainWorldGame>("get_IsStorySession"),
                    i => i.MatchBrfalse(out notstory),
                    i => i.MatchLdarg(0),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchLdstr("Slugcat")
                    );
                // not sure what do
                // if story, or session host, create player
                // session join does not ?

                // part 3 - no breakie if no player/creatures
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdfld<AbstractRoom>("creatures"),
                    i => i.MatchLdcI4(0),
                    i => i.MatchCallOrCallvirt(out _),
                    i => i.MatchStfld<RoomCamera>("followAbstractCreature")
                    );

                skip = c.IncomingLabels.Last();
                c.GotoPrev(MoveType.After, i => i.MatchBrtrue(skip));
                c.Emit(OpCodes.Br, skip); // don't run desperate bit of code that follows creatures[0]

            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }
    }
}