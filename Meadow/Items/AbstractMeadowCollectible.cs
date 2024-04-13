using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    // todo plug into progression system

    // todo progression system prototype
    public class AbstractMeadowCollectible : AbstractPhysicalObject
    {
        public bool placed;
        public bool collectedLocally;
        public bool collected;
        public TickReference collectedTR;
        public int collectedAt;
        const int duration = 40 * 10;
        public OnlinePhysicalObject online;

        internal bool Expired => collected && world.game.clock > collectedAt + duration;

        public AbstractMeadowCollectible(World world, AbstractObjectType type, WorldCoordinate pos, EntityID ID) : base(world, type, null, pos, ID)
        {

        }

        public override void Update(int time)
        {
            base.Update(time);

            if(online == null)
            {
                online = OnlinePhysicalObject.map.GetValue(this, (apo) => throw new KeyNotFoundException(apo.ToString()));
            }
            
            if (Expired && online.isMine)
            {
                RainMeadow.Debug("Expired:" + online);
                this.Destroy();
                this.Room.RemoveEntity(this);
            }
        }

        public void Collect()
        {
            if (collectedLocally) { return; }
            RainMeadow.Debug("Collected locally:" + online);
            collectedLocally = true;
            MeadowProgression.ItemCollected(this);

            if (collected) { return; }
            if (online.isMine)
            {
                NowCollected();
            }
            else
            {
                online.owner.InvokeRPC(CollectRemote, online);
            }
        }

        private void NowCollected()
        {
            if (!online.isMine) { throw new InvalidProgrammerException("not owner: " + online); }
            if (collected) { return; }
            RainMeadow.Debug("Collected:" + online);
            var ws = world.GetResource();
            collected = true;
            collectedAt = world.game.clock;
            collectedTR = ws.owner.MakeTickReference();

            OnlineManager.lobby.owner.InvokeRPC(MeadowGameMode.ItemConsumed, (byte)ws.ShortId(), type);
        }

        [RPCMethod]
        public static void CollectRemote(OnlinePhysicalObject online)
        {
            RainMeadow.Debug("Collect remote!:" + online);
            if (online != null && online.isMine && online.apo is AbstractMeadowCollectible amc)
            {
                amc.NowCollected();
            }
            else
            {
                RainMeadow.Error($"{online != null} && {online?.isMine} && {online?.apo is AbstractMeadowCollectible}");
            }
        }

        public override void Realize()
        {
            base.Realize(); // important for hooks!
            if (this.realizedObject != null)
            {
                return;
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowPlant)
            {
                this.realizedObject = new MeadowPlant(this);
            }
            if (type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed
                || type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue
                || type == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold
                )
            {
                this.realizedObject = new MeadowCollectToken(this);
            }
        }

        public static void Enable()
        {
            IL.SaveState.AbstractPhysicalObjectFromString += SaveState_AbstractPhysicalObjectFromString;
            APOFS += AbstractMeadowCollectible_APOFS;
        }

        private static AbstractPhysicalObject AbstractMeadowCollectible_APOFS(World world, string[] arr, EntityID entityID, AbstractObjectType apoType, WorldCoordinate pos)
        {
            RainMeadow.Debug(apoType);
            if (apoType == RainMeadow.Ext_PhysicalObjectType.MeadowTokenRed
                || apoType == RainMeadow.Ext_PhysicalObjectType.MeadowTokenBlue
                || apoType == RainMeadow.Ext_PhysicalObjectType.MeadowTokenGold
                )
            {
                return new AbstractMeadowCollectible(world, apoType, pos, entityID);
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
                    RainMeadow.Debug("funny delegate");
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
