using System;
using System.Linq;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    public class RealizedSandGrubState : RealizedCreatureState
    {
        // TODO reduce this to only whats needed.
        [OnlineField]
        short burrowIndex;
        [OnlineFieldHalf]
        public float tailLength;
        [OnlineFieldHalf]
        public float lastTetherLength;
        [OnlineFieldHalf]
        public float tetherLength;
        [OnlineFieldHalf]
        public float lastMouthOpen;
        [OnlineField]
        int bites;
        [OnlineField]
        public int buryCounter;
        [OnlineField]
        public int tentacleHuntCounter;
        public RealizedSandGrubState() { }
        public RealizedSandGrubState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            SandGrub grub = (SandGrub)onlineEntity.realizedCreature;

            var burrow = grub.burrow;
            burrowIndex = -1;
            if (burrow != null)
            {
                burrowIndex = GetBurrowIndex(grub.burrow);
            }

            tailLength = grub.tailLength;
            tetherLength = grub.tetherLength;
            lastTetherLength = grub.lastTetherLength;
            lastMouthOpen = grub.lastMouthOpen;
            bites = grub.BitesLeft;
            buryCounter = grub.buryCounter;
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not SandGrub grub) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            if (burrowIndex < 0)
            {
                RainMeadow.Error($"target {onlineEntity} is without a burrow. {burrowIndex}");
            }
            else
            {
                var newBurrow = GetBurrow(grub, burrowIndex);
                if (newBurrow != null)
                {
                    grub.burrow = newBurrow;
                }
                else
                {
                    RainMeadow.Error($"target {onlineEntity} burrow index was out of range or grub {grub} null: expected {burrowIndex} but got {grub.room.updateList.OfType<SandGrubBurrow>().Count()}");
                }
            }

            grub.tailLength = tailLength;
            grub.tetherLength = tetherLength;
            grub.lastTetherLength = lastTetherLength;
            grub.lastMouthOpen = lastMouthOpen;
            grub.BitesLeft = bites;
            grub.tentacleHuntCounter = tentacleHuntCounter;
            grub.buryCounter = buryCounter;
        }

        public static short GetBurrowIndex(SandGrubBurrow burrow)
        {
            var burrows = burrow.room.updateList.OfType<SandGrubBurrow>().ToList();
            for (int i = 0; i < burrows.Count; i++)
            {
                if (burrows[i] == burrow) return (short)i;
            }
            return -1;
        }

        public static SandGrubBurrow GetBurrow(SandGrub grub, int index)
        {
            if (grub == null || grub.room == null) return null;
            var burrows = grub.room.updateList.OfType<SandGrubBurrow>().ToList();
            if (index >= 0 && index < burrows.Count)
            {
                return burrows[index];
            }
            return null;
        }
    }
}