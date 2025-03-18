using Menu.Remix.MixedUI;
using UnityEngine;
using Menu;

namespace RainMeadow
{
    public class LobbyChatTextBox : OpTextBox
    {
        public LobbyChatTextBox(ConfigurableBase config, Vector2 pos, float sizeX, float logSizeY) : base(config, pos, sizeX)
        {
            this.maxLength = (int)Mathf.Round(sizeX / 5);
        }
    }
}
