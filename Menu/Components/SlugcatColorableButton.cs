using MSCScugs = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using RWCustom;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class SlugcatColorableButton : IllustrationButton
    {
        public static string GetFileForSlugcat(SlugcatStats.Name? slugcat, bool isColored, bool isDead = false)
        {
            if (slugcat == null || isColored)
            {
                return GetFileForSlugcatIndex(slugcat, 0, isDead);
            }
            if (slugcat == SlugcatStats.Name.White || slugcat == SlugcatStats.Name.Yellow || slugcat == SlugcatStats.Name.Red)
            {
                return GetFileForSlugcatIndex(slugcat, slugcat == SlugcatStats.Name.White ? 0 : slugcat == SlugcatStats.Name.Yellow ? 1 : 2);
            }
            if (IsMSCSlugcat(slugcat))
            {
                return GetFileForSlugcatIndex(slugcat, 4, isDead);
            }
            return GetFileForSlugcatIndex(slugcat, 0, isDead);
        }
        public static string GetFileForSlugcatIndex(SlugcatStats.Name? slugcat, int colorIndex, bool isDead = false)
        {
            if (slugcat == null)
            {
                return $"Multiplayerportrait{colorIndex}2";
            }
            int deadIndex = isDead? 0 : 1;
            if (slugcat == SlugcatStats.Name.Night || (ModManager.Watcher && slugcat == Watcher.WatcherEnums.SlugcatStatsName.Watcher))
            {
                return $"Multiplayerportrait3{deadIndex}"; //no multi color support for night portrait yet
            }
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
        public SlugcatColorableButton(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 sizeOffset, SlugcatStats.Name slugcat, bool isColored) : base(menu, owner, pos, "", GetFileForSlugcat(slugcat, isColored))
        {
            size += sizeOffset;
            this.isColored = isColored;
            this.slugcat = slugcat;
        }

        public bool isColored;
        public SlugcatStats.Name? slugcat;
    }
}
