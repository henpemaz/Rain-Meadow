using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Menu;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public static class ColorHelpers
    {
        public static string SetHSLString(Vector3 hsl)
        {
            return $"{hsl.x},{hsl.y},{hsl.z}";
        }
        public static Vector3 GetCustomColorHSL(this PlayerProgression progression, SlugcatStats.Name id, int bodyIndex)
        {
            Vector3 color = Vector3.one;
            if (bodyIndex >= 0 && progression.miscProgressionData.colorChoices.ContainsKey(id.value) && progression.miscProgressionData.colorChoices[id.value].Count > bodyIndex && progression.miscProgressionData.colorChoices[id.value][bodyIndex].Contains(","))
            {
                string[] hslArray = progression.miscProgressionData.colorChoices[id.value][bodyIndex].Split(',');
                color = new Vector3(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
            }
            return color;
        }
        public static List<Vector3> GetCustomColorHSLs(this PlayerProgression progression, SlugcatStats.Name id)
        {
            List<Vector3> list = [];
            if (progression.miscProgressionData.colorChoices.ContainsKey(id.value) && progression.miscProgressionData.colorChoices[id.value] != null)
            {
                for (int i = 0; i < progression.miscProgressionData.colorChoices[id.value].Count; i++)
                {
                    Vector3 hsl = Vector3.one;
                    string hslString = progression.miscProgressionData.colorChoices[id.value][i];
                    if (hslString.Contains(","))
                    {
                        string[] hslArray = hslString.Split(',');
                        hsl = new(float.Parse(hslArray[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(hslArray[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    }
                    list.Add(hsl);
                }
            }
            return list;
        }
        public static void SaveCustomColorHSL(this PlayerProgression progression, SlugcatStats.Name id, int bodyIndex, Vector3 hsl)
        {
            progression.SaveCustomColorHSL(id, bodyIndex, SetHSLString(hsl));
        }
        public static void SaveCustomColorHSL(this PlayerProgression progression, SlugcatStats.Name id, int bodyIndex, string hslString)
        {
            if (!progression.miscProgressionData.colorChoices.ContainsKey(id.value))
            {
                RainMeadow.Debug("WARNING! Failed to save color choices due to slugcat not saved");
                return;
            }
            if (progression.miscProgressionData.colorChoices[id.value].Count <= bodyIndex)
            {
                RainMeadow.Debug("WARNING! Failed to save color choices due to index being more than body count!");
                return;
            }
            progression.miscProgressionData.colorChoices[id.value][bodyIndex] = hslString;
        }
        public static bool IsCustomColorEnabled(this PlayerProgression progression, SlugcatStats.Name id)
        {
            return progression.miscProgressionData.colorsEnabled != null && 
                progression.miscProgressionData.colorsEnabled.ContainsKey(id.value) && 
                progression.miscProgressionData.colorsEnabled[id.value];
        }
        public static Vector3 RWHSLRange(Vector3 hsl)
        {
            return new(Mathf.Clamp(hsl.x, 0, 0.99f), hsl.y, Mathf.Clamp(hsl.z, 0.01f, 1));
        }
        public static Vector3 RWJollyPicRange(Vector3 hsl)
        {
            return new(hsl.x, hsl.y, Mathf.Clamp(hsl.z, 0.01f, 1));
        }
        public static Color HSL2RGB(Vector3 hsl)
        {
            return Custom.HSL2RGB(hsl.x % 1, hsl.y, hsl.z);
        }
        public static List<Color> GetCustomColors(this PlayerProgression progression, SlugcatStats.Name id)
        {
            return progression.IsCustomColorEnabled(id)? [..progression.GetCustomColorHSLs(id).Select(HSL2RGB)] : GetDefaultColors(id);
        }

        public static List<Color> GetDefaultColors(SlugcatStats.Name id)
        {
            return [..PlayerGraphics.DefaultBodyPartColorHex(id).Select(Custom.hexToColor)];
        }
    }
}
