using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RainMeadow
{
    public class ArenaOnlineSetup : ArenaSetup
    {
        public static string SetIntoSaveString(string name, string value)
        {
            return name + "<msuB>" + value + "<msuA>";
        }
        public static List<string> SessionSaveIdentifiableStrings =>
        [
            "CURRGAMETYPE",
            "GAMETYPE",
        ];
        public bool isSavingNonSessionOnly = false;
        public ArenaOnlineSetup(ProcessManager manager) : base(manager)
        {
        }
        public string GetSessionSaveString()
        {
            string saveString = "";
            ArenaHelpers.ParseArenaSetupSaveString(manager.rainWorld.options.LoadArenaSetup(savFilePath), (name, value) =>
            {
                if (name != null && SessionSaveIdentifiableStrings.Contains(name))
                {
                    saveString += SetIntoSaveString(name, value);
                }
            });
            return saveString;
        }
        public string SetSaveStringFilter(string origText)
        {
            if (!saveNonSessionOnly) return origText;
            string newSaveString = "";
            string SessionSaveString = GetSessionSaveString();
            ArenaHelpers.ParseArenaSetupSaveString(origText, (name, value) =>
            {
                if (name != null && !SessionSaveIdentifiableStrings.Contains(name))
                {
                    RainMeadow.Debug($"Saving: {name}");
                    newSaveString += SetIntoSaveString(name, value);
                }
            });
            newSaveString += SessionSaveString;
            return newSaveString;
        }
        public void SaveNonSessionToFile()
        {
            isSavingNonSessionOnly = true;
            SaveToFile();
            isSavingNonSessionOnly = false;
        }
        public bool saveNonSessionOnly = false;
    }
}
