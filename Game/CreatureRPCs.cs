using System;
using System.Collections.Generic;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {

        [RPCMethod]
        void CreatureGrabRPC(OnlineEntity.EntityId creatureID, GraspRef graspRef)
        {
            if (creatureID.FindEntity() is not OnlineCreature oc) return;
            if (oc.realizedCreature is null) return;
            if (graspRef.onlineGrabbed.FindEntity() is not OnlinePhysicalObject obj) return;
            if (obj.apo.realizedObject is null) return;

            graspRef.MakeGrasp(oc.realizedCreature, obj.apo.realizedObject);
        }
        
    }
}