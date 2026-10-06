using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class AlbumButton : IllustrationButton, IHaveADescription
    {
        public AlbumButton(Menu.Menu menu, MenuObject owner, Vector2 pos, float desiredWidth, string fileName, string description = "")
            : base(menu, owner, pos, "", fileName)
        {
            float scale = desiredWidth / size.x;
            portrait.sprite.scale = scale;
            size *= scale;
            this.description = description;
        }

        public string description;
        public string Description { get => description; set => description = value; }
    }
}
