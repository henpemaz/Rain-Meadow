using RWCustom;
using UnityEngine;
using static RainMeadow.MeadowProgression;

namespace RainMeadow
{
    public class EmoteRadialDisplayer
    {
        private FContainer container;
        private TriangleMesh[] meshes;
        private FSprite[] icons;
        private FSprite[] lasticons;
        private FSprite[] tiles;
        private FSprite[] lasttiles;
        private TriangleMesh centerMesh;
        private Vector2[] emoteInitPositions;


        public Color colorUnselected = new Color(0f, 0f, 0f, 0.2f);
        public Color colorSelected = new Color(1f, 1f, 1f, 0.2f);

        // relative to emote size
        const float innerRadiusFactor = 1f;
        const float outterRadiusFactor = 2.076f;
        const float emoteRadiusFactor = 1.42f;
        float innerRadius;
        float outterRadius;
        float emoteRadius;

        public bool isVisible
        {
            get { return container.isVisible; }
            set { container.isVisible = value; }
        }

        public Vector2 pos
        {
            get { return container.GetPosition(); }
            set { container.SetPosition(value); }
        }

        public float alpha
        {
            get { return container.alpha; }
            set { container.alpha = value; container.isVisible = (value > 0f); }
        }

        public EmoteRadialDisplayer(FContainer parentContainer, MeadowAvatarData customization, Emote[] emotes, Vector2 pos, float emotesSize)
        {
            this.container = new FContainer();
            this.meshes = new TriangleMesh[8];
            this.icons = new FSprite[8];
            this.lasticons = new FSprite[8];
            this.tiles = new FSprite[8];
            this.lasttiles = new FSprite[8];
            this.emoteInitPositions = new Vector2[8];

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

                lasttiles[i] = new FSprite("Futile_White");
                lasttiles[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                lasttiles[i].alpha = 0.0f;
                lasttiles[i].SetPosition(pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f)));
                container.AddChild(lasttiles[i]);

                lasticons[i] = new FSprite("Futile_White");
                lasticons[i].scale = emotesSize / EmoteDisplayer.emoteSourceSize;
                lasticons[i].alpha = 0.0f;
                lasticons[i].SetPosition(pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f)));
                container.AddChild(lasticons[i]);

                emoteInitPositions[i] = pos + RWCustom.Custom.RotateAroundOrigo(Vector2.up * emoteRadius, (i) * (360f / 8f));
                centerMesh.vertices[i + 1] = pos + dira * innerRadius;
            }
            centerMesh.vertices[0] = pos;

            SetEmotes(emotes, customization);

            parentContainer.AddChild(container);
        }
        
        internal void positionssss(float inittime, bool fliptotheright)
        {
            //if (Time.time - inittime>4) return;
            for (int i = 0; i < 8; i++)
            {
                if ((tiles[i] == null)) continue;
                float thing = inittime + ((fliptotheright?i:(7-i)) / 40f);

                float y = (1 - Custom.SCurve(Mathf.InverseLerp(thing, thing + 0.3f, Time.time + 0.12f), 0.65f)) * -10f * (fliptotheright ? 1f : -1f);
                Vector2 offset = Custom.RotateAroundOrigo(new Vector2(0, y), (i) * (360f / 8f));

                Vector2 newpos = emoteInitPositions[i] + offset;
                tiles[i].SetPosition(newpos);
                icons[i].SetPosition(newpos);

                float thealpha = icons[i].alpha;
                if (Time.time == inittime || (thealpha != 0.6f && thealpha != 1))
                {
                    float alpha = Custom.SCurve(Mathf.InverseLerp(thing, thing + 0.3f, Time.time), 0.65f) * 0.6f;
                    tiles[i].alpha = alpha;
                    icons[i].alpha = alpha;
                }
            }

            for (int j = 0; j < 8; j++)
            {
                if ((lasttiles[j] == null)) continue;
                float thing = inittime + ((fliptotheright ? j : (7 - j)) / 40f);

                float y = (1 - Custom.SCurve(Mathf.InverseLerp(0f, 0.3f, 0.3f - ((Time.time - thing) + 0.12f)), 0.65f)) * -10f * (fliptotheright ? -1f : 1f);
                Vector2 offset = Custom.RotateAroundOrigo(new Vector2(0, y), j * (360f / 8f));
                Vector2 newpos = emoteInitPositions[j] + offset;
                lasttiles[j].SetPosition(newpos);
                lasticons[j].SetPosition(newpos);

                float alpha = Custom.SCurve(Mathf.InverseLerp(0f, 0.3f, 0.3f - (Time.time - thing)), 0.65f) * 0.6f;
                lasttiles[j].alpha = alpha;
                lasticons[j].alpha = alpha;
            }
        }

        public void DramaticEntrance()
        {

        }

        public void SetEmotes(Emote[] emotes, MeadowAvatarData customization)
        {
            for (int i = 0; i < icons.Length; i++)
            {
                if (emotes[i] != null)
                {
                    if (icons[i] != null)
                    {
                        lasticons[i].element = icons[i].element;
                        lasticons[i].color   = icons[i].color;
                        lasticons[i].alpha   = icons[i].alpha;
                        lasticons[i].x = icons[i].x;
                        lasticons[i].y = icons[i].y;
                        lasttiles[i].element = tiles[i].element;
                        lasttiles[i].color   = tiles[i].color;
                        lasttiles[i].alpha   = tiles[i].alpha;
                        lasttiles[i].x = tiles[i].x;
                        lasttiles[i].y = tiles[i].y;
                    }
                    else
                    {
                        lasticons[i].alpha = 0f;
                        lasttiles[i].alpha = 0f;
                    }
                    icons[i].SetElementByName(customization.GetEmote(emotes[i]));
                    icons[i].color = customization.EmoteColor(emotes[i]);
                    icons[i].alpha = 0f;
                    tiles[i].SetElementByName(customization.GetBackground(emotes[i]));
                    tiles[i].color = customization.EmoteBackgroundColor(emotes[i]);
                    tiles[i].alpha = 0f;
                }
                else
                {
                    if (icons[i] != null)
                    {
                        //lasticons[i] = icons[i];
                        lasticons[i].element = icons[i].element;
                        lasticons[i].color   = icons[i].color;
                        lasticons[i].alpha   = icons[i].alpha;
                        lasticons[i].x = icons[i].x;
                        lasticons[i].y = icons[i].y;
                        lasttiles[i].element = tiles[i].element;
                        lasttiles[i].color   = tiles[i].color;
                        lasttiles[i].alpha   = tiles[i].alpha;
                        lasttiles[i].x = tiles[i].x;
                        lasttiles[i].y = tiles[i].y;
                    }
                    else
                    {
                        lasticons[i].alpha = 0f;
                        lasttiles[i].alpha = 0f;
                    }
                    icons[i].alpha = 0f;
                    tiles[i].alpha = 0f;
                }
            }
        }

        internal void SetSelected(int selected)
        {
            ClearSelection();
            if (selected > -1) meshes[selected].color = colorSelected; else centerMesh.color = colorSelected;
        }

        internal void ClearSelection()
        {
            centerMesh.color = colorUnselected;
            for (int i = 0; i < 8; i++)
            {
                meshes[i].color = colorUnselected;
            }
        }

        internal void ClearSprites()
        {
            container.RemoveFromContainer();
            container.RemoveAllChildren();
            container = null;
            meshes = null;
            tiles = null;
            icons = null;
        }
    }
}
