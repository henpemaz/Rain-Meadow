using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class StorySaveInfo
    {
        public string campaignName { get; private set; }
        public SlugcatStats.Name save { get; private set; }
        public StorySaveInfo(SlugcatStats.Name campaign, string saveName) 
        {
            campaignName = campaign.value;
            ExtEnum<SlugcatStats.Name>.values.AddEntry(saveName);
            if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), saveName, false, out var rawEnumBase)) 
            {
                throw new Exception($"Unable to generate save: {saveName}");
            }
            save = rawEnumBase as SlugcatStats.Name;
        }
    }
}
