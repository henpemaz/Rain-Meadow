using System;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowPersonaSettings : PersonaSettingsEntity
    {
        internal float tintAmount;
        internal MeadowProgression.Skin skin;
        internal Color tint;

        public MeadowPersonaSettings(OnlinePlayer owner, EntityId id) : base(owner, id)
        {
            RainMeadow.Debug(this);
        }

        public override NewEntityEvent AsNewEntityEvent(OnlineResource onlineResource)
        {
            RainMeadow.Debug(this);
            return new NewMeadowPersonaSettingsEvent(this);
        }

        public static MeadowPersonaSettings FromEvent(NewMeadowPersonaSettingsEvent newPersonaSettingsEvent, OnlineResource inResource)
        {
            RainMeadow.Debug(newPersonaSettingsEvent);
            return new MeadowPersonaSettings(OnlineManager.lobby.PlayerFromId(newPersonaSettingsEvent.owner), newPersonaSettingsEvent.entityId);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            RainMeadow.Debug(this);
            return new MeadowPersonaSettingsState(this, tick);
        }

        public class MeadowPersonaSettingsState : EntityState
        {
            [OnlineField]
            public short skin;
            [OnlineField]
            public float tintAmount;
            [OnlineField]
            public byte tintR;
            [OnlineField]
            public byte tintG;
            [OnlineField]
            public byte tintB;

            public MeadowPersonaSettingsState() : base() { }

            public MeadowPersonaSettingsState(OnlineEntity onlineEntity, uint ts) : base(onlineEntity, ts)
            {
                RainMeadow.Debug("creating state");
                var meadowPersonaSettings = onlineEntity as MeadowPersonaSettings;
                skin = (short)(meadowPersonaSettings.skin?.Index ?? -1);
                tintAmount = meadowPersonaSettings.tintAmount;
                tintR = (byte)(meadowPersonaSettings.tint.r * 255);
                tintG = (byte)(meadowPersonaSettings.tint.g * 255);
                tintB = (byte)(meadowPersonaSettings.tint.b * 255);
                RainMeadow.Debug("done creating state");
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                RainMeadow.Debug("reading state");
                var meadowPersonaSettings = onlineEntity as MeadowPersonaSettings;
                meadowPersonaSettings.skin = new MeadowProgression.Skin(MeadowProgression.Skin.values.GetEntry(skin));
                meadowPersonaSettings.tintAmount = tintAmount;
                meadowPersonaSettings.tint.r = tintR / 255f;
                meadowPersonaSettings.tint.g = tintG / 255f;
                meadowPersonaSettings.tint.b = tintB / 255f;
                RainMeadow.Debug("done reading state");
            }
        }
    }
}