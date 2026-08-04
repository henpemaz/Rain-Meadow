using System.Collections.Generic;
using RainMeadow.UI.Components;
using UnityEngine;

namespace RainMeadow
{
    /// <summary>
    /// The HUD addition to <see cref="SlugIcon"/>. Draws either a single atlas element (Kill_Slugcat, for ex.) or a
    /// layered slugcat class icon (like the beautiful sluggy icons for the lobby), sharing <see cref="SlugIcon"/>'s sprite and color maps.
    /// </summary>
    /// <remarks>
    /// <see cref="SlugIcon"/> itself is a <c>PositionedMenuObject</c> and so needs a menu to live
    /// in. Plain <see cref="FSprite"/>s in a container of
    /// its own instead so it can be managed as one icon. Thank you @Timbits & @None
    /// </remarks>
    public class SlugcatClassIconHud
    {
        /// <summary>If any atlses are missing, fallback to ye ol faithful</summary>
        private const string MissingElementFallback = "Kill_Slugcat";

        public FContainer container = new();
        public List<FSprite> sprites = [];

        private List<string> spriteNames = [];
        private string element = "";
        private string slugcat = "";
        private bool dead;

        public string ElementName => element;

        public float x
        {
            get => container.x;
            set => container.x = value;
        }
        public float y
        {
            get => container.y;
            set => container.y = value;
        }
        public float Alpha
        {
            get => container.alpha;
            set => container.alpha = value;
        }
        public float scale
        {
            get => container.scale;
            set => container.scale = value;
        }

        public SlugcatClassIconHud(FContainer parent)
        {
            parent.AddChild(container);
            container.alpha = 0f;
            container.x = -1000f;
        }

        /// <summary>Draw a single atlas element (tinted) <paramref name="color"/>.</summary>
        public void SetElement(string elementName, Color color)
        {
            if (element != elementName)
            {
                element = elementName;
                slugcat = "";
                Rebuild();
            }

            for (int i = 0; i < sprites.Count; i++)
                sprites[i].color = color;
        }

        /// <summary>
        /// Draw <paramref name="slugcatName"/>'s layered class icon, colored by
        /// <paramref name="colors"/> (head, face, and optional features).
        /// </summary>
        public void SetSlugcat(string slugcatName, bool dead, List<Color>? colors)
        {
            if (element != "" || slugcat != slugcatName || this.dead != dead)
            {
                element = "";
                slugcat = slugcatName;
                this.dead = dead;
                Rebuild();
            }

            ApplyPalette(colors);
        }

        public void RemoveFromContainer()
        {
            ClearSprites();
            container.RemoveFromContainer();
        }

        private void ClearSprites()
        {
            for (int i = 0; i < sprites.Count; i++)
                sprites[i].RemoveFromContainer();
            sprites = [];
            spriteNames = [];
        }

        private void Rebuild()
        {
            ClearSprites();

            if (element != "")
            {
                AddSprite(element);
                return;
            }

            if (slugcat == "")
                return;

            SlugIcon.LoadAtlas();
            foreach (string spriteName in SlugIcon.GetSpriteNames(slugcat, dead))
                AddSprite(spriteName, skipIfMissing: true);

            // fallback for modded scugs
            if (sprites.Count == 0)
                AddSprite(MissingElementFallback);
        }

        private void AddSprite(string spriteName, bool skipIfMissing = false)
        {
            if (!Futile.atlasManager.DoesContainElementWithName(spriteName))
            {
                if (skipIfMissing)
                    return;
                RainMeadow.Error($"SlugcatClassIconHud: no atlas element named {spriteName}");
                spriteName = MissingElementFallback;
            }

            FSprite sprite = new(spriteName, true);
            container.AddChild(sprite);
            sprites.Add(sprite);
            spriteNames.Add(spriteName);
        }

        private void ApplyPalette(List<Color>? newColors)
        {
            if (sprites.Count == 0)
                return;

            List<Color> colors = SlugIcon.GetColors(slugcat, newColors);

            for (int i = 0; i < sprites.Count; i++)
            {
                int colorIndex = SlugIcon.ColorIndexForSprite(spriteNames[i]);
                sprites[i].color =
                    colorIndex >= 0 && colorIndex < colors.Count ? colors[colorIndex] : Color.white;
            }
        }
    }
}
