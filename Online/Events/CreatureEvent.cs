using RWCustom;
using UnityEngine;

namespace RainMeadow;

public abstract partial class CreatureEvent
{
    public class Violence : OnlineEvent
    {
        private OnlinePhysicalObject OnlineVillain; // can be null
        private OnlineCreature OnlineVictim;
        private byte VictimChunkIndex;
        private AppendageRef VictimAppendage; // can be null
        private Vector2? DirectionAndMomentum;
        private byte DamageType;
        private float Damage;
        private float StunBonus;
        
        public Violence() { }
        public Violence(OnlinePhysicalObject onlineVillain, OnlineCreature onlineVictim, int victimChunkIndex, PhysicalObject.Appendage.Pos victimAppendage, Vector2? directionAndMomentum, Creature.DamageType damageType, float damage, float stunBonus)
        {
            OnlineVillain = onlineVillain;
            OnlineVictim = onlineVictim;
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
            serializer.SerializeEntityNullable(ref OnlineVillain);
            serializer.SerializeEntity(ref OnlineVictim);
            serializer.Serialize(ref VictimChunkIndex);
            serializer.SerializeNullable(ref VictimAppendage);
            serializer.Serialize(ref DirectionAndMomentum);
            serializer.Serialize(ref DamageType);
            serializer.Serialize(ref Damage);
            serializer.Serialize(ref StunBonus);
        }

        public override void Process()
        {
            var CastDamageType = new Creature.DamageType(Creature.DamageType.values.GetEntry(DamageType));
            var CastVictimAppendage = VictimAppendage?.GetAppendagePos(OnlineVictim);

            var victim = (Creature)OnlineVictim.apo.realizedObject;
            victim.Violence(OnlineVillain?.apo.realizedObject.firstChunk, DirectionAndMomentum, victim.bodyChunks[VictimChunkIndex], CastVictimAppendage, CastDamageType, Damage, StunBonus);
        }
    }

    public class SuckedIntoShortCut : OnlineEvent
    {
        OnlineCreature suckedCreature;
        IntVector2 entrancePos;
        bool carriedByOther;
        
        public SuckedIntoShortCut() { }
        public SuckedIntoShortCut(OnlineCreature suckedCreature, IntVector2 entrancePos, bool carriedByOther)
        {
            this.suckedCreature = suckedCreature;
            this.entrancePos = entrancePos;
            this.carriedByOther = carriedByOther;
        }
        public override EventTypeId eventType => EventTypeId.CreatureEventSuckedIntoShortCut;

        public override void CustomSerialize(Serializer serializer)
        {
            base.CustomSerialize(serializer);
            serializer.SerializeEntity(ref suckedCreature);
            serializer.Serialize(ref entrancePos.x);
            serializer.Serialize(ref entrancePos.y);
        }

        public override void Process()
        {
            suckedCreature.enteringShortCut = true;
            var creature = (Creature)suckedCreature.apo.realizedObject;
            creature.SuckedIntoShortCut(entrancePos, carriedByOther);
        }
    }
}