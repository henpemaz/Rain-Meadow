using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerIcon
    {
        public OnlineStoryHud storyHud;

        public int playerNumber;

        public FSprite gradient;

        public float baseGradScale;

        public float baseGradAlpha;

        public FSprite iconSprite;

        public Color color;

        public Vector2 pos;

        public Vector2 lastPos;

        public float blink;

        public int blinkRed;

        public bool dead;

        public float lastBlink;

        public StoryClientSettings clientSettings;

        public AbstractCreature player;

        public float rad;

        public PlayerState playerState => player.state as PlayerState;

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(lastPos, pos, timeStacker);
        }

        public void ClearSprites()
        {
            // gradient.RemoveFromContainer();
            iconSprite.RemoveFromContainer();
        }

        public OnlinePlayerIcon(OnlineStoryHud meter, AbstractCreature associatedPlayer, Color color)
        {
            player = associatedPlayer;
            this.storyHud = meter;
            lastPos = pos;
            // AddGradient(Mathf.Clamp01(color);
            iconSprite = new FSprite("Kill_Slugcat");
            this.color = color;
            this.storyHud.fContainer.AddChild(iconSprite);
            playerNumber = playerState?.playerNumber ?? 0;
            baseGradScale = 3.75f;
            baseGradAlpha = 0.45f;
        }

        public void AddGradient(Color color)
        {
            gradient = new FSprite("Futile_White");
            gradient.shader = storyHud.hud.rainWorld.Shaders["FlatLight"];
            gradient.color = color;
            gradient.scale = baseGradScale;
            gradient.alpha = baseGradAlpha;
            // meter.fContainer.AddChild(gradient);
        }

        public void Draw(float timeStacker)
        {
            float num = Mathf.Lerp(storyHud.lastFade, storyHud.fade, timeStacker);
            iconSprite.alpha = num;
            // gradient.alpha = Mathf.SmoothStep(0f, 1f, num) * baseGradAlpha;
            iconSprite.x = DrawPos(timeStacker).x;
            iconSprite.y = DrawPos(timeStacker).y + (float)(dead ? 7 : 0);
            // gradient.x = iconSprite.x;
            // gradient.y = iconSprite.y;
            if (storyHud.counter % 6 < 2 && lastBlink > 0f)
            {
                color = Color.Lerp(color, RWCustom.Custom.HSL2RGB(RWCustom.Custom.RGB2HSL(color).x, RWCustom.Custom.RGB2HSL(color).y, RWCustom.Custom.RGB2HSL(color).z + 0.2f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(lastBlink, blink, timeStacker)));
            }

            iconSprite.color = color;
        }

        public void Update()
        {
            this.clientSettings = (StoryClientSettings)OnlineManager.lobby.gameMode.clientSettings;

            blink = Mathf.Max(0f, blink - 0.05f);
            lastBlink = blink;
            lastPos = pos;
            color = clientSettings.bodyColor;
            rad = RWCustom.Custom.LerpAndTick(rad, RWCustom.Custom.LerpMap(storyHud.fade, 0f, 0.79f, 0.79f, 1f, 1.3f), 0.12f, 0.1f);
            if (blinkRed > 0)
            {
                blinkRed--;
                rad *= Mathf.SmoothStep(1.1f, 0.85f, (float)(storyHud.counter % 20) / 20f);
                color = Color.Lerp(color, Color.cyan, rad / 4f);
            }

            iconSprite.scale = rad;
            // gradient.scale = baseGradScale * rad;
            if (playerState.permaDead || playerState.dead)
            {
                color = Color.gray;
                if (!dead)
                {
                    iconSprite.RemoveFromContainer();
                    iconSprite = new FSprite("Multiplayer_Death");
                    iconSprite.scale *= 0.8f;
                    storyHud.fContainer.AddChild(iconSprite);
                    dead = true;
                    storyHud.customFade = 5f;
                    blink = 3f;
                    // gradient.color = Color.Lerp(Color.red, Color.black, 0.5f);
                }
            }
        }
    }
}