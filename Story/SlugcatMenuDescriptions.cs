using System;
using Menu;


namespace RainMeadow
{
    public partial class SlugcatCustomSelection : SlugcatSelectMenu.SlugcatPage

    {     
        private ValueTuple<string, string> GetDescription()
        {
            if (OnlineManager.lobby.isOwner)
            {
                if (slugcatNumber == SlugcatStats.Name.White)
                {
                    text = menu.Translate("THE SURVIVOR");
                    subText = menu.Translate("A nimble omnivore, both predator and prey. Lost in a harsh and indifferent<LINE>land you must make your own way, with wit and caution as your greatest assets.");
                }
                else if (slugcatNumber == SlugcatStats.Name.Yellow)
                {
                    text = menu.Translate("THE MONK");
                    subText = menu.Translate("Weak of body but strong of spirit. In tune with the mysteries of the world and<LINE>empathetic to its creatures, your journey will be a significantly more peaceful one.");

                }
                else if (slugcatNumber == SlugcatStats.Name.Red)
                {
                    text = menu.Translate("THE HUNTER");
                    subText = menu.Translate("Strong and quick, with a fierce metabolism requiring a steady diet of meat. But the<LINE>stomach wont be your only concern, as the path of the hunter is one of extreme peril.");

                }
                subText = RWCustom.Custom.ReplaceLineDelimeters(subText);
            }
             else
            {
                text = "Host is choosing campaign...";
                subText = $"Please wait until host is ready";
            }
            return (text, subText);

        }
    }
}