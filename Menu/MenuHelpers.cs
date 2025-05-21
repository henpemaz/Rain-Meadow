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
    }
}
