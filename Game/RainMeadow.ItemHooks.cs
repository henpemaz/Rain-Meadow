using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Globalization;
using System.Linq;

namespace RainMeadow
{
    public partial class RainMeadow
    {
        private void ItemHooks()
        {
            IL.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
            
            APOFS += AbstractMeadowCollectible_APOFS;
            APOFS += SeedCob_APOFS;
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

            c.GotoNext(i => i.MatchNewobj<AbstractPhysicalObject>());
            c.GotoPrev(MoveType.After, i => i.MatchBr(out skip));
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


        public delegate AbstractPhysicalObject APOFSHandler(World world, string[] arr, EntityID entityID, AbstractPhysicalObject.AbstractObjectType apoType, WorldCoordinate pos);

        public static event APOFSHandler APOFS;
    }
}
