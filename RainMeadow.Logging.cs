using BepInEx.Logging;
using System.Runtime.CompilerServices;

namespace RainMeadow
{
    partial class RainMeadow
    {
        public static void Debug(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            callerFile = callerFile.Substring(callerFile.LastIndexOf('\\') + 1);
            callerFile = callerFile.Substring(0, callerFile.LastIndexOf('.'));
            instance.Logger.LogInfo($"{callerFile}.{callerName}:{data}");
        }
        public static void DebugMethod([CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            callerFile = callerFile.Substring(callerFile.LastIndexOf('\\') + 1);
            callerFile = callerFile.Substring(0, callerFile.LastIndexOf('.'));
            instance.Logger.LogInfo($"{callerFile}.{callerName}");
        }
        internal static void Error(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            callerFile = callerFile.Substring(callerFile.LastIndexOf('\\') + 1);
            callerFile = callerFile.Substring(0, callerFile.LastIndexOf('.'));
            instance.Logger.LogError($"{callerFile}.{callerName}:{data}");
        }
    }
}
