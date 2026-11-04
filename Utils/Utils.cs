using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Menu;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    internal static class Utils
    {
        [Obsolete("Use the provided Translator in the class (this.Translate()), pass in a translator to a dependent function, or use the ProcessManager's translator (processManager.rainWorld.inGameTranslator).")]
        public static InGameTranslator Translator => Custom.rainWorld.inGameTranslator;

        [Obsolete("Use the provided Translator in the class (this.Translate()), pass in a translator to a dependent function, or use the ProcessManager's translator (processManager.rainWorld.inGameTranslator).")]
        public static string Translate(string text)
        {
            return Translator.Translate(text);
        }

        public static string GetMeadowTitleFileName(bool isShadow)
        {
            var fileName = isShadow ? "shadow" : "title";

            var translatedfileName = fileName + "_" + Translator.currentLanguage.value.ToLower();

            // Fallback to English
            if (!File.Exists(AssetManager.ResolveFilePath($"illustrations/rainmeadowtitle/{translatedfileName}.png")))
            {
                return fileName + "_english";
            }

            return translatedfileName;
        }

        public static string GetTranslatedLobbyName(string username)
        {
            var lobbyName = Translator.Translate("<USERNAME>'s Lobby");

            return lobbyName.Replace("<USERNAME>", username);
        }


        public static void Restart(string args = "")
        {
            Process currentProcess = Process.GetCurrentProcess();
            string text = "\"" + currentProcess.MainModule.FileName + "\"";
            IDictionary environmentVariables = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);
            List<string> list = new List<string>();
            foreach (object obj in environmentVariables)
            {
                DictionaryEntry dictionaryEntry = (DictionaryEntry)obj;
                if (dictionaryEntry.Key.ToString().StartsWith("DOORSTOP"))
                {
                    list.Add(dictionaryEntry.Key.ToString());
                }
            }
            foreach (string text2 in list)
            {
                environmentVariables.Remove(text2);
            }
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.EnvironmentVariables.Clear();
            foreach (object obj2 in environmentVariables)
            {
                DictionaryEntry dictionaryEntry2 = (DictionaryEntry)obj2;
                processStartInfo.EnvironmentVariables.Add((string)dictionaryEntry2.Key, (string)dictionaryEntry2.Value);
            }
            processStartInfo.UseShellExecute = false;
            processStartInfo.FileName = text;
            processStartInfo.Arguments = string.IsNullOrEmpty(args)
                ? $"{WaitForPidArg} {currentProcess.Id}"
                : $"{args} {WaitForPidArg} {currentProcess.Id}";
            Process.Start(processStartInfo);
            Application.Quit();
        }

        public const string WaitForPidArg = "+meadow_wait_pid";
        private const int WaitForPreviousInstanceTimeoutMs = 10000;

        // creates some issues when trying to rejoin while closing previous game instance, so wait for the PID to close fully
        public static void WaitForPreviousInstance()
        {
            try
            {
                string[] args = Environment.GetCommandLineArgs();
                int idx = Array.IndexOf(args, WaitForPidArg);
                if (idx < 0 || args.Length <= idx + 1 || !int.TryParse(args[idx + 1], out int pid))
                    return;

                RainMeadow.Info($"Waiting for the previous game instance ({pid}) to exit");
                Stopwatch stopwatch = Stopwatch.StartNew();
                bool exited = false;

                while (!exited && stopwatch.ElapsedMilliseconds < WaitForPreviousInstanceTimeoutMs)
                {
                    try
                    {
                        using Process previous = Process.GetProcessById(pid);
                        exited = previous.HasExited;
                    }
                    catch (ArgumentException)
                    {
                        exited = true;
                    }

                    if (!exited)
                        System.Threading.Thread.Sleep(50);
                }

                if (exited)
                    RainMeadow.Info($"Previous game instance exited after {stopwatch.ElapsedMilliseconds}ms");
                else
                    RainMeadow.Error($"Previous game instance ({pid}) did not exit within {WaitForPreviousInstanceTimeoutMs}ms, continuing anyway");
            }
            catch (Exception e)
            {
                RainMeadow.Error(e);
            }
        }

        /// <summary>
        /// Adds a range of items to a list, excluding items which are already in the list.
        /// </summary>
        /// <param name="self">The list to add to.</param>
        /// <param name="items">The range of items to add.</param>
        public static void AddDistinctRange<T>(this IList<T> self, IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                if (self.Contains(item))
                {
                    continue;
                }

                self.Add(item);
            }
        }

        /// <summary>
        /// Trims an occurence of a string from the start of another.
        /// </summary>
        /// <param name="target">String to be trimmed.</param>
        /// <param name="trimString">String to trim from the target.</param>
        /// <returns>The trimmed string.</returns>
        public static string TrimStart(this string target, string trimString)
        {
            if (string.IsNullOrEmpty(trimString))
            {
                return target;
            }

            var result = target;

            while (result.StartsWith(trimString))
            {
                result = result.Substring(trimString.Length);
            }

            return result;
        }

        /// <summary>
        /// Take elements from an enumerable from startIndex to (endIndex - 1) inclusive.
        /// </summary>
        /// <param name="enumerable">The enumerable to take from.</param>
        /// <param name="startIndex">The start index (inclusive).</param>
        /// <param name="endIndex">The end index (exclusive).</param>
        /// <returns>The taken elements.</returns>
        public static List<T> TakeFromTo<T>(this IEnumerable<T> enumerable, int startIndex, int endIndex)
        {
            var fromStart = enumerable.Skip(startIndex).ToList();

            var toTake = endIndex - startIndex;

            return fromStart.Take(toTake).ToList();
        }

        public static Color SafeHexToColor(string hex)
        {
            try
            {
                if (hex != "000000")
                    return Custom.hexToColor(hex);
            }
            catch (Exception) { }
            return new Color(0.01f, 0.01f, 0.01f, 1f);
        }

        public static int RealPing(int ping) => Math.Max(1, ping - 16);
        public static Color RealPingColor(int realping) => Color.Lerp((realping > 200 ? Color.red : realping > 100 ? Color.yellow : Color.green), MenuColorEffect.rgbVeryDarkGrey, 0.65f);
    }
}
