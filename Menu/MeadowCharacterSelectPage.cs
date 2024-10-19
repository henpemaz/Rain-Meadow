using Menu;
using System;
using UnityEngine;

namespace RainMeadow
{
    public class MeadowCharacterSelectPage : SlugcatSelectMenu.SlugcatPage
    {
        public MeadowMenu realMenu;
        public MeadowProgression.Character character;
        private readonly bool locked;
        public MenuLabel mainLabel;
        public MenuLabel infoLabel;
        public bool isNew;
        public float flashSin;
        private TokenMenuDisplayer unlockProgres;

        public MeadowCharacterSelectPage(MeadowMenu realMenu, SlugcatSelectMenu fakeMenu, int pageIndex, MeadowProgression.Character character, bool locked = false) : base(fakeMenu, null, pageIndex, RainMeadow.Ext_SlugcatStatsName.OnlineSessionPlayer)
        {
            this.realMenu = realMenu;
            this.character = character;
            this.locked = locked;

            string main = locked ? "Locked" : GetSaveLocation();
            string info = "";
            isNew = string.IsNullOrEmpty(main);
            if (isNew)
            {
                main = GetCharacterName();
                info = realMenu.Translate("New character!");
            }
            else if (!locked)
            {
                info = GetPlaytime();
            }
            base.AddImage(false);
            this.slugcatImage.menu = realMenu;

            this.mainLabel = new MenuLabel(realMenu, this, main, new Vector2(-1000f, this.imagePos.y - 268f), new Vector2(0f, 30f), true, null);
            this.subObjects.Add(this.mainLabel);

            this.infoLabel = new MenuLabel(realMenu, this, info, new Vector2(-1000f, this.imagePos.y - 268f - 30f), new Vector2(0f, 30f), true, null);
            this.subObjects.Add(this.infoLabel);

            this.mainLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.MediumGrey);
            this.infoLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);

            if (locked && !this.slugcatImage.flatMode)
            {
                foreach (var index in MeadowProgression.characterData[character].selectSpriteIndexes)
                {
                    this.slugcatImage.depthIllustrations[index].sprite.shader = menu.manager.rainWorld.Shaders["RM_SceneHidden"];
                }
            }

            if (locked)
            {
                this.unlockProgres = new TokenMenuDisplayer(realMenu, this, new Vector2(-1000f, this.imagePos.y - 268f - 34f), MeadowProgression.TokenGoldColor, $"{MeadowProgression.progressionData.characterUnlockProgress}/{MeadowProgression.characterProgressTreshold}");
                this.subObjects.Add(unlockProgres);
            }
        }

        private string GetSaveLocation()
        {
            if (MeadowProgression.progressionData.characterProgress.ContainsKey(character) && MeadowProgression.progressionData.characterProgress[character].saveLocation != MeadowProgression.characterData[character].startingCoords && !string.IsNullOrEmpty(MeadowProgression.progressionData.characterProgress[character].saveLocation.ResolveRoomName()))
            {
                var text = Region.GetRegionFullName(MeadowProgression.progressionData.characterProgress[character].saveLocation.ResolveRoomName().Substring(0, 2), slugcatNumber);
                if (text.Length > 0)
                {
                    text = menu.Translate(text);
                }
                return text;
            }
            return "";
        }

        private string GetPlaytime()
        {
            if (MeadowProgression.progressionData.characterProgress.ContainsKey(character))
            {
                var timeSpan = TimeSpan.FromMilliseconds(MeadowProgression.progressionData.characterProgress[character].timePlayed);
                //return string.Format("{0:D}h:{1:D2}m:{2:D2}s", timeSpan.Hours + timeSpan.Days * 24, timeSpan.Minutes, timeSpan.Seconds);
                return string.Format("{0:D1} : {1:D2}", timeSpan.Hours + timeSpan.Days * 24, timeSpan.Minutes);
            }
            return "";
        }

        public string GetCharacterName()
        {
            return MeadowProgression.characterData[character].displayName;
        }

        public override void GrafUpdate(float timeStacker) // why did they make this so hacky...
        {

            flashSin = (flashSin + timeStacker / 6f) % (2 * Mathf.PI);

            float scroll = base.Scroll(timeStacker);
            float alpha = base.UseAlpha(timeStacker);
            float centerX = base.MidXpos + scroll * base.ScrollMagnitude + 0.01f;
            this.mainLabel.label.alpha = alpha;
            this.mainLabel.pos.x = centerX;
            this.infoLabel.label.alpha = alpha;
            this.infoLabel.pos.x = centerX;
            if (isNew)
            {
                this.infoLabel.label.color = HSLColor.Lerp(Menu.Menu.MenuColor(Menu.Menu.MenuColors.VeryDarkGrey), Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey), 0.5f + 0.5f * Mathf.Sin(flashSin)).rgb;
            }
            if (locked)
            {

                this.unlockProgres.pos.x = centerX;
                this.unlockProgres.alpha = alpha;
                //this.unlockProgres.label.label.alpha = alpha;
                //this.unlockProgres.label.pos.x = centerX;
                this.unlockProgres.token.pos.x = centerX;
                this.unlockProgres.token.alpha = alpha;
            }
            base.GrafUpdate(1f); // force latest pos
        }
    }
}
