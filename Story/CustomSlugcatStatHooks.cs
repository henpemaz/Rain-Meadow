using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static RainMeadow.RainMeadow;
using UnityEngine;

namespace RainMeadow
{
    public partial class RainMeadow
    {

     
        private int SlugcatStats_NourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugcatStats.Name slugcatIndex, IPlayerEdible eatenobject)
        {

            if (isStoryMode(out var storyGameMode))
            {

                int pip = 0;

                if (slugcatIndex == Ext_SlugcatStatsName.OnlineStoryRed)
                {
                    bool flag = true;
                    if (eatenobject is Centipede || eatenobject is VultureGrub || eatenobject is Hazer || eatenobject is EggBugEgg || eatenobject is SmallNeedleWorm || eatenobject is JellyFish)
                    {
                        flag = false;
                    }

                    pip = ((!flag) ? (pip + 4 * eatenobject.FoodPoints) : (pip + eatenobject.FoodPoints));
                }
                else if (slugcatIndex == Ext_SlugcatStatsName.OnlineStoryWhite || slugcatIndex == Ext_SlugcatStatsName.OnlineStoryYellow) // TODO: MSC Support one day
                {
                    pip = (!ModManager.MSC ? (pip + 4 * eatenobject.FoodPoints) : (pip + 2));
                }
                return pip;
            }

            return orig(slugcatIndex, eatenobject);

        }

        private RWCustom.IntVector2 SlugcatStats_SlugcatFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugcatStats.Name slugcat)
        {
            if (isStoryMode(out var storyGameMode))
            {
                if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryWhite)
                {
                    return new RWCustom.IntVector2(7, 4);
                }
                if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryYellow)
                {
                    return new RWCustom.IntVector2(5, 3);
                }
                if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryRed)
                {
                    return new RWCustom.IntVector2(9, 6);
                }
            }
            return orig(slugcat);

        }

        private void SlugcatStats_ctor(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugcatStats.Name slugcat, bool malnourished)
        {

            orig(self, slugcat, malnourished);

            if (OnlineManager.lobby.gameMode is ArenaCompetitiveGameMode)
            {
                self.throwingSkill = 1;
            }

            if (isStoryMode(out var storyGameMode))
            {

                if (OnlineManager.lobby == null) return;


                if (slugcat == Ext_SlugcatStatsName.OnlineStoryWhite) 
                {

                    self.throwingSkill = 1;
                    self.name = slugcat;


                }

                if (slugcat == Ext_SlugcatStatsName.OnlineStoryYellow)
                {

                    self.bodyWeightFac = 0.95f;
                    self.generalVisibilityBonus = -0.1f;
                    self.visualStealthInSneakMode = 0.6f;
                    self.loudnessFac = 0.75f;
                    self.lungsFac = 1.2f;
                    self.throwingSkill = 0;
                    self.name = slugcat;


                }


                if (slugcat == Ext_SlugcatStatsName.OnlineStoryRed)
                {

                    self.runspeedFac = 1.2f;
                    self.bodyWeightFac = 1.12f;
                    self.generalVisibilityBonus = 0.1f;
                    self.visualStealthInSneakMode = 0.3f;
                    self.loudnessFac = 1.35f;
                    self.throwingSkill = 2;
                    self.poleClimbSpeedFac = 1.25f;
                    self.corridorClimbSpeedFac = 1.2f;
                    self.name = slugcat;


                }

                if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryWhite)
                {
                    self.maxFood = 7;
                    self.foodToHibernate = 4;
                }
                else if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryYellow)
                {
                    self.maxFood = 5;
                    self.foodToHibernate = 3;
                }
                else if (storyGameMode.currentCampaign == Ext_SlugcatStatsName.OnlineStoryRed)
                {
                    self.maxFood = 9;
                    self.foodToHibernate = 6;
                }


                else
                {

                    self.bodyWeightFac = Mathf.Min(self.bodyWeightFac, 0.9f);
                    self.runspeedFac = 0.875f;
                    self.poleClimbSpeedFac = 0.8f;
                    self.corridorClimbSpeedFac = 0.86f;

                }
                if (malnourished)
                {
                    self.throwingSkill = 0;
                }
            }

        }
    }
}
