using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System.Globalization;
using System.Linq;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void ItemHooks()
        {
            IL.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
            On.SaveState.ReportConsumedItem += SaveState_ReportConsumedItem;
            APOFS += AbstractMeadowCollectible_APOFS;

            // Seedcobs are cursed
            APOFS += SeedCob_APOFS;
            IL.SeedCob.PlaceInRoom += SeedCob_PlaceInRoom;
        }

        private void SeedCob_PlaceInRoom(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            // basically, move that base() call to the bottom of the method

            // Part 1: don't run base() if online
            ILLabel skip1 = c.DefineLabel();
            c.GotoNext(MoveType.AfterLabel,
                i => i.MatchLdarg(0),
                i => i.MatchLdarg(1),
                i => i.MatchCallOrCallvirt<PhysicalObject>("PlaceInRoom")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((SeedCob self) =>
            {
                if (OnlineManager.lobby != null)
                {
                    return false;
                }
                return true;
            });
            c.Emit(OpCodes.Brfalse, skip1);
            c.Index += 3;
            c.MarkLabel(skip1);


            // part 2 run at bottom of file
            ILLabel skip2 = c.DefineLabel();
            c.Index = c.Instrs.Count;
            c.GotoPrev(MoveType.AfterLabel,
                i => i.MatchRet());
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate((SeedCob self) =>
            {
                if (OnlineManager.lobby != null)
                {
                    return true;
                }
                return false;
            });
            c.Emit(OpCodes.Brfalse, skip2);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit<PhysicalObject>(OpCodes.Call, "PlaceInRoom");
            c.MarkLabel(skip2);
        }

        // not handled by vanilla apofs for whathever freaking reason
        private AbstractPhysicalObject SeedCob_APOFS(World world, string[] array, EntityID entityID, AbstractPhysicalObject.AbstractObjectType apoType, WorldCoordinate pos)
        {
            if (apoType == AbstractPhysicalObject.AbstractObjectType.SeedCob)
            {
                return new SeedCob.AbstractSeedCob(world, null, pos, entityID, int.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture), int.Parse(array[4], NumberStyles.Any, CultureInfo.InvariantCulture), false, null);
            }
            return null;
        }

        private static AbstractPhysicalObject AbstractMeadowCollectible_APOFS(World world, string[] arr, EntityID entityID, AbstractPhysicalObject.AbstractObjectType apoType, WorldCoordinate pos)
        {
            if (apoType == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed
                || apoType == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue
                || apoType == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold
                )
            {
                return new AbstractMeadowCollectible(world, apoType, pos, entityID);
            }
            else if (apoType == RainMeadow.Ext_PhysicalObjectType.MeadowGhost)
            {
                return new AbstractMeadowGhost(world, apoType, pos, entityID);
            }
            else if (apoType == RainMeadow.Ext_PhysicalObjectType.MeadowPlant)
            {
                return new AbstractMeadowCollectible(world, apoType, pos, entityID);
            }
            return null;
        }

        private static void SaveState_AbstractPhysicalObjectFromString(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ILLabel skip = null;
            var cont = c.DefineLabel();

            // resolve locals instead of hardcoding
            var stringArrayType = il.Module.ImportReference(typeof(string[]));
            var arrLoc = il.Body.Variables.First(v => v.VariableType.FullName == stringArrayType.FullName);
            var idType = il.Module.ImportReference(typeof(EntityID));
            var idLoc = il.Body.Variables.First(v => v.VariableType.FullName == idType.FullName);
            var abstractObjectTypeType = il.Module.ImportReference(typeof(AbstractPhysicalObject.AbstractObjectType));
            var typeLoc = il.Body.Variables.First(v => v.VariableType.FullName == abstractObjectTypeType.FullName);
            var posType = il.Module.ImportReference(typeof(WorldCoordinate));
            var posLoc = il.Body.Variables.First(v => v.VariableType.FullName == posType.FullName);
            var abstractObjectType = il.Module.ImportReference(typeof(AbstractPhysicalObject));
            var apoLoc = il.Body.Variables.First(v => v.VariableType.FullName == abstractObjectType.FullName);

            // insert:
            // else if (MeadowHandledType(...)) { // no op, everything hapens in the call }
            // else if (IsTypeConsumable) ...

            // navigate to end clause new AbstractPhysicalObject
            c.GotoNext(i => i.MatchNewobj<AbstractPhysicalObject>());
            c.GotoPrev(MoveType.Before, i => i.MatchLdarg(0)); // start of else body
            // navigate to generic IsTypeConsumable handling
            var consumablefail = c.IncomingLabels.First().Branches.First(); // we are in the else to that if
            c.GotoPrev(MoveType.After, i => i.MatchBr(out skip)); // the instruction before is a jump to the end of the whole ifelse chain, we'll need it too

            c.MoveAfterLabels();
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, arrLoc);
            c.Emit(OpCodes.Ldloc, idLoc);
            c.Emit(OpCodes.Ldloc, typeLoc);
            c.Emit(OpCodes.Ldloc, posLoc);
            c.Emit(OpCodes.Ldloca, apoLoc);
            c.EmitDelegate(
                (World world, string[] arr, EntityID entityID, AbstractPhysicalObject.AbstractObjectType apoType, WorldCoordinate pos, out AbstractPhysicalObject result) =>
                {
                    result = APOFS.InvokeWhileNull<AbstractPhysicalObject>(world, arr, entityID, apoType, pos);
                    return result != null;
                });
            c.Emit(OpCodes.Brfalse, cont);
            c.Emit(OpCodes.Br, skip);
            c.MarkLabel(cont);
        }

        private static void SaveState_ReportConsumedItem(On.SaveState.orig_ReportConsumedItem orig, SaveState self, World world, bool karmaFlower, int originroom, int placedObjectIndex, int waitCycles)
        {
            orig(self, world, karmaFlower, originroom, placedObjectIndex, waitCycles);
            if (OnlineManager.lobby != null && !OnlineManager.lobby.isOwner)
            {
                OnlineManager.lobby.owner.InvokeRPC(ConsumableRPCs.reportConsumedItem, karmaFlower, originroom, placedObjectIndex, waitCycles);
            }
        }

        public delegate AbstractPhysicalObject APOFSHandler(World world, string[] arr, EntityID entityID, AbstractPhysicalObject.AbstractObjectType apoType, WorldCoordinate pos);

        public static event APOFSHandler APOFS;
    }
}
