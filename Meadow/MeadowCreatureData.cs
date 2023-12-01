using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCreatureData : EntityData
    {
        public OnlineCreature owner;
        internal uint emotesTick;
        internal float emotesLife;
        internal List<EmoteType> emotes = new();
        internal byte emotesVersion;

        public MeadowCreatureData(OnlineCreature owner)
        {
            this.owner = owner;
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
        internal uint emotesTick;
        [OnlineFieldHalf]
        internal float emotesLife;
        [OnlineField]
        internal byte emotesVersion;

        public MeadowCreatureDataState() { }
        public MeadowCreatureDataState(MeadowCreatureData meadowCreatureData)
        {
            emotes = new(meadowCreatureData.emotes);
            emotesVersion = meadowCreatureData.emotesVersion;
            emotesLife = meadowCreatureData.emotesLife;
            emotesTick = meadowCreatureData.emotesTick;

            if (Input.GetKey(KeyCode.L))
            {
                RainMeadow.Debug($"sending {emotesTick} {emotesLife} {emotesVersion}");
                RainMeadow.Debug(string.Join("-", emotes.list.Select(e => e.value)));
            }
        }

        internal override void ReadTo(OnlineEntity onlineEntity)
        {
            if(onlineEntity is OnlineCreature oc && oc.gameModeData is MeadowCreatureData mcd)
            {
                mcd.emotes = emotes.list;
                mcd.emotesVersion = emotesVersion;
                mcd.emotesLife = emotesLife;
                mcd.emotesTick = emotesTick;

                if (Input.GetKey(KeyCode.L))
                {
                    RainMeadow.Debug($"reading {emotesTick} {emotesLife} {emotesVersion}");
                    RainMeadow.Debug(string.Join("-", emotes.list.Select(e => e.value)));
                }
            }
        }
    }

}