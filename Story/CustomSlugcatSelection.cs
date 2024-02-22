using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class SlugcatCustomSelection : SlugcatSelectMenu.SlugcatPage
    {
        public StoryMenu storyCustomMenu;
        public SlugcatStats.Name slug;
        public MenuLabel mainLabel;
        public MenuLabel infoLabel;
        private string main;
        private string info;
        public float flashSin;
        public (SlugcatStats.Name Name, string DisplayName, string Description)[] slugcatDesc;

        public SlugcatCustomSelection(StoryMenu storyCustomMenu, SlugcatSelectMenu unusedMenu, int pageIndex, SlugcatStats.Name slug) : base(unusedMenu, null, pageIndex, RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer)
        {
            this.storyCustomMenu = storyCustomMenu;
            this.slug = slug;
            base.AddImage(false);
            this.slugcatImage.menu = storyCustomMenu;

            main = "pain";
            info = GetDescription();



            this.mainLabel = new MenuLabel(storyCustomMenu, this, main, new Vector2(-1000f, this.imagePos.y - 268f), new Vector2(200f, 30f), true, null);
            this.mainLabel.label.alignment = FLabelAlignment.Center;
            this.subObjects.Add(this.mainLabel);

            this.infoLabel = new MenuLabel(storyCustomMenu, this, info, new Vector2(-1000f, this.imagePos.y - 268f - 30f), new Vector2(200f, 30f), true, null);
            this.infoLabel.label.alignment = FLabelAlignment.Center;
            this.subObjects.Add(this.infoLabel);

            this.mainLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            this.infoLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);



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

        }

        public string GetDescription()
        {
            if (slug == SlugcatStats.Name.White)
            {
                info = "pain2";
                return info;

            }
            if (slug == SlugcatStats.Name.Red)
            {
                info = "pain4";
                return info;
            }
            return "";
        }
    }
}
