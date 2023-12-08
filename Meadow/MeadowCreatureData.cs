using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCreatureData : EntityData
    {
        public OnlineCreature owner;
        internal TickReference emotesTick; // tick of primary resource owner
        internal float emotesLife; // seconds
        internal List<EmoteType> emotes = new();
        internal byte emotesVersion;
        internal Player.InputPackage input;

        public MeadowCreatureData(OnlineCreature owner)
        {
            this.owner = owner;
            this.emotesTick = new TickReference(OnlineManager.lobby.owner);
        }

        internal override EntityDataState MakeState(OnlineResource inResource)
        {
            if(inResource is RoomSession)
            {
                return new MeadowCreatureDataState(this);
            }
            return null;
        }
    }

    public abstract class EntityData
    {
        protected EntityData() { }

        internal abstract EntityDataState MakeState(OnlineResource inResource);
    }

    [DeltaSupport(level = StateHandler.DeltaSupport.NullableDelta)]
    public abstract class EntityDataState : OnlineState
    {
        protected EntityDataState() { }

        internal abstract void ReadTo(OnlineEntity onlineEntity);
    }


    public class MeadowCreatureDataState : EntityDataState
    {
        [OnlineField(nullable=true)]
        public Generics.AddRemoveSortedExtEnums<EmoteType> emotes;
        [OnlineField]
        public TickReference emotesTick;
        [OnlineFieldHalf]
        public float emotesLife;
        [OnlineField]
        public byte emotesVersion;
        [OnlineField(group = "inputs")]
        public ushort inputs;
        [OnlineFieldHalf(group = "inputs")]
        public float analogInputX;
        [OnlineFieldHalf(group = "inputs")]
        public float analogInputY;

        public MeadowCreatureDataState() { }
        public MeadowCreatureDataState(MeadowCreatureData meadowCreatureData)
        {
            emotes = new(meadowCreatureData.emotes.ToList());
            emotesVersion = meadowCreatureData.emotesVersion;
            emotesLife = meadowCreatureData.emotesLife;
            emotesTick = meadowCreatureData.emotesTick;

            var i = meadowCreatureData.input;
            inputs = (ushort)(
                  (i.x == 1 ? 1 << 0 : 0)
                | (i.x == -1 ? 1 << 1 : 0)
                | (i.y == 1 ? 1 << 2 : 0)
                | (i.y == -1 ? 1 << 3 : 0)
                | (i.downDiagonal == 1 ? 1 << 4 : 0)
                | (i.downDiagonal == -1 ? 1 << 5 : 0)
                | (i.pckp ? 1 << 6 : 0)
                | (i.jmp ? 1 << 7 : 0)
                | (i.thrw ? 1 << 8 : 0)
                | (i.mp ? 1 << 9 : 0));

            analogInputX = i.analogueDir.x;
            analogInputY = i.analogueDir.y;
        }

        internal override void ReadTo(OnlineEntity onlineEntity)
        {
            if(onlineEntity is OnlineCreature oc && oc.gameModeData is MeadowCreatureData mcd)
            {
                mcd.emotes = emotes.list;
                mcd.emotesVersion = emotesVersion;
                mcd.emotesLife = emotesLife;
                mcd.emotesTick = emotesTick;

                Player.InputPackage i = default;
                if (((inputs >> 0) & 1) != 0) i.x = 1;
                if (((inputs >> 1) & 1) != 0) i.x = -1;
                if (((inputs >> 2) & 1) != 0) i.y = 1;
                if (((inputs >> 3) & 1) != 0) i.y = -1;
                if (((inputs >> 4) & 1) != 0) i.downDiagonal = 1;
                if (((inputs >> 5) & 1) != 0) i.downDiagonal = -1;
                if (((inputs >> 6) & 1) != 0) i.pckp = true;
                if (((inputs >> 7) & 1) != 0) i.jmp = true;
                if (((inputs >> 8) & 1) != 0) i.thrw = true;
                if (((inputs >> 9) & 1) != 0) i.mp = true;
                i.analogueDir.x = analogInputX;
                i.analogueDir.y = analogInputY;
                mcd.input = i;
            }
            else
            {
                RainMeadow.Error("Failed to read MeadowCreatureDataState into " + onlineEntity);
            }
        }
    }
}