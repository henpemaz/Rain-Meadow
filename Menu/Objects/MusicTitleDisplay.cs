using Menu;
using UnityEngine;

namespace RainMeadow
{
    public class MusicTitleDisplay : MenuLabel
    {
        static int dttt = 20;
        static float lastprogress = 0f;
        public FSprite musicSprite;
        public MusicTitleDisplay(Menu.Menu menu, MenuObject owner, string displayText, Vector2 pos, Vector2 size) : base(menu, owner, displayText, pos, size, false)
        {
            musicSprite = new FSprite("musicSymbol", true);
            musicSprite.SetPosition(pos); // + size / 2
            musicSprite.alpha = 0f;
            owner.menu.container.AddChild(musicSprite);
            lastprogress = 0f;
            label.alignment = FLabelAlignment.Left;
        }
        public void Display(string desiredtext, float progress)
        {
            bool appearing = lastprogress <= progress;
            if (appearing && (progress < 0.5f)) return;
            if (!appearing) desiredtext = "";
            lastprogress = progress;
            if (desiredtext == text) return;
            if (dttt > 0) 
                dttt--;
            else {
                dttt = appearing ? 4 : 0;
                bool correct = true;
                char goodletter = 'a';
                if (desiredtext.Length < text.Length) correct = false;
                else
                {
                    for (int i = 0; i < desiredtext.Length; i++) 
                    {
                        if (i == text.Length) {
                            goodletter = desiredtext[i];
                            break;
                        }
                        if (desiredtext[i] != text[i])
                        {
                            correct = false;
                            break;
                        }
                    }
                }
                if (!correct) { text = text.Substring(0, text.Length - 1); } else { text = text + goodletter; }
                if (!appearing && (progress < 0.7) && text.Length > 0) text = text.Substring(0, text.Length - 1);
                if (!appearing && (progress < 0.4) && text.Length > 0) text = text.Substring(0, text.Length - 1);
            }
        }

        public override void Update()
        {
            Display(menu.manager.musicPlayer?.song?.name ?? "", (menu as PauseMenu).blackFade);

            base.Update();
            musicSprite.alpha = RWCustom.Custom.SCurve(lastprogress, 0.65f);
            musicSprite.x = pos.x - 16f; // ((1 - RWCustom.Custom.SCurve(lastprogress + 0.4f, 0.65f)) * 30)  // + size.x / 2  - (label.textRect.width / 2 + 10f);
            musicSprite.y = pos.y + size.y / 2 + Mathf.Sin(Time.time * 3f) * 2f - ((1 - RWCustom.Custom.SCurve(lastprogress + 0.4f, 0.65f)) * 30);
        }
    } 
}
