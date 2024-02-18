using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RainMeadow
{
    public partial class OnlinePlayerSpecificHud
    {
        public partial class JollyOffRoom : JollyPointer
        {
            public List<FSprite> sprites;

            public Vector2 localPos;

            public StoryAvatarSettings personaSettings;

            public Vector2 playerPos;

            public Vector2 drawPos;

            public Vector2 lastDrawPos;

            public Vector2 roomPos;

            public Color drawcolor;

            public float scale;

            public float screenSizeX;

            public float screenSizeY;

            public Vector2 middleScreen;

            public Vector2 rectangleSize;

            public float diagScale;

            public float uAlpha;

            public JollyOffRoom(OnlinePlayerSpecificHud jollyHud)
                : base(jollyHud)
            {

                hidden = true;
                sprites = new List<FSprite>();
                timer = 0;
                scale = 1.25f;
                InitiateSprites();
                screenSizeX = jollyHud.hud.rainWorld.options.ScreenSize.x;
                screenSizeY = jollyHud.hud.rainWorld.options.ScreenSize.y;
                middleScreen = new Vector2(screenSizeX / 2f, screenSizeY / 2f);
                rectangleSize = new Vector2(screenSizeX - (float)(2 * screenEdge), screenSizeY - (float)(2 * screenEdge));
                diagScale = Mathf.Abs(Vector2.Distance(Vector2.zero, middleScreen));
            }

            public void InitiateSprites()
            {
                this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;

                sprites.Add(new FSprite("GuidanceSlugcat")
                {
                    shader = jollyHud.hud.rainWorld.Shaders["Hologram"],
                    scale = scale
                });
                sprites.Add(new FSprite("Futile_White")
                {
                    shader = jollyHud.hud.rainWorld.Shaders["FlatLight"],
                    alpha = 0f,
                    x = -1000f
                });
                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].color = personaSettings.bodyColor;
                    sprites[i].alpha = 0.1f;
                    jollyHud.fContainer.AddChild(sprites[i]);
                }
            }

            public override void Update()
            {
                base.Update();
                if (base.PlayerState.permaDead)
                {
                    slatedForDeletion = true;
                    return;
                }

                // This is for the golden slugcat symbol (ie. missing slugcat)
                var players = OnlineManager.lobby.playerAvatars
                                 .Where(avatar => avatar.type != (byte)OnlineEntity.EntityId.IdType.none)
                                 .Select(avatar => avatar.FindEntity(true))
                                 .OfType<OnlinePhysicalObject>()
                                 .Select(opo => opo.apo)
                                 .OfType<AbstractCreature>()
                                 .ToList();

                for (int i = 0; i < players.Count; i++)

                {
                    if (players[i].realizedCreature != null && (!OnlineManager.players[i].isMe))
                    {
                        playerPos = players[i].world.RoomToWorldPos(players[i].realizedCreature.mainBodyChunk.pos, players[i].Room.index);
                        roomPos = players[i].world.RoomToWorldPos(jollyHud.camPos, jollyHud.Camera.room.abstractRoom.index);
                    }

                    lastDrawPos = drawPos;
                    drawPos = playerPos - roomPos;
                    float num = Mathf.Abs(Vector2.Distance(drawPos, middleScreen));
                    scale = Mathf.Lerp(0.65f, 1.65f, Mathf.Pow(diagScale / num, 1.2f));
                    float num2 = middleScreen.x - rectangleSize.x / 2f;
                    float num3 = middleScreen.x + rectangleSize.x / 2f;
                    float num4 = middleScreen.y - rectangleSize.y / 2f;
                    float num5 = middleScreen.y + rectangleSize.y / 2f;
                    if (num2 < drawPos.x && drawPos.x < num3 && num4 < drawPos.y && drawPos.y < num5)
                    {
                        float b = Mathf.Abs(drawPos.x - num2);
                        float num6 = Mathf.Abs(drawPos.x - num3);
                        float num7 = Mathf.Abs(drawPos.y - num4);
                        float d = Mathf.Abs(drawPos.y - num5);
                        float smallestNumber = GetSmallestNumber(num7, b, num6, d);
                        if (AreClose(smallestNumber, b))
                        {
                            drawPos.x = num2;
                        }
                        else if (AreClose(smallestNumber, num6))
                        {
                            drawPos.x = num3;
                        }
                        else if (AreClose(smallestNumber, num7))
                        {
                            drawPos.y = num4;
                        }
                        else
                        {
                            drawPos.y = num5;
                        }
                    }

                    drawPos.x = Mathf.Clamp(drawPos.x, screenEdge, screenSizeX - (float)screenEdge);
                    drawPos.y = Mathf.Clamp(drawPos.y, screenEdge, screenSizeY - (float)screenEdge);
                    if (jollyHud.PlayerRoomBeingViewed || jollyHud.inShortcut || !knownPos || forceHide)
                    {
                        hidden = true;
                    }
                    else if (hidden)
                    {
                        hidden = false;
                        lastDrawPos = drawPos;
                    }

                    alpha = ((!hidden) ? 0.85f : 0f);
                }
            }

            public override void Draw(float timeStacker)
            {
                base.Draw(timeStacker);
                this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;

                if (hidden)
                {
                    sprites[0].isVisible = false;
                    sprites[1].isVisible = false;
                    return;
                }

                if (UnityEngine.Random.value > Mathf.InverseLerp(0.5f, 0.75f, alpha))
                {
                    for (int i = 0; i < sprites.Count; i++)
                    {
                        sprites[i].isVisible = false;
                    }

                    return;
                }

                uAlpha = Mathf.SmoothStep(lastAlpha, alpha, timeStacker);
                Vector2 vector = Vector2.Lerp(drawPos, lastDrawPos, timeStacker);
                for (int j = 0; j < sprites.Count; j++)
                {
                    sprites[j].isVisible = true;
                    sprites[j].x = vector.x;
                    sprites[j].y = vector.y;
                    sprites[j].scale = scale;
                    sprites[j].color = personaSettings.bodyColor;
                }

                sprites[1].scale = Mathf.Lerp(80f, 110f, 1f) / 16f;
                sprites[0].alpha = Mathf.Lerp(sprites[0].alpha, uAlpha, timeStacker * 0.5f);
                sprites[1].alpha = Mathf.Lerp(sprites[1].alpha, 0.15f * Mathf.Pow(uAlpha, 2f), timeStacker);
            }

            public float GetSmallestNumber(float a, float b, float c, float d)
            {
                return Mathf.Min(a, Mathf.Min(b, Mathf.Min(c, d)));
            }

            public bool AreClose(float a, float b)
            {
                return (double)Mathf.Abs(a - b) <= 0.01;
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                RainMeadow.Debug("JollyOfscreen: Clearing sprites");
                for (int i = 0; i < sprites.Count; i++)
                {
                    sprites[i].RemoveFromContainer();
                }
            }
        }
    }
}
