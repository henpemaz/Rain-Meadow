using RWCustom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static int totalWritten = 0;
        public static int totalRead = 0;

        private static FContainer overlayContainer;
        private static int framerate;

        private class ProfilerGraph
        {
            private readonly Func<float> _getValue;

            public enum Format
            {
                None,
                MB,
                KBPS
            }
            private Format format;
            private string formatString;

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

                this.formatString = "";

                switch (format)
                {
                    case Format.MB:
                        this.formatString = "MB";
                        break;
                    case Format.KBPS:
                        this.formatString = "Kbps";
                        break;
                }

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

                labels[0] = new FLabel(Custom.GetFont(), min.ToString() + formatString) // Min
                {
                    alignment = FLabelAlignment.Left,
                    x = indexLines[0].x + width + 5f,
                    y = indexLines[0].y - 15,
                };
                container.AddChild(labels[0]);

                labels[1] = new FLabel(Custom.GetFont(), max.ToString() + formatString) // Max
                {
                    alignment = FLabelAlignment.Left,
                    x = indexLines[1].x + width + 5f,
                    y = indexLines[1].y + 15,
                };
                container.AddChild(labels[1]);

                labels[2] = new FLabel(Custom.GetFont(), (max / 2).ToString() + formatString) // Average
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
                labels[2].text = CurrentValue.ToString() + formatString;

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

        private class StateChart
        {
            public FContainer container;

            public FSprite[] sprites;
            public FLabel mainLabel;
            public List<FLabel> labels = new();

            public Mode displayMode;

            private bool pageKeyDown;

            public enum Mode
            {
                State,
                Entity,
                Opo,
                Comparison,
                Player
            }

            public Vector2 pos;
            public StateChart(FContainer container, Vector2 pos)
            {
                this.container = container;
                this.pos = pos;
                this.displayMode = Mode.State;
                this.sprites = new FSprite[360];

                for (int i = 0; i < sprites.Length; i++)
                {
                    sprites[i] = new FSprite("pixel")
                    {
                        x = pos.x,
                        y = pos.y,
                        anchorY = 0,
                        scaleY = 80,
                        scaleX = 1.5f,
                        rotation = Mathf.InverseLerp(0f, sprites.Length, i) * 360f,
                        color = Color.gray
                    };
                    container.AddChild(sprites[i]);
                }

                mainLabel = new FLabel(Custom.GetFont(), "")
                {
                    alignment = FLabelAlignment.Left,
                    x = pos.x - 80,
                    y = pos.y + 100,
                };
                container.AddChild(mainLabel);
            }

            public void Update()
            {
                if (StateProfiler.Instance is null) return;
                if (Input.GetKey(KeyCode.RightBracket) && !pageKeyDown)
                {
                    displayMode = (Mode)(((int)displayMode + 1) % Enum.GetValues(typeof(Mode)).Length);
                    ClearChart();
                }
                if (Input.GetKey(KeyCode.LeftBracket) && !pageKeyDown)
                {
                    displayMode = displayMode - 1 < 0 ? (Mode)Enum.GetValues(typeof(Mode)).Length - 1 : (Mode)(((int)displayMode - 1) % Enum.GetValues(typeof(Mode)).Length);
                    ClearChart();
                }
                pageKeyDown = Input.GetKey(KeyCode.RightBracket) || Input.GetKey(KeyCode.LeftBracket);
                switch (displayMode)
                {
                    case Mode.State:
                        StateDisplay();
                        break;
                    case Mode.Entity:
                        EntityDisplay();
                        break;
                    case Mode.Opo:
                        OpoDisplay();
                        break;
                    case Mode.Comparison:
                        ComparisonDisplay();
                        break;
                    case Mode.Player:
                        PlayerDisplay();
                        break;
                }
            }

            private void OpoDisplay()
            {
                var entries = new List<KeyValuePair<string, int>>();
                int total = 0;

                var opos = OnlineManager.recentEntities.Values.OfType<OnlinePhysicalObject>().ToList();

                total = opos.Count;

                entries = opos.Select(x => x.apo).GroupBy(x => x.type.value).ToDictionary(x => x.Key, x => x.Count()).ToList();

                entries = entries.OrderBy(x => -x.Value).ToList();

                UpdateLabels(entries.Count);

                int index = 0;
                foreach (var entry in entries)
                {
                    labels[index].text = $"[{index}] {entry.Key} - {String.Format("{0:P2}", (float)entry.Value / total)} - {entry.Value}";
                    labels[index].color = ColorFromString(entry.Key);

                    labels[index].x = pos.x - 80;
                    labels[index].y = pos.y - 100 - (index * 15);

                    index++;
                }

                mainLabel.text = $"Total OnlinePhysicalObjects - {total}";

                int stepsTaken = 0;
                foreach (var entry in entries)
                {
                    float percentage = (float)entry.Value / total;

                    var steps = (int)Mathf.Floor(percentage * 360f);
                    for (int i = 0; i < steps; i++)
                    {
                        if (stepsTaken + i < sprites.Length)
                        {
                            sprites[stepsTaken + i].color = ColorFromString(entry.Key);
                        }
                        else
                        {
                            break;
                        }
                    }
                    stepsTaken += steps;
                }
            }

            private void EntityDisplay()
            {
                var entries = new List<KeyValuePair<Type, int>>();
                int total = 0;

                entries = OnlineManager.recentEntities.Values.GroupBy(x => x.GetType()).ToDictionary(x => x.Key, x => x.Count()).ToList();
                total = OnlineManager.recentEntities.Values.Count();

                entries = entries.OrderBy(x => -x.Value).ToList();

                UpdateLabels(entries.Count);

                int index = 0;
                foreach (var entry in entries)
                {
                    labels[index].text = $"[{index}] {entry.Key.Name} - {String.Format("{0:P2}", (float)entry.Value / total)} - {entry.Value}";
                    labels[index].color = ColorFromType(entry.Key);

                    labels[index].x = pos.x - 80;
                    labels[index].y = pos.y - 100 - (index * 15);

                    index++;
                }
                mainLabel.text = $"Total Entities - {total}";

                int stepsTaken = 0;
                foreach (var entry in entries)
                {
                    float percentage = (float)entry.Value / total;

                    var steps = (int)Mathf.Floor(percentage * 360f);
                    for (int i = 0; i < steps; i++)
                    {
                        if (stepsTaken + i < sprites.Length)
                        {
                            sprites[stepsTaken + i].color = ColorFromType(entry.Key);
                        }
                        else
                        {
                            break;
                        }
                    }
                    stepsTaken += steps;
                }
            }

            private void StateDisplay()
            {
                var entries = StateProfiler.Instance.data.Values.OrderBy(x => -x.ticksSpent).ToArray();

                UpdateLabels(entries.Length);

                long total = 0;

                foreach(var entry in entries)
                {
                    total += entry.ticksSpent;
                }

                int index = 0;
                foreach (var entry in entries)
                {
                    labels[index].text = $"[{index}] {entry.type.Name} - {(float)(entry.ticksSpent / total) * 100}%";
                    labels[index].color = entry.color;

                    labels[index].x = pos.x - 80;
                    labels[index].y = pos.y - 100 - (index * 15);

                    index++;
                }

                mainLabel.text = $"Total Write Delay - {(float)(total / Stopwatch.Frequency)}";

                int stepsTaken = 0;
                foreach (var entry in entries)
                {
                    float percentage = (float)entry.ticksSpent / total;

                    var steps = (int)Mathf.Floor(percentage * 360f);
                    for (int i = 0; i < steps; i++)
                    {
                        if (stepsTaken + i < sprites.Length)
                        {
                            sprites[stepsTaken + i].color = entry.color;
                        }
                        else
                        {
                            break;
                        }
                    }
                    stepsTaken += steps;
                }
            }

            private void ComparisonDisplay()
            {
                var entries = new List<KeyValuePair<string, int>>();
                int total = 0;

                var failedComparisons = OnlineManager.recentFailedComparisons.ToList();

                total = failedComparisons.Count;
                entries = failedComparisons.Distinct().ToDictionary(x => x.DeclaringType.Name + "." + x.Name, x => failedComparisons.Count(y => y == x)).ToList();

                entries = entries.OrderBy(x => -x.Value).ToList();

                UpdateLabels(entries.Count);

                int index = 0;
                foreach (var entry in entries)
                {
                    labels[index].text = $"[{index}] {entry.Key} - {String.Format("{0:P2}", (float)entry.Value / total)} - {entry.Value}";
                    labels[index].color = ColorFromString(entry.Key);

                    labels[index].x = pos.x - 130;
                    labels[index].y = pos.y - 100 - (index * 15);

                    index++;
                }

                mainLabel.text = $"Total Failed Comparisons - {total}";

                int stepsTaken = 0;
                foreach (var entry in entries)
                {
                    float percentage = (float)entry.Value / total;

                    var steps = (int)Mathf.Floor(percentage * 360f);
                    for (int i = 0; i < steps; i++)
                    {
                        if (stepsTaken + i < sprites.Length)
                        {
                            sprites[stepsTaken + i].color = ColorFromString(entry.Key);
                        }
                        else
                        {
                            break;
                        }
                    }
                    stepsTaken += steps;
                }
            }

            private void PlayerDisplay()
            {
                var entries = new List<KeyValuePair<OnlinePlayer, int>>();
                int total = 0;
                var players = OnlineManager.players;

                entries = players.ToDictionary(x => x, x =>
                {
                    int value = 0;
                    foreach (var bytes in x.bytesIn)
                    {
                        value += bytes;
                    }
                    total += value;
                    return value;
                }).ToList();

                entries = entries.OrderBy(x => -x.Value).ToList();

                UpdateLabels(entries.Count);

                int index = 0;
                foreach (var entry in entries)
                {
                    labels[index].text = $"[{index}] {entry.Key.id.name} - {String.Format("{0:P2}", (float)entry.Value / total)} - {entry.Value}";
                    labels[index].color = ColorFromString(entry.Key.id.name);

                    labels[index].x = pos.x - 130;
                    labels[index].y = pos.y - 100 - (index * 15);

                    index++;
                }

                mainLabel.text = $"Player Bandwidth - {total}";

                int stepsTaken = 0;
                foreach (var entry in entries)
                {
                    float percentage = (float)entry.Value / total;

                    var steps = (int)Mathf.Floor(percentage * 360f);
                    for (int i = 0; i < steps; i++)
                    {
                        if (stepsTaken + i < sprites.Length)
                        {
                            sprites[stepsTaken + i].color = ColorFromString(entry.Key.id.name);
                        }
                        else
                        {
                            break;
                        }
                    }
                    stepsTaken += steps;
                }
            }

            public void UpdateLabels(int length)
            {
                if (length > 0)
                {
                    while (labels.Count != length)
                    {
                        if (labels.Count > length)
                        {
                            labels.Last().RemoveFromContainer();
                            labels.RemoveAt(labels.Count - 1);
                        }
                        if (labels.Count < length)
                        {
                            var label = new FLabel(Custom.GetFont(), "[]")
                            {
                                alignment = FLabelAlignment.Left
                            };
                            container.AddChild(label);
                            labels.Add(label);
                        }
                    }
                }

            }

            public void ClearChart()
            {
                foreach (var sprite in sprites)
                {
                    sprite.color = Color.gray;
                }
                foreach (var label in labels)
                {
                    label.RemoveFromContainer();
                }
                labels.Clear();
            }

            public void RemoveSprites()
            {
                mainLabel.RemoveFromContainer();
                foreach(var sprite in sprites)
                {
                    sprite.RemoveFromContainer();
                }
                foreach(var label in labels)
                {
                    label.RemoveFromContainer();
                }
            }

            public Color ColorFromType(Type type)
            {
                return ColorFromString(type.Name);
            }

            public Color ColorFromString(string s)
            {
                uint fnvOffset = 2166136261;
                uint fnvPrime = 16777619;

                uint hash = fnvOffset;

                for(int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= fnvPrime;
                }

                return Color.HSVToRGB(hash / (float)uint.MaxValue, 0.8f, 0.8f);
            }
        }

        private static List<ProfilerGraph> profilerGraphs = new List<ProfilerGraph>();
        private static StateChart chart;
        private static bool keyDown;
        private static bool minusDown;

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
            if (self.devToolsActive && Input.GetKey(KeyCode.Minus) && MeadowProfiler.patched && !minusDown)
            {
                if (MeadowProfiler.Instance is not null)
                {
                    MeadowProfiler.Instance.Update();
                    FlameGraph.OutputFlameGraph();
                }
            }
            minusDown = self.devToolsActive && Input.GetKey(KeyCode.Minus);

            Vector2 screenSize = self.rainWorld.options.ScreenSize;

            framerate = (int)(1.0f / dt);

            foreach(var graph in profilerGraphs)
            {
                graph.Update();
            }

            chart?.Update();
        }

        public static void CreateOverlay(RainWorldGame self)
        {
            if (MeadowProfiler.Instance is null)
            {
                MeadowProfiler.Instance = new();
            }

            Vector2 screenSize = self.rainWorld.options.ScreenSize;
            overlayContainer = new FContainer();

            if (MeadowProfiler.patched)
            {
                overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Advanced Profiling Enabled, press '-' to generate a collapsed stack.")
                {
                    x = screenSize.x / 2f + 0.01f,
                    y = 740.01f,
                    color = new Color(1f, 1f, 0.5f)
                });
            }

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

            // Read Bandwidth
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Total Read")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 410,
            });

            profilerGraphs.Add(new ProfilerGraph(overlayContainer, new Vector2(5.01f, screenSize.y - 520), 240, 80, () =>
            {
                int value = totalRead / 1000;
                if (profilerGraphs[2].max < value) profilerGraphs[2].max = value;
                return value;
            }, 0f, 1000f, true, false, ProfilerGraph.Format.KBPS));

            // Write Bandwidth
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Total Written")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 610,
            });

            profilerGraphs.Add(new ProfilerGraph(overlayContainer, new Vector2(5.01f, screenSize.y - 720), 240, 80, () =>
            {
                int value = totalWritten / 1000;
                if (profilerGraphs[3].max < value) profilerGraphs[3].max = value;
                return value;
            }, 0f, 1000f, true, false, ProfilerGraph.Format.KBPS));

            chart = new(overlayContainer, new Vector2(screenSize.x - 120, screenSize.y - 120));


            Futile.stage.AddChild(overlayContainer);
        }

        public static void RemoveOverlay(RainWorldGame self)
        {
            profilerGraphs.Clear();
            overlayContainer?.RemoveFromContainer();
            overlayContainer = null;
            chart?.RemoveSprites();
            chart = null;
            MeadowProfiler.Instance?.Destroy();
        }
    }
}
