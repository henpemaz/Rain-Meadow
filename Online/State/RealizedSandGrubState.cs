using System;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    public class RealizedSandGrubState : RealizedCreatureState
    {


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

            grub.tailLength = tailLength;
            grub.tetherLength = tetherLength;
            grub.lastTetherLength = lastTetherLength;
            grub.lastMouthOpen = lastMouthOpen;
            grub.BitesLeft = bites;
            grub.tentacleHuntCounter = tentacleHuntCounter;
            grub.buryCounter = buryCounter;
           
        }
    }
}

