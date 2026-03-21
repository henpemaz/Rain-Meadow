using System.Collections.Generic;
using OverseerHolograms;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    [OnlineState.DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public class OverseerHologramState : OnlineState
    {
        [OnlineFieldHalf]
        public Vector2 pos;

        [OnlineField(group = "message")]
        public string message;
        
        [OnlineField(nullable: true)] 
        public OnlineEntity.EntityId? communicateWith;

        public OverseerHologramState() { }
        public OverseerHologramState(OverseerHologram hologram)
        {
            pos = hologram.pos;
            message = hologram.message.value;
            communicateWith = hologram.communicateWith?.abstractCreature?.GetOnlineCreature()?.id;
        }

        public void MakeHologram(Overseer overseer) 
        {
            RainMeadow.DebugMe();
            if (overseer.hologram is not null)
            {
                overseer.hologram.stillRelevant = false;
                overseer.hologram = null;
            }
            if (message == RainMeadow.Ext_OverseerHologram_Message.OverseerEmote.value)
            {
                if (CreatureController.creatureControllers.TryGetValue(overseer, out var controller) 
                        && controller is OverseerController overseerController && overseerController.latestEmoteDisplayer is not null && overseerController.latestEmote is not null)
                {
                    overseerController.AddEmoteLocal(overseerController.latestEmoteDisplayer, overseerController.latestEmote);
                }
            }
        }

        public void ReadToHologram(OverseerHologram hologram)
        {
            hologram.stillRelevant = true;
            hologram.pos = pos;
        }
    }


    // TODO: The overseer type should be synced in abstractcreaturedef
    //          maybe we could hijack the abscreature customdata?
    // TODO: When zipping, the body and the mycelium gets stretched along the zip.
    //          Should be fixable by ensuring DrawPosOfSegment returns what we want, 
    //          and throwing in a well timed reset on the graphics module
    // TODO: Death is completely broken, need to make Die or HitSomethingWithoutStopping an event.
    //          this leads to an overseer being fake killed on remote.
    public class RealizedOverseerState : RealizedCreatureState
    {
        [OnlineField(group = "owner_iterator")]
        private int ownerIterator;  // our guide is 1, vanilla goes from 0-5, sandbox uses 10+
        [OnlineField]
        private IntVector2 rootTile;
        [OnlineField]
        private IntVector2 hoverTile;
        [OnlineFieldHalf]
        private Vector2 lookAt;
        [OnlineField]
        private Overseer.Mode mode;
        [OnlineField(nullable = true)]
        private OnlinePhysicalObject? conversationPartner;

        [OnlineField(nullable: true, group = "hologram")]
        private OverseerHologramState? hologramstate;

        public RealizedOverseerState() { }
        public RealizedOverseerState(OnlineCreature entity) : base(entity)
        {
            Overseer o = (Overseer)entity.apo.realizedObject;

            ownerIterator = (o.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator;
            rootTile = o.rootTile;
            hoverTile = o.hoverTile;
            mode = o.mode;
            lookAt = o.AI.lookAt;
            conversationPartner = o.conversationPartner?.abstractCreature.GetOnlineObject();
            if (o.hologram is not null && !o.hologram.slatedForDeletetion)
            {
                hologramstate = new OverseerHologramState(o.hologram);
                RainMeadow.Debug("Hologram State");
            }
            else
            {
                hologramstate = null;
            }
        }
        public override bool ShouldPosBeLenient(PhysicalObject po)
        {
            if (po is Overseer o) return true;
            return base.ShouldPosBeLenient(po);
        }

        public readonly HashSet<Overseer.Mode> withdrawn_modes = [ Overseer.Mode.SittingInWall ];
        public readonly HashSet<Overseer.Mode> transition_modes = [ Overseer.Mode.Emerging, Overseer.Mode.Withdrawing, Overseer.Mode.Zipping ]; 

        public override void ReadTo(OnlineEntity onlineEntity)
        {
            base.ReadTo(onlineEntity);
            if ((onlineEntity as OnlineCreature).realizedCreature is not Overseer overseer) { RainMeadow.Error("target not realized: " + onlineEntity); return; }

            (overseer.abstractCreature.abstractAI as OverseerAbstractAI).ownerIterator = ownerIterator;

            if (!transition_modes.Contains(overseer.mode))
            {
                if (overseer.mode != mode)
                {
                    if (mode == Overseer.Mode.SittingInWall)
                    {
                        overseer.SwitchModes(Overseer.Mode.Withdrawing);
                        overseer.afterWithdrawMode = Overseer.Mode.SittingInWall;
                    }
                    else if (overseer.mode == Overseer.Mode.SittingInWall)
                    {
                        overseer.SwitchModes(Overseer.Mode.Emerging);
                    }
                    else if (!transition_modes.Contains(mode))
                    {
                        overseer.SwitchModes(mode);  
                    }
                }

                if (overseer.rootTile != rootTile)
                {
                    overseer.FindZipPath(rootTile, hoverTile);
                    if (overseer.mode == Overseer.Mode.SittingInWall)
                    {
                        overseer.SwitchModes(Overseer.Mode.Zipping);
                    }
                    else
                    {
                        overseer.nextHoverTile = hoverTile;
                        overseer.afterWithdrawMode = Overseer.Mode.Zipping;
                        overseer.SwitchModes(Overseer.Mode.Withdrawing);
                    }
                }
                else
                {
                    overseer.hoverTile = hoverTile;
                }
            }
            
            
            overseer.conversationPartner = conversationPartner?.apo?.realizedObject as Overseer;

            if (hologramstate is not null)
            {
                if (overseer.hologram is not null && overseer.hologram.message.value != hologramstate.message)
                {
                    overseer.hologram.stillRelevant = false;
                    overseer.hologram = null;
                } 

                if (overseer.hologram is null) hologramstate.MakeHologram(overseer);
                if (overseer.hologram is not null) hologramstate.ReadToHologram(overseer.hologram);
            }
            else
            {
                overseer.AI.lookAt = lookAt;
                if (overseer.hologram is not null)
                {
                    overseer.hologram.stillRelevant = false;
                    overseer.hologram = null;
                }
            }
        }
    }
}
