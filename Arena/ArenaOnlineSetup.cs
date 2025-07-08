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
        public static string SetIntoSaveString(string[] array)
        {
            string saveString = "";
            for (int i = 0; i < array.Length; i++)
                saveString += array[i] + (i < array.Length - 1 ? "<msuB>" : "<msuA>");
            return saveString;
        }
        public ArenaOnlineSetup(ProcessManager manager) : base(manager)
        {
        }
        public string GetSessionSaveString()
        {
            string saveString = "";
            ArenaHelpers.ParseArenaSetupSaveString(manager.rainWorld.options.LoadArenaSetup(savFilePath), (array) =>
            {
                if (array.Length > 0 && sessionSaveIdentifiableStrings.Contains(array[0]))
                    saveString += SetIntoSaveString(array);
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
            ArenaHelpers.ParseArenaSetupSaveString(origText, (array) =>
            {
                if (array.Length > 0 && !sessionSaveIdentifiableStrings.Contains(array[0]))
                    newSaveString += SetIntoSaveString(array);
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
