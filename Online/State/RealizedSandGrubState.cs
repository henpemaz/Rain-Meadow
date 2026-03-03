using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Watcher;

namespace RainMeadow
{
    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class RealizedSandGrubState : RealizedCreatureState
    {
        [OnlineField]
        byte bites;
        [OnlineField(nullable = true)]
        SandGrubBurrowState? burrow;

        [OnlineFieldHalf]
        Vector2 head;
        [OnlineFieldHalf]
        float tetherLength;

        [OnlineField(group = "counters")]
        int returnToBurrowCounter;

        public RealizedSandGrubState() { }
        public RealizedSandGrubState(OnlineCreature onlineEntity) : base(onlineEntity)
        {
            SandGrub grub = (SandGrub)onlineEntity.realizedCreature;

            bites = (byte)grub.BitesLeft;
            if (grub.burrow != null)
            {
                burrow = new(grub.burrow);
            }

            returnToBurrowCounter = grub.returnToBurrowCounter;
            tetherLength = grub.tetherLength;

            if (grub.Big)
            {
                head = grub.tentacle.Tip.pos;
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).apo.realizedObject is not SandGrub grub) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            grub.BitesLeft = bites;
            if (burrow == null)
            {
                if (grub.burrow != null)
                {
                    grub.SwitchBurrow(null);
                }
            }
            else
            {
                burrow.ReadTo(grub.burrow.network);
            }

            grub.returnToBurrowCounter = returnToBurrowCounter;
            grub.tetherLength = tetherLength;

            if (grub.Big)
            {
                grub.tentacle.Tip.pos = head;
            }
        }
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class SandGrubBurrowState : OnlineState
    {
        [OnlineField(nullable = true)]
        OnlinePhysicalObject? grub;
        [OnlineFieldHalf]
        Vector2 pos;
        [OnlineFieldHalf]
        Vector2 dir;
        public SandGrubBurrowState() { }

        public SandGrubBurrowState(SandGrubBurrow burrow)
        {
            grub = burrow.grub?.abstractCreature?.GetOnlineObject();
            pos = burrow.pos;
            dir = burrow.dir;
        }


        public void ReadTo(SandGrubNetwork network)
        {
            foreach(var networkedBurrow in network.burrows)
            {
                if (networkedBurrow.pos.CloseEnough(pos, 1/4f))
                {
                    ReadTo(networkedBurrow);
                    break;
                }
            }
        }

        public void ReadTo(SandGrubBurrow burrow)
        {
            burrow.pos = pos;
            burrow.dir = dir;

            var po = grub?.apo?.realizedObject;
            if (po != burrow.grub)
            {
                if (po is null)
                {
                    burrow.grub = null;
                }
                else
                {
                    (po as SandGrub).SwitchBurrow(burrow);
                }
            }
        }
    }

    //[DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    //public class SandGrubNetworkState : OnlineState
    //{
    //    [OnlineField]
    //    Generics.DynamicOrderedStates<SandGrubBurrowState> burrows;
    //    public SandGrubNetworkState() { }

    //    public SandGrubNetworkState(SandGrubNetwork network)
    //    {
    //        List<SandGrubBurrowState> list = new();
    //        for(int i = 0; i < network.burrows.Count; i++)
    //        {
    //            list.Add(new(network.burrows[i], i));
    //        }
    //    }

    //    public void ReadTo(SandGrubNetwork network)
    //    {
    //        for(int i = 0; i < burrows.list.Count; i++)
    //        {
    //            burrows.list[i].ReadTo(network.burrows[i], i);
    //        }
    //    }
    //}
}