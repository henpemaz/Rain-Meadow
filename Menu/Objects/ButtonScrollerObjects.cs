using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Menu;
using Menu.Remix.MixedUI;
using UnityEngine;
using static RainMeadow.ButtonScroller;

namespace RainMeadow.UI.Components
{
    public class ScrollSymbolButton : SimplerSymbolButton, IPartOfButtonScroller
    {
        public float Alpha { get; set; } = 1;
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public ScrollSymbolButton(Menu.Menu menu, MenuObject owner, string symbolName, string signalText, Vector2 pos, Vector2 size = default) : base(menu, owner, symbolName, signalText, pos)
        {
            this.size = size == default ? this.size : size;
        }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            roundedRect.size = size;
        }

    }
    public class AlignedMenuLabel(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, bool bigText, FTextParams txtParams = null) : MenuLabel(menu, owner, text, pos, size, bigText, txtParams), ButtonScroller.IPartOfButtonScroller
    {
        public float Alpha { get; set; } = 1;
        public Vector2 Pos { get => pos; set => pos = value; }
        public Vector2 Size { get => size; set => size = value; }
        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            label.x = DrawX(timeStacker) + (labelPosAlignment == FLabelAlignment.Left ? 0 : labelPosAlignment == FLabelAlignment.Right ? size.x : size.x / 2);
            label.y = DrawY(timeStacker) + (verticalLabelPosAlignment == OpLabel.LabelVAlignment.Bottom ? 0 : verticalLabelPosAlignment == OpLabel.LabelVAlignment.Top ? size.y : size.y / 2);
        }
        public FLabelAlignment labelPosAlignment = FLabelAlignment.Center;
        public OpLabel.LabelVAlignment verticalLabelPosAlignment = OpLabel.LabelVAlignment.Center;
    }

    public class UsernameMenuLabel : AlignedMenuLabel
    {
        private FSprite hostIcon;
        public bool Host => hostIcon != null;
        public UsernameMenuLabel(Menu.Menu menu, MenuObject owner, string text, Vector2 pos, Vector2 size, bool bigText, FTextParams txtParams = null) : base(menu, owner, text, pos, size, bigText, txtParams)
        {
            if (OnlineManager.lobby.owner.id.GetPersonaName() == text)
            {
                hostIcon = new FSprite("ChieftainA")
                {
                    x = pos.x,
                    y = pos.y,
                    scaleX = 0.5f,
                    scaleY = 0.5f,
                    anchorX = 0.25f,
                };
                Container.AddChild(hostIcon);
            }
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (hostIcon == null) return;
            label.x += 14;
            hostIcon.x = DrawX(timeStacker) + (labelPosAlignment == FLabelAlignment.Left ? 0 : labelPosAlignment == FLabelAlignment.Right ? size.x : size.x / 2);
            hostIcon.y = DrawY(timeStacker) + (verticalLabelPosAlignment == OpLabel.LabelVAlignment.Bottom ? 0 : verticalLabelPosAlignment == OpLabel.LabelVAlignment.Top ? size.y : size.y / 2);
            hostIcon.alpha = label.alpha;
            hostIcon.color = label.color;
        }

        public override void RemoveSprites()
        {
            if (hostIcon != null)
            {
                hostIcon.RemoveFromContainer();
            }
            base.RemoveSprites();
        }
    }
}
