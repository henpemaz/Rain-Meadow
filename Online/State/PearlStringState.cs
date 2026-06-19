using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RainMeadow.Generics;

namespace RainMeadow
{
    public class PearlStringState : OnlineEntity.EntityState
    {
        [OnlineField]
        public bool[] connected;

        [OnlineField("pearls")]
        public DynamicOrderedEntityIDs pearlEntityIDs;

        public static OnlineEntity.EntityId nullPearlID = new OnlineEntity.EntityId(0, OnlineEntity.EntityId.IdType.none, 0);

        public PearlStringState() : base() { }
        public PearlStringState(OnlinePearlString onlineEntity, OnlineResource inResource, uint ts) : base(onlineEntity, inResource, ts)
        {
            List<AbstractConsumable> pearls;
            List<bool> activeConnections;
            switch (onlineEntity.pearlString)
            {
                case ScavengerOutpost.PearlString outpostPearlString:
                    pearls = outpostPearlString.pearls;
                    activeConnections = outpostPearlString.activeConnections;
                    break;
                case MoreSlugcats.HangingPearlString hangingPearlString:       
                    pearls = hangingPearlString.pearls;
                    activeConnections = hangingPearlString.activeConnections;     
                    break;
                default:
                    throw new InvalidProgrammerException("implemente this");
            }

            if (activeConnections.Count > byte.MaxValue) throw new InvalidOperationException("too many connections");
            connected = activeConnections.ToArray();
            var pearlids = pearls.Select(x => x.GetOnlineObject()?.id).ToList();
            if (pearlids.Contains(null)) 
            {
                pearlEntityIDs = new DynamicOrderedEntityIDs(new List<OnlineEntity.EntityId>()); // can't update
            }
            else
            {
                pearlEntityIDs = new DynamicOrderedEntityIDs(pearlids); // can't update            
            }
        }

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if (!pearlEntityIDs.list.Any()) return;
            OnlinePearlString pearlString = onlineEntity as OnlinePearlString ?? throw new InvalidOperationException("not pearl string");
            List<AbstractConsumable?> pearls = pearlEntityIDs.list.Select(x => (x.FindEntity() as OnlineConsumable)?.apo as AbstractConsumable).ToList();
            if (pearls.Contains(null)) return;

            for (int i = 0; i < Math.Min(pearls.Count, connected.Length); i++)
            {
                if (pearls[i].realizedObject is PhysicalObject obj)
                {
                    if (obj.grabbedBy.Any())
                    {
                        connected[i] = false;
                    }
                }
            }

            switch (pearlString.pearlString)
            {
                case ScavengerOutpost.PearlString outpostPearlString:
                    if (!pearlString.initializedPearls) 
                    {
                        pearlString.initializedPearls = true;
                        foreach (var pearl in outpostPearlString.pearls) pearl.Destroy();
                    }
                    outpostPearlString.pearls = pearls;
                    outpostPearlString.activeConnections = connected.ToList();
                    break;
                case MoreSlugcats.HangingPearlString hangingPearlString:       
                    if (!pearlString.initializedPearls) 
                    {
                        pearlString.initializedPearls = true;
                        foreach (var pearl in hangingPearlString.pearls)  pearl.Destroy();
                    }
                    hangingPearlString.pearls = pearls;
                    hangingPearlString.activeConnections = connected.ToList();
                    break;
                default:
                    throw new InvalidProgrammerException("implemente this");
            }

        }


    }
}