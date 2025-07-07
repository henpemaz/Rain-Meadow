﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public static class MenuHelpers
    {
        public static string LongTranslate(this Menu.Menu menu, string s) => Custom.ReplaceLineDelimeters(menu.Translate(s));
        public static void SafeAddSubobjects(this MenuObject container, params MenuObject?[] subObjectsToAdd)
        {
            if (container == null || subObjectsToAdd == null)
            {
                return;
            }
            container.subObjects.AddRange(subObjectsToAdd.Where(x => x != null && !container.subObjects.Contains(x)));
        }
        public static bool IsAllRemixUINotHeld(this MenuTabWrapper tabWrapper) => !tabWrapper.wrappers.All(x => x.Key is UIconfig config && config.held);
        public static float GetMaxWidthInText(this string text, bool bigText)
        {
            FLetterQuad[] quads = [..Futile.atlasManager.GetFontWithName(LabelTest.GetFont(bigText)).GetQuadInfoForText(text, new())?.SelectMany(x => x.quads)];
            if (quads?.Length > 0)
            {
                return quads.Max(x => x.rect.width);
            }
            return LabelTest.CharMean(bigText);
        }

        /// <returns>an array of strings that were split based on string input over a width based on rw text</returns>
        public static string[] SplitIntoStrings(this string text, float width, bool bigText = false) //not using wrapText since method checks if the language is wrappable
        {
            width = Mathf.Max(text.GetMaxWidthInText(bigText), width);
            List<string> strings = [];
            if (text != null)
            {
                int num = 0;
                while (num < text.Length)
                {
                    string trimmedTxt = LabelTest.TrimText(text.Substring(num), width, bigText: bigText);
                    strings.Add(trimmedTxt);
                    num += trimmedTxt.Length;
                }
            }
            return [.. strings];

        }
        public static void GetStringSplit(string text, float width, Action<string> recieveString, bool bigText = false)
        {
            //made another copy because for some reason the third line in arena lobby will ignore width
            Predicate<string> s = t => LabelTest.GetWidth(t, bigText) < width;
            string[] words = text.Split(' ');
            string trimmedTxt = string.Empty;
            foreach (string word in words)
            {
                string temp = trimmedTxt;
                temp += $"{(!string.IsNullOrEmpty(temp) ? " " : "")}{word}";
                if (s(temp)) //skip over to next word if it doesnt overflow
                {
                    trimmedTxt = temp;
                    continue;
                }
                if (!string.IsNullOrEmpty(trimmedTxt) && s(trimmedTxt + " "))
                {
                    recieveString(trimmedTxt + " "); //if space doesnt overflow then recieve string and set text back to empty 
                    trimmedTxt = word; //dont skip word before going to the next word
                    continue;
                }
                foreach (string line in temp.SplitIntoStrings(width, bigText))
                    recieveString(line); //for extremly long words etc whole text`HAHAHAHHAHAHAHAHAHAHHA`
                trimmedTxt = string.Empty; //reset
            }
            if (trimmedTxt.Length > 0) recieveString(trimmedTxt); //add text if it isnt added, happens if there is no overflow
        }
        public static ValueTuple<string, string>[] GetStringSplit(string text, float width, Action<string> recieveString, int maxLineCount, bool bigText = false)
        {
            maxLineCount = Mathf.Max(maxLineCount, 1);
            int amtOfLinesAdded = 0;
            Predicate<string> s = t => LabelTest.GetWidth(t, bigText) < width;
            Func<bool> hasOverflowed = () => amtOfLinesAdded >= maxLineCount;
            Action<string> tryRecieveString = _ =>
            {
                recieveString(_);
                amtOfLinesAdded++;
            };
            string[] words = text.Split(' ');
            List<ValueTuple<string, string>> remainingWords = [];
            string trimmedTxt = string.Empty;
            foreach (string word in words)
            {
                if (hasOverflowed())
                {
                    remainingWords.Add(new(word, " "));
                    continue;
                }
                string temp = trimmedTxt;
                temp += $"{(!string.IsNullOrEmpty(temp)? " " : "")}{word}";
                if (s(temp)) //skip over to next word if it doesnt overflow
                {
                    trimmedTxt = temp;
                    continue;
                }
                if (!string.IsNullOrEmpty(trimmedTxt) && s(trimmedTxt + " "))
                {
                    tryRecieveString(trimmedTxt + " "); //if space doesnt overflow then recieve string and set text back to empty 
                    trimmedTxt = word; //dont skip word before going to the next word
                    if (hasOverflowed()) //if the new line after space caused a new overflow
                    {
                        remainingWords.Add((word, ""));
                        trimmedTxt = string.Empty;
                    }
                    continue;
                }
                foreach (string line in temp.SplitIntoStrings(width, bigText))
                {
                    if (hasOverflowed())
                    {
                        remainingWords.Add((line, ""));
                        continue;
                    }
                    tryRecieveString(line); //for extremly long words etc whole text`HAHAHAHHAHAHAHAHAHAHHA`
                }
                trimmedTxt = string.Empty; //reset
            }
            if (trimmedTxt.Length > 0) recieveString(trimmedTxt); //add text if it isnt added, happens if there is no overflow
            return [..remainingWords];
        }

        /// <returns>an array of strings that were split based on string input over a width based on rw text</returns>
        public static string[] SmartSplitIntoStrings(this string text, float wrapWidth, bool bigText = false)
        {
            List<string> strings = [];
            GetStringSplit(text, wrapWidth, strings.Add, bigText);
            return [.. strings];
        }
        /// <summary>
        /// maxLineCount has to be above 0! it has to
        /// </summary>
        /// <returns>an array of strings that were split based on string input over a width based on rw text</returns>
        public static string[] SmartSplitIntoFixedStrings(this string text, float wrapWidth, int maxLineCount, out string remainingString, bool bigText = false)
        {
            List<string> strings = [];
            ValueTuple<string, string>[] remainingWords = GetStringSplit(text, wrapWidth, strings.Add, maxLineCount, bigText);
            remainingString = string.Join("", remainingWords.Select(x => x.Item2).Zip(remainingWords.Select(x => x.Item1), (connector, word) => $"{connector}{word}"));
            return [.. strings];
        }
    }
}
