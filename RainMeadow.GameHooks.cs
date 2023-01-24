using MonoMod.Cil;
using System.Linq;
using System;
using Mono.Cecil.Cil;

namespace RainMeadow
{
    partial class RainMeadow
    {
        private void RainWorldGame_ctor(ILContext il)
        {
            try
            {
                // part 1 - create right session type
                //else
                //{
                //    this.session = new StoryGameSession(manager.rainWorld.progression.PlayingAsSlugcat, this);
                //}
                // ========== becomes ===========
                //else if (self.manager.menuSetup.startGameCondition == OnlineSession.Ext_OnlineSession.Online)
                //{
                //    this.session = new OnlineSession(manager.rainWorld.progression.PlayingAsSlugcat, this);
                //}
                //else
                //{
                //    this.session = new StoryGameSession(manager.rainWorld.progression.PlayingAsSlugcat, this);
                //}
                var c = new MonoMod.Cil.ILCursor(il);
                c.GotoNext(moveType: MoveType.After,
                    i => i.MatchLdarg(0),
                    i => i.MatchLdarg(1),
                    i => i.MatchLdfld<ProcessManager>("rainWorld"),
                    i => i.MatchLdfld<RainWorld>("progression"),
                    i => i.MatchCallvirt<PlayerProgression>("get_PlayingAsSlugcat"),
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
                c.EmitDelegate((RainWorldGame self) => { return self.manager.menuSetup.startGameCondition == Ext_StoryGameInitCondition.Online; });
                ILLabel story = il.DefineLabel();
                c.Emit(OpCodes.Brfalse, story);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Newobj, typeof(OnlineSession).GetConstructor(new Type[] { typeof(RainWorldGame) }));
                c.Emit<RainWorldGame>(OpCodes.Stfld, "session");
                c.Emit(OpCodes.Br, skip);
                c.MarkLabel(story);

                //// part 3 - no breakie if no player/creatures
                //c.GotoNext(moveType: MoveType.After,
                //    i => i.MatchLdfld<AbstractRoom>("creatures"),
                //    i => i.MatchLdcI4(0),
                //    i => i.MatchCallOrCallvirt(out _),
                //    i => i.MatchStfld<RoomCamera>("followAbstractCreature")
                //    );

                //skip = c.IncomingLabels.Last();
                //c.GotoPrev(MoveType.After, i => i.MatchBrtrue(skip));
                //c.Emit(OpCodes.Br, skip); // don't run desperate bit of code that follows creatures[0]
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        private void WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues_LoadingContext(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues_LoadingContext orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues, WorldLoader.LoadingContext context)
        {
            if (game.session is OnlineSession os && !os.ShouldWorldLoadCreatures(game))
            {
                setupValues.worldCreaturesSpawn = false;
            }
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues, context);
        }

        private void WorldLoader_ctor_RainWorldGame_Name_bool_string_Region_SetupValues(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self, RainWorldGame game, SlugcatStats.Name playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (game.session is OnlineSession os && !os.ShouldWorldLoadCreatures(game))
            {
                setupValues.worldCreaturesSpawn = false;
            }
            orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
        }
    }
}
