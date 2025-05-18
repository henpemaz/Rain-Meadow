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
        public ArenaOnlineSetup(ProcessManager manager) : base(manager)
        {
        }
        public string GetSessionSaveString()
        {
            string saveString = "";
            ArenaHelpers.ParseArenaSetupSaveString(manager.rainWorld.options.LoadArenaSetup(savFilePath), (name, value) =>
            {
                if (name != null && sessionSaveIdentifiableStrings.Contains(name))
                {
                    saveString += SetIntoSaveString(name, value);
                }
            });
            return saveString;
        }
        public string SetSaveStringFilter(string origText)
        {
            if (!isSavingNonSessionOnly)
            {
                RainMeadow.Debug("Saving everything!");
                return origText;
            }
            string newSaveString = "";
            string SessionSaveString = GetSessionSaveString();
            ArenaHelpers.ParseArenaSetupSaveString(origText, (name, value) =>
            {
                if (name != null && !sessionSaveIdentifiableStrings.Contains(name))
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
            RainMeadow.DebugMe();
            isSavingNonSessionOnly = true;
            SaveToFile();
            isSavingNonSessionOnly = false;
        }
        public bool isSavingNonSessionOnly = false;
        public List<string> sessionSaveIdentifiableStrings = ["CURRGAMETYPE", "GAMETYPE"];
    }
}
