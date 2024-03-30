using System;
using System.Collections.Generic;
using System.Linq;
using Kittehface.Framework20;

namespace RainMeadow
{
    public class StorySaveManager
    {
        public static StorySaveManager instance { get; private set; }
        public List<StorySaveInfo> storyModeSaves { get; private set; }
        private static string fileName = "onlineSave";
        private UserData.File onlineSaveFile;

        public static List<string> nonCampaignSlugcats = new List<string> { "Night" , "Inv" , "Slugpup" , "MeadowOnline" , "MeadowOnlineRemote" };
        public static void InitializeStorySaves()
        {
            instance = new StorySaveManager();
            instance.storyModeSaves = new List<StorySaveInfo>();
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
                    onlineSaveFile.OnWriteCompleted += OnlineSaveFile_OnWriteCompleted;
                    onlineSaveFile.Write();
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
                            if (StorySaveInfo.tryParse(temp[0], temp[1], out var storyInfo))
                            {
                                nonCampaignSlugcats.Add(storyInfo.save.value);
                                storyModeSaves.Add(storyInfo);
                            }
                        }
                    }
                }
            }
            else if (result.Contains(UserData.Result.FileNotFound)) {
                onlineSaveFile.OnWriteCompleted += OnlineSaveFile_OnWriteCompleted;
                onlineSaveFile.Write();
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
            var saveInfo = new StorySaveInfo(campaign, saveName);
            if (saveInfo != null) 
            {
                instance.storyModeSaves.Add(saveInfo);
            }
        }

        public static void saveToDisk() 
        {
            var text = instance.storyModeSaves.Aggregate("",(a, b) => a +","+ b.ToString());
            RainMeadow.Debug(text);
            instance.onlineSaveFile.Set(RWCustom.Custom.rainWorld.options.saveSlot.ToString(), text);
            instance.onlineSaveFile.OnWriteCompleted += instance.OnlineSaveFile_OnWriteCompleted;
        }

        public static void deleteStorySave(string saveName)
        {
            var saveToRemove = instance.storyModeSaves.FirstOrDefault(x => x.save.value == saveName);
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

        public static List<StorySaveInfo> getCampaignSaves(SlugcatStats.Name campaign) 
        {
            return instance.storyModeSaves.Where(x => x.campaignName == campaign.value).ToList();
        }
    }
}
