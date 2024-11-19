using Menu;
using UnityEngine;

namespace RainMeadow
{
    public partial class SlugcatCustomSelection : SlugcatSelectMenu.SlugcatPage
    {
        public StoryMenu storyCustomMenu;

        public SlugcatCustomSelection(StoryMenu storyCustomMenu, SlugcatSelectMenu unusedMenu, int pageIndex, SlugcatStats.Name slug) : base(unusedMenu, null, pageIndex, slug)
        {
            this.storyCustomMenu = storyCustomMenu;
            base.AddImage(false);
            this.slugcatImage.menu = storyCustomMenu;
            this.slugcatNumber = slug;
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
        }
    }
}
