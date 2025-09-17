using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Profiling;

namespace RainMeadow
{
    public static class ProfilerOverlay
    {
        public static bool profilerActive;

        private static FContainer overlayContainer;
        private static int framerate;

        private class ProfilerGraph
        {
            private readonly Func<float> _getValue;

            public enum Format
            {
                None,
                MB
            }
            private Format format;

            public FSprite[] graphLines;
            public FSprite[] indexLines;
            public FLabel[] labels;

            public FSprite backDrop;

            private float[] pastValues;

            private Vector2 pos;

            private int width, height, index;
            public float min, max;

            private bool inverted;
            private bool incremented;

            public float CurrentValue => _getValue();

            public ProfilerGraph(FContainer container, Vector2 pos, int width, int height, Func<float> readValue, float min, float max, bool inverted = false, bool incremented = false, Format format = Format.None)
            {
                _getValue = readValue;
                this.width = width;
                this.height = height;
                this.pos = pos;

                this.min = min;
                this.max = max;
                this.inverted = inverted;
                this.format = format;

                backDrop = new FSprite("pixel")
                {
                    color = Color.black,
                    x = pos.x,
                    y = pos.y,
                    alpha = 0.25f,
                    anchorX = 0,
                    anchorY = 0,
                    scaleX = width,
                    scaleY = height,
                };
                container.AddChild(backDrop);

                graphLines = new FSprite[width];
                for (int i = 0; i < graphLines.Length; i++)
                {
                    graphLines[i] = new FSprite("pixel");
                    graphLines[i].x = pos.x + i;
                    graphLines[i].y = pos.y;
                    graphLines[i].anchorY = 0;
                    container.AddChild(graphLines[i]);
                }

                indexLines = new FSprite[3];

                indexLines[0] = new FSprite("pixel"); // Min
                indexLines[0].color = inverted ? Color.green : Color.red;

                indexLines[1] = new FSprite("pixel"); // Max
                indexLines[1].color = inverted ? Color.red : Color.green;

                indexLines[2] = new FSprite("pixel"); // Average
                indexLines[2].color = Color.yellow;
                indexLines[2].alpha = 0.5f;

                for (int i = 0; i < indexLines.Length; i++)
                {
                    indexLines[i].x = pos.x;
                    indexLines[i].y = pos.y;

                    indexLines[i].anchorX = 0;
                    indexLines[i].scaleX = width;
                    container.AddChild(indexLines[i]);
                }

                indexLines[1].y = pos.y + height;

                labels = new FLabel[3];

                labels[0] = new FLabel(Custom.GetFont(), min.ToString() + (format == Format.MB ? " MB" : "")) // Min
                {
                    alignment = FLabelAlignment.Left,
                    x = indexLines[0].x + width + 5f,
                    y = indexLines[0].y - 15,
                };
                container.AddChild(labels[0]);

                labels[1] = new FLabel(Custom.GetFont(), max.ToString() + (format == Format.MB ? " MB" : "")) // Max
                {
                    alignment = FLabelAlignment.Left,
                    x = indexLines[1].x + width + 5f,
                    y = indexLines[1].y + 15,
                };
                container.AddChild(labels[1]);

                labels[2] = new FLabel(Custom.GetFont(), (max / 2).ToString() + (format == Format.MB ? " MB" : "")) // Average
                {
                    alignment = FLabelAlignment.Left,
                    x = indexLines[2].x + width + 5f,
                    y = indexLines[2].y,
                };
                container.AddChild(labels[2]);
            }

            public void Increment()
            {
                incremented = false;
                Update();
                incremented = true;
            }

