using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace RainMeadow
{
    public static class MenuHelpers
    {
        public static void SafeAddSubobjects(this MenuObject container, params MenuObject?[] subObjectsToAdd)
        {
            if (container == null || subObjectsToAdd == null)
            {
                return;
            }
            container.subObjects.AddRange(subObjectsToAdd.Where(x => x != null && !container.subObjects.Contains(x)));
        }
        public static bool IsAllRemixUINotHeld(this MenuObject owner) => owner.subObjects.OfType<UIelementWrapper>().All(x => !(x.thisElement is UIconfig config && config.held));
        public static string[] SplitIntoStrings(string text, float width, bool bigText = false) //not using wrapText since method checks if the language is wrappable
        {
            width = Mathf.Max(LabelTest.CharMean(bigText), width);
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

        /// <summary>
        /// This delimits the text on word boundaries into multiple lines to fit some metric of width based on rw text width. Tries to smartly split string
        /// </summary>
        public static void GetStringSplit(string text, float width, Action<string> recieveString, bool bigText = false)
        {
            Predicate<string> s = t => LabelTest.GetWidth(t, bigText) < width;

            string[] words = text.Split(' ');
            string trimmedTxt = string.Empty;
            foreach (string word in words)
            {
                string temp = trimmedTxt;
                if (!string.IsNullOrEmpty(temp)) temp += " ";
                temp += word;
                if (s(temp)) //skip over to next word if it doesnt overflow
                {
                    trimmedTxt = temp;
                    continue;
                }
                if (!string.IsNullOrEmpty(trimmedTxt) && s(trimmedTxt + " ")) trimmedTxt += " "; //if space doesnt overflow then recieve string and set text back to empty
                else trimmedTxt = LabelTest.TrimText(temp, width, false, bigText); 
                recieveString(trimmedTxt);
                trimmedTxt = string.Empty;
                continue;
            }
            if (trimmedTxt.Length > 0) recieveString(trimmedTxt); //add text if it isnt added, happens if there is no overflow
        }
        public static string[] SmartSplitIntoStrings(this string text, float wrapWidth, bool bigText = false)
        {
            List<string> strings = [];
            GetStringSplit(text, wrapWidth, strings.Add, bigText);
            return [.. strings];
        }
    }
}
