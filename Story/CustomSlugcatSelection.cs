using Menu;
using UnityEngine;

namespace RainMeadow
{
    public partial class SlugcatCustomSelection : SlugcatSelectMenu.SlugcatPage
    {
        public StoryMenu storyCustomMenu;
        public SlugcatStats.Name slug;
        public MenuLabel mainLabel;
        public MenuLabel infoLabel;
        public string text;
        public string subText;
        public float flashSin;

        public SlugcatCustomSelection(StoryMenu storyCustomMenu, SlugcatSelectMenu unusedMenu, int pageIndex, SlugcatStats.Name slug) : base(unusedMenu, null, pageIndex, RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer)
        {
            this.storyCustomMenu = storyCustomMenu;
            this.slug = slug;
            base.AddImage(false);
            this.slugcatImage.menu = storyCustomMenu;
            this.slugcatNumber = slug;

        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            flashSin = (flashSin + timeStacker / 6f) % (2 * Mathf.PI);

            float scroll = base.Scroll(timeStacker);
            float alpha = base.UseAlpha(timeStacker);

        }

    }

}
