using Menu.Remix.MixedUI;
using System.Collections.Generic;
using UnityEngine;

namespace RainMeadow
{
    public class OpComboBox2 : OpComboBox
    {
        public OpComboBox2(ConfigurableBase configBase, Vector2 pos, float width, List<ListItem> list) : base(configBase, pos, width, list)
        {
        }
        public override void Change()
        {
            base.Change();
            this.OnChanged?.Invoke();
        }
        public event System.Action OnChanged;

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            if (this._rectList != null && !_rectList.isHidden)
            {
                myContainer.MoveToFront();

                for (int j = 0; j < 9; j++)
                {
                    this._rectList.sprites[j].alpha = 1;
                }
            }
        }
    }
}
