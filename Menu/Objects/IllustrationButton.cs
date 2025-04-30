using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text;
using System.Threading.Tasks;

namespace RainMeadow.UI.Components
{
    public class IllustrationButton : ButtonTemplate, ButtonScroller.IPartOfButtonScroller
    {
        public float Alpha { get => alpha; set => alpha = value; }
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public IllustrationButton(Menu.Menu menu, MenuObject owner, Vector2 pos, string folderName, string fileName) : base(menu, owner, pos, Vector2.zero)
        {
            roundedRect = new(menu, this, Vector2.zero, size, true);
            selectRect = new(menu, this, Vector2.zero, size, false);
            portrait = new(menu, this, folderName, fileName, Vector2.zero, true, true);
            size = portrait.size;
            subObjects.AddRange([portrait, roundedRect, selectRect]);
        }
        public override void Update()
        {
            base.Update();
            buttonBehav.Update();
            roundedRect.fillAlpha = Mathf.Lerp(0.3f, 0.6f, buttonBehav.col);
            roundedRect.addSize = new Vector2(10f, 6f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.1415927f)) * (buttonBehav.clicked ? 0f : 1f);
            selectRect.addSize = new Vector2(2f, -2f) * (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * 3.1415927f)) * (buttonBehav.clicked ? 0f : 1f);
            buttonBehav.greyedOut = forceGreyedOut || Alpha < 1;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            roundedRect.size = size;
            selectRect.size = size;
            portrait.pos = size / 2;

        }
        public virtual void UpdateAlpha(float alpha)
        {
            portrait.setAlpha = alpha * desiredOrigAlpha;
            for (int i = 0; i < roundedRect.sprites.Length; i++)
            {
                roundedRect.sprites[i].alpha = alpha;
                roundedRect.fillAlpha = alpha / 2;
            }
            for (int i = 0; i < selectRect.sprites.Length; i++)
            {
                selectRect.sprites[i].alpha = alpha;
            }
        }
        public void SetNewImage(string folderName, string fileName)
        {
            portrait.folderName = folderName;
            portrait.fileName = fileName;
            portrait.LoadFile();
            portrait.sprite.SetElementByName(portrait.fileName);
        }

        public float alpha = 1, desiredOrigAlpha = 1;
        public bool forceGreyedOut;
        public MenuIllustration portrait;
        public RoundedRect roundedRect, selectRect;
    }

}
