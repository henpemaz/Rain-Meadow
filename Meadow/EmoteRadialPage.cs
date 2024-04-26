using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteRadialPage
    {
        private Emote[] emotes;
        private MeadowAvatarCustomization customization;
        private TriangleMesh[] meshes;
        private FSprite[] icons;
        private FSprite[] tiles;
        private TriangleMesh centerMesh;


        public Color colorUnselected = new Color(0f, 0f, 0f, 0.2f);
        public Color colorSelected = new Color(1f, 1f, 1f, 0.2f);

        // relative to emote size
        const float innerRadiusFactor = 1f;
        const float outterRadiusFactor = 2.076f;
        const float emoteRadiusFactor = 1.42f;
        float innerRadius;
        float outterRadius;
        float emoteRadius;

        public EmoteRadialPage(HUD.HUD hud, FContainer container, MeadowAvatarCustomization customization, Emote[] emotes, Vector2 pos, float emotesSize)
        {
            this.customization = customization;
            this.meshes = new TriangleMesh[8];
            this.icons = new FSprite[8];
            this.tiles = new FSprite[8];
            this.centerMesh = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[] { new(0, 1, 2), new(0, 2, 3), new(0, 3, 4), new(0, 4, 5), new(0, 5, 6), new(0, 6, 7), new(0, 7, 8), new(0, 8, 1), }, false);
            container.AddChild(this.centerMesh);

            this.innerRadius = innerRadiusFactor * emotesSize;
            this.outterRadius = outterRadiusFactor * emotesSize;
            this.emoteRadius = emoteRadiusFactor * emotesSize;

            centerMesh.color = colorUnselected;
            for (int i = 0; i < 8; i++)
            {
                Vector2 dira = RWCustom.Custom.RotateAroundOrigo(Vector2.up, (-1f + 2 * i) * (360f / 16f));
                Vector2 dirb = RWCustom.Custom.RotateAroundOrigo(Vector2.up, (1f + 2 * i) * (360f / 16f));
                this.meshes[i] = new TriangleMesh("Futile_White", new TriangleMesh.Triangle[] { new(0, 1, 2), new(2, 3, 0) }, false);

                meshes[i].vertices[0] = pos + dira * innerRadius;
                meshes[i].vertices[1] = pos + dira * outterRadius;
                meshes[i].vertices[2] = pos + dirb * outterRadius;
                meshes[i].vertices[3] = pos + dirb * innerRadius;

                meshes[i].color = colorUnselected;

                container.AddChild(meshes[i]);

                tiles[i] = new FSprite("Futile_White");
                tiles[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                tiles[i].alpha = 0.6f;
                tiles[i].SetPosition(pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f)));
                container.AddChild(tiles[i]);

                icons[i] = new FSprite("Futile_White");
                icons[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                icons[i].alpha = 0.6f;
                icons[i].SetPosition(pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f)));
                container.AddChild(icons[i]);

                centerMesh.vertices[i + 1] = pos + dira * innerRadius;
            }
            centerMesh.vertices[0] = pos;

            SetEmotes(emotes);
        }

        public void SetEmotes(Emote[] emotes)
        {
            this.emotes = emotes;
            for (int i = 0; i < icons.Length; i++)
            {
                if (emotes[i] != null)
                {
                    icons[i].SetElementByName(customization.GetEmote(emotes[i]));
                    icons[i].alpha = 0.6f;
                    tiles[i].SetElementByName(customization.GetBackground(emotes[i]));
                    tiles[i].alpha = 0.6f;
                }
                else
                {
                    icons[i].alpha = 0f;
                    tiles[i].alpha = 0f;
                }
            }
        }

        internal void SetSelected(int selected)
        {
            ClearSelection();
            if (selected > -1 && emotes[selected] != null) meshes[selected].color = colorSelected; else centerMesh.color = colorSelected;
        }

        internal void ClearSelection()
        {
            centerMesh.color = colorUnselected;
            for (int i = 0; i < 8; i++)
            {
                meshes[i].color = colorUnselected;
            }
        }
    }
}
