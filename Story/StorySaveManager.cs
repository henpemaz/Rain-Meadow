using System;
using System.Collections.Generic;
using System.Linq;


namespace RainMeadow
{
    public class StorySaveManager
    {
        public List<StorySaveInfo> storyModeSaves;
        public StorySaveManager() { }

        public StorySaveInfo generateStorySave(SlugcatStats.Name campaign, string saveName) 
        {
            var allSlugcatNames = ExtEnum<SlugcatStats.Name>.values;
            if (campaign == null) 
            {
                throw new ArgumentNullException("Null campaign, bad dev D:");
            }
            if (string.IsNullOrWhiteSpace(saveName)) 
            {
                throw new ArgumentException("savename empty");
            }
            if (allSlugcatNames.entries.Contains(saveName)) 
            {
                throw new ArgumentException($"savename {saveName} already exists");
            }
            var temp = new StorySaveInfo(campaign, saveName);
            storyModeSaves.Add(temp);
            return temp;
        }

        public void init() 
        { 
            //TODO: Grab all storymode save names and generate the current list of `storyModeSaves`
        }

        public void saveToDisk() 
        { 
            //TODO: Save all current save names to a file somewhere....tbd
        }
    }
}
