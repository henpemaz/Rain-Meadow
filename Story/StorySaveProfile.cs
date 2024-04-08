using System;
using System.Collections.Generic;
using System.Linq;

namespace RainMeadow
{
    public class StorySaveProfile
    {
        public string campaignName { get; private set; }
        public string displayName { get; private set; }
        public SlugcatStats.Name save { get; private set; }
        private StorySaveProfile() { }
        public StorySaveProfile(SlugcatStats.Name campaign, string saveName) 
        {
            campaignName = campaign.value;
            displayName = saveName;
            var actualSaveName = campaignName + ":" + displayName;
            ExtEnum<SlugcatStats.Name>.values.AddEntry(actualSaveName);
            if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), actualSaveName, false, out var rawEnumBase)) 
            {
                throw new Exception($"Unable to generate save: {actualSaveName}");
            }
            save = (SlugcatStats.Name)rawEnumBase;
        }

        public override string ToString()
        {
            return $"{campaignName}|{displayName}";
        }

        public static bool tryParse(string campaign, string saveName, out StorySaveProfile output) 
        {
            output = new StorySaveProfile();

            var actualSaveName = campaign + ":" + saveName;
            ExtEnum<SlugcatStats.Name>.values.AddEntry(actualSaveName);
            if (!ExtEnumBase.TryParse(typeof(SlugcatStats.Name), actualSaveName, false, out var rawEnumBase))
            {
                return false;
            }

            output.campaignName = campaign;
            output.displayName = saveName;
            output.save = (SlugcatStats.Name)rawEnumBase;
            return true;
        }
    }
}
