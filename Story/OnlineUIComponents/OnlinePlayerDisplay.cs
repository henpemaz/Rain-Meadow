using RWCustom;
using System.Linq;
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
        public int resetUsernameCounter;
        public float alpha;
        public float lastAlpha;
        public float blink;
        public float lastBlink;
        public bool switchedToDeathIcon;
        public int onlineTimeSinceSpawn;
        public string iconString;

        SlugcatCustomization customization;

        public OnlinePlayerDisplay(PlayerSpecificOnlineHud owner, SlugcatCustomization customization) : base(owner)
        {

            this.owner = owner;
            this.resetUsernameCounter = 200;

            this.pos = new Vector2(-1000f, -1000f);
            this.lastPos = this.pos;
            this.gradient = new FSprite("Futile_White", true);
            this.gradient.shader = owner.hud.rainWorld.Shaders["FlatLight"];
            this.gradient.color = new Color(0f, 0f, 0f);
            owner.hud.fContainers[0].AddChild(this.gradient);
            this.gradient.alpha = 0f;
            this.gradient.x = -1000f;
            this.label = new FLabel(Custom.GetFont(), customization.nickname);
            this.label.color = Color.white;


            owner.hud.fContainers[0].AddChild(this.label);
            this.label.alpha = 0f;
            this.label.x = -1000f;
            this.arrowSprite = new FSprite("Multiplayer_Arrow", true);
            owner.hud.fContainers[0].AddChild(this.arrowSprite);
            this.arrowSprite.alpha = 0f;
            this.arrowSprite.x = -1000f;
            this.arrowSprite.color = Color.white;

            if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                this.iconString = "ChieftainA";
            }
            else
            {
                this.iconString = "Kill_Slugcat";

            }
            this.slugIcon = new FSprite(iconString, true);
            owner.hud.fContainers[0].AddChild(this.slugIcon);
            this.slugIcon.alpha = 0f;
            this.slugIcon.x = -1000f;
            this.slugIcon.color = Color.white;

            this.blink = 1f;
            this.switchedToDeathIcon = false;

            this.label.color = customization.SlugcatColor();
            this.arrowSprite.color = customization.SlugcatColor();
            this.slugIcon.color = customization.SlugcatColor();
            this.customization = customization;
        }

        public override void Update()
        {
            base.Update();
            onlineTimeSinceSpawn++;

            bool show = RainMeadow.rainMeadowOptions.ShowFriends.Value || (owner.clientSettings.isMine && onlineTimeSinceSpawn < 120);
            if (show || this.alpha > 0)
            {
                this.lastAlpha = this.alpha;
                this.blink = 1f;
                this.alpha = Custom.LerpAndTick(this.alpha, owner.needed && show ? 1 : 0, 0.08f, 0.033333335f);

                if (owner.found)
                {
                    this.pos = owner.drawpos;
                    if (owner.pointDir == Vector2.down) pos += new Vector2(0f, 45f);

                    if (this.lastAlpha == 0) this.lastPos = pos;

                    if (owner.PlayerConsideredDead) this.alpha = Mathf.Min(this.alpha, 0.5f);

                    if (owner.PlayerConsideredDead != switchedToDeathIcon)
                    {
                        slugIcon.RemoveFromContainer();
                        slugIcon = new FSprite(owner.PlayerConsideredDead ? "Multiplayer_Death" : iconString);
                        owner.hud.fContainers[0].AddChild(slugIcon);
                        switchedToDeathIcon = owner.PlayerConsideredDead;
                    }
                }
                else
                {
                    pos.x = -1000;
                }

                this.counter++;

            }
            if (!show) this.lastAlpha = this.alpha;
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

            color = customization.SlugcatColor();

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

            if (this.label.text != customization.nickname) // we've updated a username
            {
                resetUsernameCounter--;
                this.label.color = color * 3f;

            }

            if (resetUsernameCounter < 10) // snappier fadeaway
            {
                this.label.color = lighter_color;

            }


            if (resetUsernameCounter < 0)
            {

                this.label.text = customization.nickname;
                resetUsernameCounter = 200;

            }

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
