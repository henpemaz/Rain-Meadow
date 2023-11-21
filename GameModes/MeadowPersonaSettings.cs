using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowPersonaSettings : PersonaSettingsEntity
    {
        public float tintAmount;
        public MeadowProgression.Skin skin;
        public Color tint;

        public static ConditionalWeakTable<OnlinePlayer, MeadowPersonaSettings> map = new();

        public MeadowPersonaSettings(EntityDefinition entityDefinition) : base(entityDefinition)
        {
            RainMeadow.Debug(this);
            map.Add(owner, this);
        }

        public static MeadowPersonaSettings FromDefinition(MeadowPersonaSettingsDefinition meadowPersonaSettingsDefinition, OnlineResource inResource)
        {
            RainMeadow.Debug(meadowPersonaSettingsDefinition);
            return new MeadowPersonaSettings(meadowPersonaSettingsDefinition);
        }

        protected override EntityState MakeState(uint tick, OnlineResource inResource)
        {
            return new MeadowPersonaSettingsState(this, tick);
        }

        public override void ApplyCustomizations(Creature creature, OnlinePhysicalObject oe)
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
                if (meadowPersonaSettings == null)
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