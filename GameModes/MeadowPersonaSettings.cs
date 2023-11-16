using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowPersonaSettings : PersonaSettingsEntity
    {
        internal float tintAmount;
        internal MeadowProgression.Skin skin;
        internal Color tint;

        public static ConditionalWeakTable<OnlinePlayer, MeadowPersonaSettings> map = new();

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
            var oe = new MeadowPersonaSettings(newPersonaSettingsEvent.owner, newPersonaSettingsEvent.entityId);

            try
            {
                map.Add(newPersonaSettingsEvent.owner, oe);
                OnlineManager.recentEntities.Add(oe.id, oe);
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
                RainMeadow.Error(Environment.StackTrace);
            }
            return oe;
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new MeadowPersonaSettingsState(this, tick);
        }

        internal override void ApplyCustomizations(Creature creature, OnlinePhysicalObject oe)
        {
            MeadowCustomization.CreatureCustomization customization = MeadowCustomization.creatureCustomizations.GetOrCreateValue(creature);
            customization.skinData = MeadowProgression.skinData[skin];
            customization.tint = new(tint.r, tint.g, tint.b);
            customization.tintAmount = tintAmount * MeadowProgression.skinData[skin].tintFactor;
        }

        public class MeadowPersonaSettingsState : PersonaSettingsState
        {
            [OnlineField]
            public short skin;
            [OnlineField]
            public byte tintAmount;
            [OnlineField]
            public byte tintR;
            [OnlineField]
            public byte tintG;
            [OnlineField]
            public byte tintB;

            public MeadowPersonaSettingsState() : base() { }

            public MeadowPersonaSettingsState(OnlineEntity onlineEntity, uint ts) : base(onlineEntity, ts)
            {
                var meadowPersonaSettings = (MeadowPersonaSettings)onlineEntity;
                skin = (short)(meadowPersonaSettings.skin?.Index ?? -1);
                tintAmount = (byte)(meadowPersonaSettings.tintAmount * 255);
                tintR = (byte)(meadowPersonaSettings.tint.r * 255);
                tintG = (byte)(meadowPersonaSettings.tint.g * 255);
                tintB = (byte)(meadowPersonaSettings.tint.b * 255);
            }

            public override void ReadTo(OnlineEntity onlineEntity)
            {
                base.ReadTo(onlineEntity);
                var meadowPersonaSettings = (MeadowPersonaSettings)onlineEntity;
                if(meadowPersonaSettings == null)
                {
                    RainMeadow.Debug("meadowPersonaSettings is null");
                    RainMeadow.Debug(onlineEntity.GetType().FullName);

                }
                meadowPersonaSettings.skin = new MeadowProgression.Skin(MeadowProgression.Skin.values.GetEntry(skin));
                meadowPersonaSettings.tintAmount = tintAmount / 255f;
                meadowPersonaSettings.tint.r = tintR / 255f;
                meadowPersonaSettings.tint.g = tintG / 255f;
                meadowPersonaSettings.tint.b = tintB / 255f;
            }
        }
    }
}