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
            var oe = new MeadowPersonaSettings(OnlineManager.lobby.PlayerFromId(newPersonaSettingsEvent.owner), newPersonaSettingsEvent.entityId);

            try
            {
                map.Add(OnlineManager.lobby.PlayerFromId(newPersonaSettingsEvent.owner), oe);
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

        internal override void ApplyCustomizations(AbstractCreature creature, OnlinePhysicalObject oe)
        {
            if(creature.realizedCreature is Player player)
            {
                MeadowCustomization.CreatureCustomization customization = new MeadowCustomization.CreatureCustomization();
                customization.skinData = MeadowProgression.skinData[skin];
                customization.tint = new(Mathf.Clamp01(tint.r), Mathf.Clamp01(tint.g), Mathf.Clamp01(tint.b)); // trust nobody
                customization.tintAmount = Mathf.Clamp01(tintAmount) * MeadowProgression.skinData[skin].tintFactor;

                MeadowCustomization.creatureCustomizations.Add(player, customization);
            }
        }

        public class MeadowPersonaSettingsState : PersonaSettingsState
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
                var meadowPersonaSettings = (MeadowPersonaSettings)onlineEntity;
                skin = (short)(meadowPersonaSettings.skin?.Index ?? -1);
                tintAmount = meadowPersonaSettings.tintAmount;
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
                meadowPersonaSettings.tintAmount = tintAmount;
                meadowPersonaSettings.tint.r = tintR / 255f;
                meadowPersonaSettings.tint.g = tintG / 255f;
                meadowPersonaSettings.tint.b = tintB / 255f;
            }
        }
    }
}