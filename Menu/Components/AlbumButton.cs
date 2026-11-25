using Menu;
using UnityEngine;

namespace RainMeadow.UI.Components
{
    public class AlbumButton : IllustrationButton, IHaveADescription
    {
        public FSprite ring;

        public AlbumButton(Menu.Menu menu, MenuObject owner, Vector2 pos, float desiredWidth, string fileName, string description = "")
            : base(menu, owner, pos, "", fileName)
        {
            float scale = desiredWidth / size.x;
            portrait.sprite.scale = scale;
            size *= scale;
            this.description = description;

            roundedRect.RemoveSprites();
            RemoveSubObject(roundedRect);
            selectRect.RemoveSprites();
            RemoveSubObject(selectRect);

            ring = new FSprite("Futile_White")
            {
                shader = menu.manager.rainWorld.Shaders["VectorCircle"],
            };
            Container.AddChild(ring);
            ring.MoveInFrontOfOtherNode(portrait.sprite);
        }

        public override void GrafUpdate(float timeStacker)
        {
            base.GrafUpdate(timeStacker);
            float bump = (buttonBehav.sizeBump + 0.5f * Mathf.Sin(buttonBehav.extraSizeBump * Mathf.PI)) * (buttonBehav.clicked ? 0f : 1f);
            float radius = size.x / 2f + 2f + 3f * bump;
            ring.SetPosition(DrawPos(timeStacker) + size / 2f);
            ring.scale = radius / 8f;
            ring.alpha = 2f / radius;
            ring.color = InterpColor(timeStacker, Menu.Menu.MenuColor(Menu.Menu.MenuColors.MediumGrey));
            portrait.sprite.rotation = Mathf.Lerp(lastSpin, spin, timeStacker);

        }
        public float spin, lastSpin;
        public override void Update()
        {
            base.Update();
            lastSpin = spin;
            spin += Mathf.Lerp(0.3f, 2f, buttonBehav.col);
        }

        public override void RemoveSprites()
        {
            ring.RemoveFromContainer();
            base.RemoveSprites();
        }

        public string description;
        public string Description { get => description; set => description = value; }
    }
}
