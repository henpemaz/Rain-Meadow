using BepInEx.Logging;
using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace RainMeadow
{
    public class InvalidProgrammerException : InvalidOperationException
    {
        public InvalidProgrammerException(string message) : base(message + " you goof") { }
    }

    partial class RainMeadow
    {
        public static string TrimCaller(string callerFile) { return callerFile.Substring(Mathf.Max(callerFile.LastIndexOf('\\'), callerFile.LastIndexOf('/')) + 1).Substring(0, callerFile.LastIndexOf('.')); }
        public static void Debug(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            instance.Logger.LogInfo($"{TrimCaller(callerFile)}.{callerName}:{data}");
        }
        public static void DebugMe([CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            instance.Logger.LogInfo($"{TrimCaller(callerFile)}.{callerName}");
        }
        internal static void Error(object data, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerName = "")
        {
            instance.Logger.LogError($"{TrimCaller(callerFile)}.{callerName}:{data}");
        }
    }
}
