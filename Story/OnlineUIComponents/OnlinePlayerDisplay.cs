using RainMeadow.GameModes;
using RWCustom;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerDisplay : OnlinePlayerHudPart
    {
        public FSprite arrowSprite;
        public FSprite gradient;
        public FLabel label;
        public FSprite slugIcon;
        public int counter;
        public int fadeAwayCounter;
        public float alpha;
        public float lastAlpha;
        public float blink;
        public float lastBlink;
        public bool switchedToDeathIcon;
        private bool isButtonToggled;


        public OnlinePlayerDisplay(PlayerSpecificOnlineHud owner) : base(owner)
        {
            this.owner = owner;


            this.pos = new Vector2(-1000f, -1000f);
            this.lastPos = this.pos;
            this.gradient = new FSprite("Futile_White", true);
            this.gradient.shader = owner.hud.rainWorld.Shaders["FlatLight"];
            this.gradient.color = new Color(0f, 0f, 0f);
            owner.hud.fContainers[0].AddChild(this.gradient);
            this.gradient.alpha = 0f;
            this.gradient.x = -1000f;
            this.label = new FLabel(Custom.GetFont(), owner.clientSettings.owner.id.name);
            this.label.color = Color.white;



            owner.hud.fContainers[0].AddChild(this.label);
            this.label.alpha = 0f;
            this.label.x = -1000f;
            this.arrowSprite = new FSprite("Multiplayer_Arrow", true);
            owner.hud.fContainers[0].AddChild(this.arrowSprite);
            this.arrowSprite.alpha = 0f;
            this.arrowSprite.x = -1000f;
            this.arrowSprite.color = Color.white;
            this.slugIcon = new FSprite("Kill_Slugcat", true);
            owner.hud.fContainers[0].AddChild(this.slugIcon);
            this.slugIcon.alpha = 0f;
            this.slugIcon.x = -1000f;
                        this.slugIcon.color = Color.white;

            this.blink = 1f;
            this.switchedToDeathIcon = false;

            this.isButtonToggled = false;

            if (RainMeadow.isStoryMode(out var _))
            {
                this.label.color = (owner.clientSettings as StoryClientSettings).SlugcatColor(); ;
                this.arrowSprite.color = (owner.clientSettings as StoryClientSettings).SlugcatColor(); ;
                this.slugIcon.color = (owner.clientSettings as StoryClientSettings).SlugcatColor(); ;


            }

            if (RainMeadow.isArenaMode(out var _))
            {
                this.label.color = (owner.clientSettings as ArenaClientSettings).SlugcatColor();

                this.arrowSprite.color = (owner.clientSettings as ArenaClientSettings).SlugcatColor();
                this.slugIcon.color = (owner.clientSettings as ArenaClientSettings).SlugcatColor();


            }
        }

        public override void Update()
        {
            base.Update();

            if (RainMeadow.rainMeadowOptions.FriendViewClickToActivate.Value && Input.GetKeyDown(RainMeadow.rainMeadowOptions.FriendsListKey.Value))
            {
                this.isButtonToggled = !this.isButtonToggled;
            }

            if (isButtonToggled || (!RainMeadow.rainMeadowOptions.FriendViewClickToActivate.Value && Input.GetKey(RainMeadow.rainMeadowOptions.FriendsListKey.Value)))
            {
                this.lastAlpha = this.alpha;
                this.blink = 1f;
                if (owner.found)
                {
                    this.alpha = Custom.LerpAndTick(this.alpha, owner.needed ? 1 : 0, 0.08f, 0.033333335f);

                    this.pos = owner.drawpos;
                    if (owner.pointDir == Vector2.down) pos += new Vector2(0f, 45f);

                    if (owner.PlayerConsideredDead)
                    {
                        this.alpha = 0.5f;
                        if (!switchedToDeathIcon)
                        {
                            slugIcon.RemoveFromContainer();
                            slugIcon = new FSprite("Multiplayer_Death");
                            owner.hud.fContainers[0].AddChild(slugIcon);
                            switchedToDeathIcon = true;
                        }
                    }
                }
                else
                {
                    pos.x = -1000;
                }

                this.counter++;
            }
            else
            {
                this.alpha = Custom.LerpAndTick(this.alpha, owner.needed ? 0 : 1, 0.08f, 0.0033333335f);
                this.lastAlpha = this.alpha;

            }

        }

        public override void Draw(float timeStacker)
        {

            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker) + new Vector2(0.01f, 0.01f);
            float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker)), 0.7f);
            this.gradient.x = vector.x;
            this.gradient.y = vector.y + 10f;
            this.gradient.scale = Mathf.Lerp(80f, 110f, num) / 16f;
            this.gradient.alpha = 0.17f * Mathf.Pow(num, 2f);
            this.arrowSprite.x = vector.x;
            this.arrowSprite.y = vector.y;
            this.arrowSprite.rotation = RWCustom.Custom.VecToDeg(owner.pointDir * -1);

            this.slugIcon.x = vector.x;
            this.slugIcon.y = vector.y + 40f;

            this.label.x = vector.x;
            this.label.y = vector.y + 20f;
            Color color = Color.white;

            if (RainMeadow.isStoryMode(out var _))
            {
                color = (owner.clientSettings as StoryClientSettings).SlugcatColor();

            }
            if (RainMeadow.isArenaMode(out var _))
            {
                color = (owner.clientSettings as ArenaClientSettings).SlugcatColor();
            }
            if (this.counter % 6 < 2 && this.lastBlink > 0f)
            {
                if (((Vector3)(Vector4)color).magnitude > 1.56f)
                {
                    color = Color.Lerp(color, new Color(0.9f, 0.9f, 0.9f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
                else
                {
                    color = Color.Lerp(color, new Color(1f, 1f, 1f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(this.lastBlink, this.blink, timeStacker)));
                }
            }
            var lighter_color = color * 1.7f;

            this.label.color = lighter_color;
            this.arrowSprite.color = lighter_color;
            this.slugIcon.color = lighter_color;

            this.label.alpha = num;
            this.arrowSprite.alpha = num;
            this.slugIcon.alpha = num;
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            this.gradient.RemoveFromContainer();
            this.arrowSprite.RemoveFromContainer();
            this.label.RemoveFromContainer();
            this.slugIcon.RemoveFromContainer();
        }
    }
}
