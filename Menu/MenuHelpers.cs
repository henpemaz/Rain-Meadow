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
            int num = 0, textLength = text.Length;
            while (num < textLength)
            {
                string currentString = text.Substring(num);
                string[] words = currentString.Split(' ');
                string trimmedTxt = string.Empty;
                foreach (string word in words)
                {
                    string temp = trimmedTxt;
                    if (!string.IsNullOrEmpty(temp)) temp += " ";
                    temp += word;
                    if (s(temp)) //see if adding a word will not overflow, if does not continue to the next word
                    {
                        trimmedTxt = temp;
                        continue;
                    }
                    if (!string.IsNullOrEmpty(trimmedTxt) && s(trimmedTxt + " ")) //add space is there is avaliable space for it
                    {
                        trimmedTxt += " ";
                        break;
                    }
                    trimmedTxt = LabelTest.TrimText(temp, width, bigText: bigText); //technically for long first word, omg pls no mitosis of characters PLSSSSS
                    break; //skip over words that are supposed to be in next line
                }
                recieveString(trimmedTxt);
                num += trimmedTxt.Length;
            }

        }
        public static string[] SmartSplitIntoStrings(this string text, float wrapWidth, bool bigText = false)
        {
            List<string> strings = [];
            GetStringSplit(text, wrapWidth, strings.Add, bigText);
            return [.. strings];
        }
    }
}
