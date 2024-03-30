using System;
using System.Collections.Generic;
using System.Linq;
using Menu.Remix.MixedUI;
using Kittehface.Framework20;

namespace RainMeadow
{
    public class StorySaveManager
    {
        public static StorySaveManager instance { get; private set; }
        public List<StorySaveProfile> storyModeSaves { get; private set; } = new List<StorySaveProfile>();
        private static string fileName = "onlineSave";
        private UserData.File onlineSaveFile;

        public static List<string> nonCampaignSlugcats = new List<string> { "Night" , "Inv" , "Slugpup" , "MeadowOnline" , "MeadowOnlineRemote" };
        public static void InitializeStorySaves()
        {
            instance = new StorySaveManager();
        }

        private StorySaveManager() {
            RainMeadow.Debug($"operationFile {Kittehface.Framework20.UserData.GetPersistentDataPath()}");
            bool useRawData = false;
            bool cloudEnabled = false;
            bool useEncryption = false;
            bool prettyPrint = true;
            bool useBinarySerialization = false;
            UserData.FileDefinition.PS4Definition ps4Definition = new UserData.FileDefinition.PS4Definition(null, null, null, null, null, null, "Media/StreamingAssets/SaveIcon.png", 1048576L, null, false);
            UserData.FileDefinition fileDefinition = new UserData.FileDefinition(fileName, useRawData, cloudEnabled, useEncryption, prettyPrint, useBinarySerialization, null, new UserData.FileDefinition.SwitchDefinition("rainworld", 1048576L), ps4Definition, null, null);
            UserData.OnFileMounted += this.UserData_OnFileMounted;
            UserData.Mount(Profiles.ActiveProfiles[0], null, fileDefinition);
        }

        private void UserData_OnFileMounted(UserData.File file, UserData.Result result)
        {
            if (file.filename == fileName) 
            {
                UserData.OnFileMounted -= this.UserData_OnFileMounted;
                if (result.IsSuccess()) 
                {
                    onlineSaveFile = file;
                    onlineSaveFile.OnReadCompleted += this.SaveFile_OnReadCompleted;
                    onlineSaveFile.Read();
                }
            }
        }

        private void SaveFile_OnReadCompleted(UserData.File file, UserData.Result result)
        {
            file.OnReadCompleted -= this.SaveFile_OnReadCompleted;
            if (result.IsSuccess())
            {

                if (result.Contains(UserData.Result.FileNotFound))
                {
                    SanitizeSaveFile();
                    return;
                }

                string text = this.onlineSaveFile.Get(RWCustom.Custom.rainWorld.options.saveSlot.ToString());
                if (!string.IsNullOrWhiteSpace(text)) 
                {
                    List<string> rawSaveInfos = text.Split(',').ToList();
                    foreach (var info in rawSaveInfos) 
                    {
                        if (!string.IsNullOrWhiteSpace(info)) 
                        {
                            var temp = info.Split('|').ToList();
                            if (StorySaveProfile.tryParse(temp[0], temp[1], out var saveProfile))
                            {
                                nonCampaignSlugcats.Add(saveProfile.save.value);
                                storyModeSaves.Add(saveProfile);
                            }
                        }
                    }
                }
                SanitizeSaveFile();
            }
            else if (result.Contains(UserData.Result.FileNotFound)) {
                SanitizeSaveFile();
                return;
            }
        }

        private void OnlineSaveFile_OnWriteCompleted(UserData.File file, UserData.Result result)
        {
            file.OnWriteCompleted -= this.OnlineSaveFile_OnWriteCompleted;
            if (result.IsSuccess())
            {
                RainMeadow.Debug("Wrote/created online save file");
            }
            else 
            {
                RainMeadow.Debug("Failed to write/create online save file");
            }
        }

        public static void generateStorySave(SlugcatStats.Name campaign, string saveName)
        {
            var allSlugcatNames = ExtEnum<SlugcatStats.Name>.values;
            if (campaign == null)
            {
                throw new ArgumentNullException("Null campaign, bad dev");
            }
            if (string.IsNullOrWhiteSpace(saveName)) 
            {
                throw new ArgumentException("Invalid save name, input is empty");
            }
            if (allSlugcatNames.entries.Contains(saveName)) 
            {
                throw new ArgumentException($"savename {saveName} already exists");
            }
            var saveProfile = new StorySaveProfile(campaign, saveName);
            if (saveProfile != null) 
            {
                instance.storyModeSaves.Add(saveProfile);
                nonCampaignSlugcats.Add(saveProfile.save.value);
            }
        }

        public static void saveToDisk() 
        {
            var text = instance.storyModeSaves.Aggregate("",(a, b) => a +","+ b.ToString());
            RainMeadow.Debug(text);
            instance.onlineSaveFile.Set(RWCustom.Custom.rainWorld.options.saveSlot.ToString(), text);
            instance.onlineSaveFile.OnWriteCompleted += instance.OnlineSaveFile_OnWriteCompleted;
        }

        public static void deleteStorySave(string campaign, string saveName)
        {
            var saveToRemove = instance.storyModeSaves.FirstOrDefault(x => x.campaignName == campaign && x.displayName == saveName);
            if (saveToRemove != null)
            {
                instance.storyModeSaves.Remove(saveToRemove);
                saveToDisk();
                RainMeadow.Debug($"Deleted save: {saveName}");
            }
            else 
            {
                RainMeadow.Debug($"could not find save: {saveName}");
            }
        }

        public static List<StorySaveProfile> getCampaignSaves(SlugcatStats.Name campaign) 
        {
            return instance.storyModeSaves.Where(x => x.campaignName == campaign.value).ToList();
        }

        public static StorySaveProfile GetStorySaveProfile(SlugcatStats.Name campaign, string saveName) 
        {
            return instance.storyModeSaves.FirstOrDefault(x => x.campaignName == campaign.value && x.displayName == saveName);
        }

        public static bool TryGetStorySaveProfile(SlugcatStats.Name campaign, string saveName, out StorySaveProfile saveslot) 
        {
            saveslot = instance.storyModeSaves.FirstOrDefault(x => x.campaignName == campaign.value && x.displayName == saveName);

            return saveslot != null;
        }

        public static List<ListItem> getListItems(SlugcatStats.Name campaign) 
        {
            var listitems = new List<ListItem>();
            var profiles = getCampaignSaves(campaign);
            foreach (var profile in profiles) 
            { 
                ListItem item = new ListItem(profile.displayName);
                listitems.Add(item);
            }
            return listitems;
        }

        //TODO: need to have a UI element to allow players to create saves. For now we do this dumb thing
        private static void SanitizeSaveFile() 
        {
            var filteredList = new List<SlugcatStats.Name>();
            for (int i = 0; i < SlugcatStats.Name.values.entries.Count; i++)
            {
                var campaignName = SlugcatStats.Name.values.entries[i];
                if (StorySaveManager.nonCampaignSlugcats.Contains(campaignName))
                {
                    continue;
                }
                if (!instance.storyModeSaves.Any(x => x.campaignName == campaignName)) 
                {
                    if (ExtEnumBase.TryParse(typeof(SlugcatStats.Name), campaignName, false, out var enumBase))
                    {
                        var temp = (SlugcatStats.Name)enumBase;
                        for (int j = 1; j <= 3; j++) 
                        {
                            generateStorySave(temp, $"save{j}");
                        }
                    }
                }
            }
            saveToDisk();
        } 
    }
}
