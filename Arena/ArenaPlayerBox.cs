using HarmonyLib;
using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public class ArenaPlayerBox : RectangularMenuObject, ButtonScroller.IPartOfButtonScroller
    {
        public float Alpha { get => alpha; set => alpha = value; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public ArenaPlayerBox(Menu.Menu menu, MenuObject owner, Vector2 pos, Vector2 size = default) : base(menu, owner, pos, size == default? new(380, 120) : size)
        {
            sprites = new FSprite[2];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = new("pixel")
                {
                    anchorX = 0,
                    anchorY = 0,
                };
                Container.AddChild(sprites[i]);
            }
            slugcatButton = new(menu, this, new(10, 10), "", "MultiplayerPortrait01");
            slugcatButton.size += new Vector2(16, 16);
            subObjects.Add(slugcatButton);
        }
        public override void RemoveSprites()
        {
            base.RemoveSprites();
            sprites.Do(x => x.RemoveFromContainer());
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            Vector2 size = DrawSize(timeStacker), pos = DrawPos(timeStacker);
            for (int i = 0; i < 2; i++)
            {
                sprites[i].scaleX = size.x;
                sprites[i].x = pos.x;
                sprites[i].y = pos.y + (size.y * i); //first sprite is bottomLine, second sprite is topLine
            }
        }
        public void UpdateAlpha(float alpha)
        {
            slugcatButton.alpha = alpha;
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].alpha = alpha;
            }
        }

        public float alpha;
        public FSprite[] sprites;
        public IllustrationButton slugcatButton;

    }
}
