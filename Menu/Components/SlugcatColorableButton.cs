﻿using MSCScugs = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RWCustom;
using UnityEngine;
using System.IO;

namespace RainMeadow.UI.Components
{
    public class SlugcatColorableButton : IllustrationButton
    {
        public static string GetFileForSlugcat(SlugcatStats.Name? slugcat, bool isColored, bool isDead = false)
        {
            if (slugcat == null || slugcat == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat || isColored)
                return GetFileForSlugcatIndex(slugcat, 0, isDead);
            if (slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Yellow || slugcat == SlugcatStats.Name.Red)
                return GetFileForSlugcatIndex(slugcat, slugcat == SlugcatStats.Name.White ? 0 : slugcat == SlugcatStats.Name.Yellow ? 1 : 2, isDead);
            if (IsMSCSlugcat(slugcat)) return GetFileForSlugcatIndex(slugcat, 4, isDead);
            for (int i = 4; i > 0; i--)
            {
                string txt = GetFileForSlugcatIndex(slugcat, i, isDead);
                if (File.Exists(AssetManager.ResolveFilePath($"illustrations/{txt}.png")))
                    return txt;
            }
            return GetFileForSlugcatIndex(slugcat, 0, isDead);
        }
        public static string GetFileForSlugcatIndex(SlugcatStats.Name? slugcat, int colorIndex, bool isDead = false, bool randomizeSofSlugcatPortrait = true)
        {
            if ((slugcat is null) || (slugcat == RainMeadow.Ext_SlugcatStatsName.OnlineRandomSlugcat))
            {
                return $"Multiplayerportrait{colorIndex}2";
            }
            int deadIndex = isDead ? 0 : 1;
            if (slugcat == SlugcatStats.Name.Night || (ModManager.Watcher && slugcat == Watcher.WatcherEnums.SlugcatStatsName.Watcher))
            {
                return $"Multiplayerportrait3{deadIndex}"; //no multi color support for night portrait yet
            }
            if (ModManager.MSC && slugcat == MSCScugs.Sofanthiel && randomizeSofSlugcatPortrait)
                return $"Multiplayerportrait{UnityEngine.Random.Range(0, 5)}{deadIndex}-{slugcat.value}";

            return $"Multiplayerportrait{(ModManager.MSC && slugcat == MSCScugs.Slugpup ? 4 : colorIndex)}{deadIndex}-{slugcat.value}";
        }
        public static bool IsMSCSlugcat(SlugcatStats.Name slugcat)
        {
            if (!ModManager.MSC)
            {
                return false;
            }
            return slugcat == MSCScugs.Gourmand || slugcat == MSCScugs.Artificer || slugcat == MSCScugs.Rivulet || slugcat == MSCScugs.Spear || slugcat == MSCScugs.Saint || slugcat == MSCScugs.Slugpup || slugcat == MSCScugs.Sofanthiel;
        }
        public SlugcatColorableButton(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 sizeOffset, SlugcatStats.Name? slugcat, bool isColored, bool isDead = false, string signal = "") : base(menu, owner, pos, "", GetFileForSlugcat(slugcat, isColored, isDead), signal)
        {
            size += sizeOffset;
            this.isColored = isColored;
            this.slugcat = slugcat;
        }
        public override Color InterpColor(float timeStacker, HSLColor baseColor)
        {
            Color baseInterpCol = base.InterpColor(timeStacker, baseColor);
            return keepSecondaryHueBase ? LerpedSecondaryHSLColor(baseInterpCol.ToHSL()) : LerpedSecondaryColor(baseInterpCol);
        }
        public override Color MyPortraitColor(Color? portraitColor, float timeStacker)
        {
            Color basePortraitColor = base.MyPortraitColor(portraitColor, timeStacker);
            return keepSecondaryHuePortrait ? LerpedSecondaryHSLColor(basePortraitColor.ToHSL(), portraitSecondaryLerpFactor) : LerpedSecondaryColor(basePortraitColor, portraitSecondaryLerpFactor);
        }
        public virtual Color LerpedSecondaryColor(Color color, float lerpFactor = 1)
        {
            Color lerpWith = secondaryColor.GetValueOrDefault(Color.clear);
            return Color.Lerp(color, lerpWith, lerpWith.a * lerpFactor);
        }
        public virtual Color LerpedSecondaryHSLColor(HSLColor hslCol, float lerpFactor = 1) //this uses secondaryColHue then baseCol saturation and lightness with lerp
        {
            if (secondaryColor == null)
            {
                return hslCol.rgb;
            }
            float alpha = secondaryColor.Value.a;
            float actualLerpFactor = lerpFactor * alpha;
            HSLColor sec = secondaryColor.Value.ToHSL();
            return new HSLColor(Mathf.Lerp(hslCol.hue, sec.hue, lerpFactor), Mathf.Lerp(hslCol.saturation, sec.saturation, actualLerpFactor), Mathf.Lerp(hslCol.lightness, sec.lightness, actualLerpFactor)).rgb;
        }
        public void LoadNewSlugcat(SlugcatStats.Name? slugcat, bool isColored, bool isDead)
        {
            if (this.slugcat != slugcat || this.isColored != isColored || this.isDead != isDead)
            {
                this.slugcat = slugcat;
                this.isColored = isColored;
                this.isDead = isDead;
                SetNewImage("", GetFileForSlugcat(this.slugcat, this.isColored, this.isDead));
            }
        }
        public void LoadNewSlugcat(SlugcatStats.Name? slugcat, int colorIndex, bool isDead, bool randomizeSofPortrait = false)
        {
            if (this.slugcat != slugcat || (this.colorIndex != colorIndex) || this.isDead != isDead)
            {
                this.slugcat = slugcat;
                this.colorIndex = colorIndex;
                this.isDead = isDead;
                SetNewImage("", GetFileForSlugcatIndex(this.slugcat, colorIndex, isDead, randomizeSofPortrait));
            }
        }
        public float portraitSecondaryLerpFactor = 1;
        public int colorIndex = 0;
        public bool isColored, isDead, keepSecondaryHuePortrait, keepSecondaryHueBase;
        public Color? secondaryColor;
        public SlugcatStats.Name? slugcat;
    }
}
