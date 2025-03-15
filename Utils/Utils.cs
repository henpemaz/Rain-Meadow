using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.Linq;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    internal static class Utils
    {
        public static InGameTranslator Translator => Custom.rainWorld.inGameTranslator;

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
            processStartInfo.Arguments = args;
            Process.Start(processStartInfo);
            Application.Quit();
        }

        /// <summary>
        /// Adds a range of items to a list, excluding items which are already in the list.
        /// </summary>
        /// <param name="self">The list to add to.</param>
        /// <param name="items">The range of items to add.</param>
        public static void AddDistinctRange<T>(this IList<T> self, IEnumerable<T> items)
        {
            foreach(var item in items)
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

        /// <summary>
        /// Detects whether the particular file exists in the mod
        /// in such a way that AssetManager.ResolveFilePath() can find it.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static bool ModFileExists(ModManager.Mod mod, params string[] paths)
        {
            return File.Exists(Path.Combine([mod.path, .. paths]))
                || (mod.hasNewestFolder && File.Exists(Path.Combine([mod.NewestPath, .. paths])))
                || (mod.hasTargetedVersionFolder && File.Exists(Path.Combine([mod.TargetedPath, .. paths])));
        }

        /// <summary>
        /// Detects whether the particular directory exists in the mod
        /// in such a way that AssetManager.ResolveDirectory() can find it.
        /// </summary>
        /// <param name="mod"></param>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static bool ModDirectoryExists(ModManager.Mod mod, params string[] paths)
        {
            return Directory.Exists(Path.Combine([mod.path, .. paths]))
                || (mod.hasNewestFolder && Directory.Exists(Path.Combine([mod.NewestPath, .. paths])))
                || (mod.hasTargetedVersionFolder && Directory.Exists(Path.Combine([mod.TargetedPath, .. paths])));
        }

        /// <summary>
        /// Gets a list of files within the directory IF it exists.
        /// If the directory does not exist, returns an empty list.
        /// </summary>
        /// <param name="path">The directory path</param>
        /// <returns></returns>
        public static IEnumerable<string> GetDirectoryFilesSafe(string path)
        {
            if (!Directory.Exists(path)) return [];
            return Directory.EnumerateFiles(path);
        }
    }
}
