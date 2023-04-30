using UnityEngine;

namespace RainMeadow;

public abstract partial class CreatureEvent
{
    public class Violence : OnlineEvent
    {
        private OnlineEntity OnlineVillain; // can be null
        private OnlineEntity OnlineVictim;
        private byte VictimChunkIndex;
        private AppendageRef VictimAppendage; // can be null
        private Vector2? DirectionAndMomentum;
        private byte DamageType;
        private float Damage;
        private float StunBonus;
        
        public Violence() { }
        public Violence(OnlineEntity onlineVillain, OnlineEntity onlineVictim, int victimChunkIndex, PhysicalObject.Appendage.Pos victimAppendage, Vector2? directionAndMomentum, Creature.DamageType damageType, float damage, float stunBonus)
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
            serializer.SerializeNullable(ref OnlineVillain);
            serializer.Serialize(ref OnlineVictim);
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

            var victim = (Creature)OnlineVictim.entity.realizedObject;
            victim.Violence(OnlineVillain?.entity.realizedObject.firstChunk, DirectionAndMomentum, victim.bodyChunks[VictimChunkIndex], CastVictimAppendage, CastDamageType, Damage, StunBonus);
        }
    }
}