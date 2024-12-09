using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RainMeadow
{
    internal static class StoryMenuHelpers
    {
        #region Remix

        // TODO: make this per-gamemode?
        public static List<string> nonGameplayRemixSettings = new() { "cfgSpeedrunTimer", "cfgHideRainMeterNoThreat", "cfgLoadingScreenTips", "cfgExtraTutorials", "cfgClearerDeathGradients", "cfgShowUnderwaterShortcuts", "cfgBreathTimeVisualIndicator", "cfgCreatureSense", "cfgTickTock", "cfgFastMapReveal", "cfgThreatMusicPulse", "cfgExtraLizardSounds", "cfgQuieterGates", "cfgDisableScreenShake", "cfgHunterBatflyAutograb", "cfgNoMoreTinnitus" };

        internal static (Dictionary<string, bool> hostBoolSettings, Dictionary<string, float> hostFloatSettings, Dictionary<string, int> hostIntSettings) GetHostBoolStoryRemixSettings()
        {
            Dictionary<string, bool> configurableBools = new();
            Dictionary<string, float> configurableFloats = new();
            Dictionary<string, int> configurableInts = new();

            if (ModManager.MMF)
            {
                Type type = typeof(MoreSlugcats.MMF);

                FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);
                var sortedFields = fields.OrderBy(f => f.Name);

                foreach (FieldInfo field in sortedFields)
                {
                    if (nonGameplayRemixSettings.Contains(field.Name)) continue;
                    var reflectedValue = field.GetValue(null);
                    if (reflectedValue is Configurable<bool> boolOption)
                    {
                        configurableBools.Add(field.Name, boolOption._typedValue);
                    }

                    if (reflectedValue is Configurable<float> floatOption)
                    {
                        configurableFloats.Add(field.Name, floatOption._typedValue);
                    }

                    if (reflectedValue is Configurable<int> intOption)
                    {
                        configurableInts.Add(field.Name, intOption._typedValue);
                    }
                }
                RainMeadow.Debug(configurableBools);
                RainMeadow.Debug(configurableInts);
                RainMeadow.Debug(configurableFloats);
            }

            return (configurableBools, configurableFloats, configurableInts);
        }

        internal static void SetClientStoryRemixSettings(Dictionary<string, bool> hostBoolRemixSettings, Dictionary<string, float> hostFloatRemixSettings, Dictionary<string, int> hostIntRemixSettings)
        {
            Type type = typeof(MoreSlugcats.MMF);

            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

            var sortedFields = fields.OrderBy(f => f.Name);

            foreach (FieldInfo field in sortedFields)
            {
                var reflectedValue = field.GetValue(null);
                if (reflectedValue is Configurable<bool> boolOption)
                {
                    for (int i = 0; i < hostBoolRemixSettings.Count; i++)
                    {
                        if (field.Name == hostBoolRemixSettings.Keys.ElementAt(i) && boolOption._typedValue != hostBoolRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {boolOption._typedValue} does not match host's, setting to {hostBoolRemixSettings.Values.ElementAt(i)}");
                            boolOption._typedValue = hostBoolRemixSettings.Values.ElementAt(i);
                        }
                    }
                }

                if (reflectedValue is Configurable<float> floatOption)
                {
                    for (int i = 0; i < hostFloatRemixSettings.Count; i++)
                    {
                        if (field.Name == hostFloatRemixSettings.Keys.ElementAt(i) && floatOption._typedValue != hostFloatRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {floatOption._typedValue} does not match host's, setting to {hostFloatRemixSettings.Values.ElementAt(i)}");
                            floatOption._typedValue = hostFloatRemixSettings.Values.ElementAt(i);
                        }
                    }
                }

                if (reflectedValue is Configurable<int> intOption)
                {
                    for (int i = 0; i < hostIntRemixSettings.Count; i++)
                    {

                        if (field.Name == hostIntRemixSettings.Keys.ElementAt(i) && intOption._typedValue != hostIntRemixSettings.Values.ElementAt(i))
                        {
                            RainMeadow.Debug($"Remix Key: {field.Name} with value {intOption._typedValue} does not match host's, setting to {hostIntRemixSettings.Values.ElementAt(i)}");
                            intOption._typedValue = hostIntRemixSettings.Values.ElementAt(i);
                        }
                    }
                }
            }
        }
        #endregion

        public static void RemoveMenuObjects(params MenuObject?[] objs)
        {
            foreach (var obj in objs)
            {
                if (obj is not null)
                {
                    obj.RemoveSprites();
                    obj.owner.RemoveSubObject(obj);
                }
            }
        }
    }
}