            public void Update()
            {
                if (incremented) return;
                var value = Mathf.InverseLerp(min, max, CurrentValue);
                var graphY = Mathf.Lerp(0, height, value);

                foreach(var graphLine in graphLines)
                {
                    graphLine.color = Color.Lerp(Color.black, graphLine.color, 0.995f);
                }

                graphLines[index].scaleY = graphY;
                if (inverted)
                {
                    graphLines[index].color = Color.Lerp(Color.green, Color.red, value);
                }
                else
                {
                    graphLines[index].color = Color.Lerp(Color.red, Color.green, value);
                }
                indexLines[2].y = pos.y + graphY;

                labels[2].y = indexLines[2].y;
                labels[2].text = CurrentValue.ToString() + (format == Format.MB ? " MB" : "");

                index++;
                if (index >= width) index = 0;
            }

            public void RemoveSprites()
            {
                backDrop.RemoveFromContainer();
                foreach (var sprite in graphLines)
                {
                    sprite.RemoveFromContainer();
                }
                foreach (var sprite in indexLines)
                {
                    sprite.RemoveFromContainer();
                }
                foreach(var label in labels)
                {
                    label.RemoveFromContainer();
                }
            }
        }

        private static List<ProfilerGraph> profilerGraphs = new List<ProfilerGraph>();
        private static bool keyDown;

        public static void Update(RainWorldGame self, float dt)
        {
            if (self.devToolsActive && Input.GetKey(KeyCode.Equals) && !keyDown)
            {
                profilerActive = !profilerActive;
            }
            keyDown = self.devToolsActive && Input.GetKey(KeyCode.Equals);
            if (overlayContainer == null && self.devToolsActive && profilerActive)
            {
                CreateOverlay(self);
            }
            if (overlayContainer != null && !self.devToolsActive || !profilerActive)
            {
                RemoveOverlay(self);
            }
            if (overlayContainer == null) return;
            Vector2 screenSize = self.rainWorld.options.ScreenSize;

            framerate = (int)(1.0f / dt);

            foreach(var graph in profilerGraphs)
            {
                graph.Update();
            }
        }

        public static void CreateOverlay(RainWorldGame self)
        {
            if (MeadowProfiler.Instance is null)
            {
                MeadowProfiler.Instance = new();
            }

            Vector2 screenSize = self.rainWorld.options.ScreenSize;
            overlayContainer = new FContainer();

            // Framerate
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Framerate")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 10,
            });

            profilerGraphs.Add(new ProfilerGraph(overlayContainer, new Vector2(5.01f, screenSize.y - 120), 240, 80, () =>
            {
                return (float)framerate;
            }, 0f, (float)(Application.targetFrameRate == -1 ? 240 : Application.targetFrameRate)));

            // Memory
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Memory Usage")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 210,
            });

            profilerGraphs.Add(new ProfilerGraph(overlayContainer, new Vector2(5.01f, screenSize.y - 320), 240, 80, () =>
            {
                return Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f); // MB Value
            }, 0f, SystemInfo.systemMemorySize, true, false, ProfilerGraph.Format.MB));

            // Bandwidth
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Bandwidth Usage")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 410,
            });
            // TODO Implement this
            profilerGraphs.Add(new ProfilerGraph(overlayContainer, new Vector2(5.01f, screenSize.y - 520), 240, 80, () =>
            {
                return 0;
            }, 0f, 10f, true, false, ProfilerGraph.Format.MB));

            // Update Timing
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Game Update (ms)")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 610,
            });
            // TODO Implement this
            profilerGraphs.Add(new ProfilerGraph(overlayContainer, new Vector2(5.01f, screenSize.y - 720), 240, 80, () =>
            {
                float value = 0;
                value = (float)MeadowProfiler.Instance?.gameUpdateTiming;
                if (profilerGraphs[3].max < value)
                {
                    profilerGraphs[3].max = value;
                }
                return value;
            }, 0f, 50f, true, false));


            Futile.stage.AddChild(overlayContainer);
        }

        public static void RemoveOverlay(RainWorldGame self)
        {
            profilerGraphs.Clear();
            overlayContainer?.RemoveFromContainer();
            overlayContainer = null;
            MeadowProfiler.Instance?.Destroy();
        }
    }
}
