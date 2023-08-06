using RWCustom;
using UnityEngine;

namespace RainMeadow;

public abstract partial class CreatureEvent
{
    public class Violence : OnlineEvent
    {
        private OnlineEntity.EntityId OnlineVillain; // can be null
        private OnlineEntity.EntityId OnlineVictim;
        private byte VictimChunkIndex;
        private AppendageRef VictimAppendage; // can be null
        private Vector2? DirectionAndMomentum;
        private byte DamageType;
        private float Damage;
        private float StunBonus;

        public Violence() { }
        public Violence(OnlinePhysicalObject onlineVillain, OnlineCreature onlineVictim, int victimChunkIndex, PhysicalObject.Appendage.Pos victimAppendage, Vector2? directionAndMomentum, Creature.DamageType damageType, float damage, float stunBonus)
        {
            OnlineVillain = onlineVillain?.id;
            OnlineVictim = onlineVictim.id;
            VictimChunkIndex = (byte)victimChunkIndex;
            VictimAppendage = victimAppendage != null ? new AppendageRef(victimAppendage) : null;
            DirectionAndMomentum = directionAndMomentum;
            DamageType = (byte)damageType;
            Damage = damage;
            StunBonus = stunBonus;
        }
        public override EventTypeId eventType => EventTypeId.CreatureEventViolence;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeNullable(ref OnlineVillain);
            serializer.Serialize(ref OnlineVictim);
            serializer.Serialize(ref VictimChunkIndex);
            serializer.SerializeNullable(ref VictimAppendage);
            serializer.SerializeNullable(ref DirectionAndMomentum);
            serializer.Serialize(ref DamageType);
            serializer.Serialize(ref Damage);
            serializer.Serialize(ref StunBonus);
        }

        public override void Process()
        {
            var victim = OnlineVictim.FindEntity() as OnlineCreature ?? throw new System.Exception("Entity not found: " + OnlineVictim);
            var villain = OnlineVillain?.FindEntity() as OnlinePhysicalObject;
            var CastDamageType = new Creature.DamageType(Creature.DamageType.values.GetEntry(DamageType));
            var CastVictimAppendage = VictimAppendage?.GetAppendagePos(victim);

            var victimCreature = (Creature)victim.apo.realizedObject;
            victimCreature.Violence(villain?.apo.realizedObject.firstChunk, DirectionAndMomentum, victimCreature.bodyChunks[VictimChunkIndex], CastVictimAppendage, CastDamageType, Damage, StunBonus);
        }
    }

    public class SuckedIntoShortCut : OnlineEvent
    {
        private OnlineEntity.EntityId suckedCreature;
        private IntVector2 entrancePos;
        private bool carriedByOther;

        public SuckedIntoShortCut() { }
        public SuckedIntoShortCut(OnlineCreature suckedCreature, IntVector2 entrancePos, bool carriedByOther)
        {
            this.suckedCreature = suckedCreature.id;
            this.entrancePos = entrancePos;
            this.carriedByOther = carriedByOther;
        }
        public override EventTypeId eventType => EventTypeId.CreatureEventSuckedIntoShortCut;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.Serialize(ref suckedCreature);
            serializer.Serialize(ref entrancePos.x);
            serializer.Serialize(ref entrancePos.y);
        }

        public override void Process()
        {
            var creature = suckedCreature.FindEntity() as OnlineCreature ?? throw new System.Exception("Entity not found: " + suckedCreature);
            creature.enteringShortCut = true;
            (creature.apo.realizedObject as Creature)?.SuckedIntoShortCut(entrancePos, carriedByOther);
        }
    }
}