using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class StorySaveInfo
    {
        public string campaignName { get; private set; }
        public SlugcatStats.Name save { get; private set; }
        private StorySaveInfo() { }
        public StorySaveInfo(SlugcatStats.Name campaign, string saveName) 
        {
            campaignName = campaign.value;
            ExtEnum<SlugcatStats.Name>.values.AddEntry(saveName);
            if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), saveName, false, out var rawEnumBase)) 
            {
                throw new Exception($"Unable to generate save: {saveName}");
            }
            save = (SlugcatStats.Name)rawEnumBase;
        }

        public override string ToString()
        {
            return $"{campaignName}|{save.value}";
        }

        public static bool tryParse(string campaign, string saveName, out StorySaveInfo output) 
        {
            output = new StorySaveInfo();

            ExtEnum<SlugcatStats.Name>.values.AddEntry(saveName);
            if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), saveName, false, out var rawEnumBase))
            {
                return false;
            }

            output.campaignName = campaign;
            output.save = (SlugcatStats.Name)rawEnumBase;
            return true;
        }
    }
}
