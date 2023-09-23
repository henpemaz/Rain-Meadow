using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCharacterSelectPage : SlugcatSelectMenu.SlugcatPage
    {
        public MeadowMenu realMenu;
        public MeadowProgression.Character character;
        public MenuLabel mainLabel;
        public MenuLabel infoLabel;
        public bool isNew;
        public float flashSin;

        public MeadowCharacterSelectPage(MeadowMenu realMenu, SlugcatSelectMenu fakeMenu, int pageIndex, MeadowProgression.Character character) : base(fakeMenu, null, pageIndex, MeadowProgression.Character.characterStats[character])
        {
            this.realMenu = realMenu;
            this.character = character;
            string main = GetSaveLocation();
            string info;
            isNew = string.IsNullOrEmpty(main);
            if (isNew)
            {
                main = GetCharacterName();
                info = realMenu.Translate("New character!");
            }
            else
            {
                info = GetPlaytime();
            }
            base.AddImage(false);
            this.slugcatImage.menu = realMenu;

            this.mainLabel = new MenuLabel(realMenu, this, main, new Vector2(-1000f, this.imagePos.y - 268f), new Vector2(200f, 30f), true, null);
            this.mainLabel.label.alignment = FLabelAlignment.Center;
            this.subObjects.Add(this.mainLabel);

            this.infoLabel = new MenuLabel(realMenu, this, info, new Vector2(-1000f, this.imagePos.y - 268f - 30f), new Vector2(200f, 30f), true, null);
            this.infoLabel.label.alignment = FLabelAlignment.Center;
            this.subObjects.Add(this.infoLabel);

            this.mainLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            this.infoLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
        }

        private string GetSaveLocation()
        {
            // todo
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowSlugcat)
            {
                return "Five Pebbles";
            }
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowSquidcicada)
            {
                return "Outskirts";
            }
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowLizard)
            {
                return "";
            }
            RainMeadow.Error("no status string for " + this.slugcatNumber);
            return "";
        }

        private string GetPlaytime()
        {
            // todo
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowSlugcat)
            {
                return "2h37";
            }
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowSquidcicada)
            {
                return "16m";
            }
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowLizard)
            {
                return "";
            }
            RainMeadow.Error("no status string for " + this.slugcatNumber);
            return "";
        }

        public string GetCharacterName()
        {
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowSlugcat)
            {
                return "SLUGCAT";
            }
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowSquidcicada)
            {
                return "SQUIDCADA";
            }
            if (slugcatNumber == RainMeadow.Ext_SlugcatStatsName.MeadowLizard)
            {
                return "LIZARD";
            }
            RainMeadow.Error("no name string for " + this.slugcatNumber);
            return "";
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            flashSin = (flashSin + timeStacker / 6f) % (2 * Mathf.PI);

            float scroll = base.Scroll(timeStacker);
            float alpha = base.UseAlpha(timeStacker);
            this.mainLabel.label.alpha = alpha;
            this.mainLabel.label.x = base.MidXpos + scroll * base.ScrollMagnitude + 0.01f;
            this.infoLabel.label.alpha = alpha;
            this.infoLabel.label.x = base.MidXpos + scroll * base.ScrollMagnitude + 0.01f;
            if (isNew)
            {
                this.infoLabel.label.color = HSLColor.Lerp(Menu.Menu.MenuColor(Menu.Menu.MenuColors.VeryDarkGrey), Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey), 0.5f + 0.5f * Mathf.Sin(flashSin)).rgb;
            }
        }
    }
}
