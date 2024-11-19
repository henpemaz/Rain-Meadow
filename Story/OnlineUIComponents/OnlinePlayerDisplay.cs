using RWCustom;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerDisplay : OnlinePlayerHudPart
    {
        public FSprite arrowSprite;
        public FSprite gradient;
        public FLabel username;
        public FLabel message;
        public FSprite slugIcon;

        public Color color;
        public Color lighter_color;

        public float H;
        public float S;
        public float V;

        public int counter;
        public int resetUsernameCounter;
        public float alpha;
        public float lastAlpha;
        public float blink;
        public float lastBlink;
        public int onlineTimeSinceSpawn;
        public string iconString;
        public bool flashIcons;

        public float fadeSpeed;
       
        SlugcatCustomization customization;

        public OnlinePlayerDisplay(PlayerSpecificOnlineHud owner, SlugcatCustomization customization) : base(owner)
        {
            this.owner = owner;
            this.resetUsernameCounter = 200;

            this.color = customization.SlugcatColor();

            Color.RGBToHSV(color, out H, out S,out V);

            if (V < 0.8f)
            {
                this.lighter_color = Color.HSVToRGB(H,S,0.8f);
            }
            else
            {
                this.lighter_color = color;
            }

            this.pos = new Vector2(-1000f, -1000f);
            this.lastPos = this.pos;
            this.gradient = new FSprite("Futile_White", true);
            owner.hud.fContainers[0].AddChild(this.gradient);
            this.gradient.shader = owner.hud.rainWorld.Shaders["FlatLight"];
            this.gradient.color = new Color(0f, 0f, 0f);
            this.gradient.alpha = 0f;
            this.gradient.x = -1000f;

            this.message = new FLabel(Custom.GetFont(), "");
            owner.hud.fContainers[0].AddChild(this.message);
            this.message.color = Color.white;
            this.message.alpha = 0f;
            this.message.x = -1000f;

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
            this.slugIcon.color = lighter_color;
            this.blink = 1f;

            this.username = new FLabel(Custom.GetFont(), customization.nickname);
            owner.hud.fContainers[0].AddChild(this.username);
            this.username.alpha = 0f;
            this.username.x = -1000f;
            this.username.color = lighter_color;

            this.arrowSprite = new FSprite("Multiplayer_Arrow", true);
            owner.hud.fContainers[0].AddChild(this.arrowSprite);
            this.arrowSprite.alpha = 0f;
            this.arrowSprite.x = -1000f;
            this.arrowSprite.color = lighter_color;

            this.customization = customization;

            this.fadeSpeed = 20f;
        }

        public override void Update()
        {
            base.Update();
            onlineTimeSinceSpawn++;

            this.flashIcons = (RainMeadow.rainMeadowOptions.ShowFriends.Value || RainMeadow.rainMeadowOptions.ReadyToContinueToggle.Value) && (owner.PlayerInGate || owner.PlayerInShelter);

            bool show = RainMeadow.rainMeadowOptions.ShowFriends.Value || (owner.clientSettings.isMine && onlineTimeSinceSpawn < 120);
            if (show || this.alpha > 0 || flashIcons)
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

                    if (onlineTimeSinceSpawn < 135 && owner.clientSettings.isMine) slugIcon.SetElementByName("Kill_Slugcat");
                    else if (owner.PlayerInShelter) slugIcon.SetElementByName("ShortcutShelter");
                    else if (owner.PlayerInGate) slugIcon.SetElementByName("ShortcutGate");
                    else if (owner.PlayerConsideredDead) slugIcon.SetElementByName("Multiplayer_Death");
                    else slugIcon.SetElementByName(iconString);

                    if (flashIcons) this.alpha = Mathf.Lerp(lighter_color.a, 0f, (Mathf.Cos(onlineTimeSinceSpawn / fadeSpeed) + 1f) / 2f);
                    else if (RainMeadow.rainMeadowOptions.ShowFriends.Value) this.alpha = lighter_color.a;
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

            this.username.x = vector.x;
            this.username.y = vector.y + 20f;

            this.message.x = vector.x + 20f;
            this.message.alignment = FLabelAlignment.Center;
            this.message.y = vector.y + 20f;

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

            if (this.message.text != "") // we've updated a username
            {
                this.username.text = customization.nickname + ":";
                resetUsernameCounter--;
                this.username.x = vector.x + this.message._textRect.x - 15f;
            }

            if (resetUsernameCounter < 0)
            {
                this.message.text = "";
                this.username.text = customization.nickname;
                resetUsernameCounter = 200;
            }



            this.arrowSprite.alpha = num;
            this.slugIcon.alpha = num;
            if (this.message.text != "" && (flashIcons || RainMeadow.rainMeadowOptions.ShowFriends.Value))
            {
                this.message.alpha = lighter_color.a;
                this.username.alpha = lighter_color.a;
            }
            else
            {
                this.message.alpha = num;
                this.username.alpha = num;
            }
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            this.gradient.RemoveFromContainer();
            this.arrowSprite.RemoveFromContainer();
            this.username.RemoveFromContainer();
            this.message.RemoveFromContainer();
            this.slugIcon.RemoveFromContainer();
        }
    }
}
