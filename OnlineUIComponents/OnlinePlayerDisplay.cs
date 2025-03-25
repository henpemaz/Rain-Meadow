using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class OnlinePlayerDisplay : OnlinePlayerHudPart
    {
        public FSprite arrowSprite;
        public FSprite gradient;
        public FLabel username;
        public List<FLabel> messageLabels = new();
        public FSprite slugIcon;
        public OnlinePlayer player;
        public class Message
        {
            public int timer;
            public string text;

            public Message(string message, int timer = 200)
            {
                this.timer = timer;
                this.text = message;
            }
        }

        public Queue<Message> messageQueue = new();

        public Color color;
        public Color lighter_color;

        public float H;
        public float S;
        public float V;

        public int counter;
        public float alpha;
        public float lastAlpha;
        public float blink;
        public float lastBlink;
        public int onlineTimeSinceSpawn;
        public string iconString;
        public bool flashIcons;

        public float fadeSpeed;

        SlugcatCustomization customization;


        public OnlinePlayerDisplay(PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player) : base(owner)
        {
            this.player = player;
            this.owner = owner;

            this.color = customization.SlugcatColor();

            Color.RGBToHSV(color, out H, out S, out V);

            if (V < 0.8f)
            {
                this.lighter_color = Color.HSVToRGB(H, S, 0.8f);
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

            for (int i = 0; i < 3; i++)
            {
                var label = new FLabel(Custom.GetFont(), "");
                this.messageLabels.Add(label);
                owner.hud.fContainers[0].AddChild(label);
                label.alignment = FLabelAlignment.Center;
                label.color = Color.white;
                label.alpha = 0f;
                label.x = -1000f;
            }

            if (RainMeadow.isArenaMode(out var arena) && arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
            {
                this.iconString = "Multiplayer_Star";
            }

            else if (owner.clientSettings.owner == OnlineManager.lobby.owner)
            {
                this.iconString = "ChieftainA";
            }
            
            //if (arena.onlineArenaGameMode.AddCustomIcon(arena, owner) != "")
            //{
            //    slugIcon.SetElementByName(arena.onlineArenaGameMode.AddCustomIcon(arena, owner));
            //}

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
                    else if (owner.PlayerInAncientShelter) slugIcon.SetElementByName("ShortcutAShelter");
                    else if (owner.PlayerInShelter) slugIcon.SetElementByName("ShortcutShelter");
                    else if (owner.PlayerInGate) slugIcon.SetElementByName("ShortcutGate");
                    else if (owner.PlayerConsideredDead) slugIcon.SetElementByName("Multiplayer_Death");
                    else if (RainMeadow.isArenaMode(out var arena) && arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
                    {
                        slugIcon.SetElementByName("Multiplayer_Star");
                        slugIcon.color = Color.yellow;
                    }
                    else slugIcon.SetElementByName(iconString);

                    if (flashIcons) this.alpha = Mathf.Lerp(lighter_color.a, 0f, (Mathf.Cos(owner.owner.hudCounter / fadeSpeed) + 1f) / 2f);
                    else if (RainMeadow.rainMeadowOptions.ShowFriends.Value) this.alpha = lighter_color.a;
                }
                else
                {
                    pos.x = -1000;
                }

                this.counter++;
            }
            if (!show) this.lastAlpha = this.alpha;

            for (int i = 0; i < messageQueue.Count;)
            {
                messageQueue.ElementAt(i).timer--;
                if (messageQueue.ElementAt(i).timer <= 0)
                {
                    messageQueue.Dequeue();
                    // this is a queue so if we dequeue it shifts the entire list
                }
                else i++;
            }
        }

        public override void Draw(float timeStacker)
        {
            Vector2 vector = Vector2.Lerp(this.lastPos, this.pos, timeStacker) + new Vector2(0.01f, 0.01f);
            var pos = vector;
            float num = Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker)), 0.7f);

            this.arrowSprite.x = pos.x;
            this.arrowSprite.y = pos.y;
            this.arrowSprite.rotation = RWCustom.Custom.VecToDeg(owner.pointDir * -1);

            this.gradient.x = pos.x;
            this.gradient.y = pos.y + 10f;
            this.gradient.scale = Mathf.Lerp(80f, 110f, num) / 16f;
            this.gradient.alpha = 0.17f * Mathf.Pow(num, 2f);

            pos.y += 20f;
            this.username.x = pos.x;
            this.username.y = pos.y;

            if (messageQueue.Count > 0)
            {
                this.username.text = customization.nickname + ": ";

                while (messageQueue.Count > messageLabels.Count) messageQueue.Dequeue();
                bool first = true;
                for (int i = messageQueue.Count - 1; i >= 0; i--)
                {
                    var messageData = messageQueue.ElementAt(i);
                    messageLabels[i].text = messageData.text;
                    messageLabels[i].alpha = Mathf.Min(messageData.timer / 10f, 1f);
                    if (first)
                    {
                        first = false;
                        this.username.x = pos.x - (messageLabels[i]._textRect.width / 2);
                        this.messageLabels[i].x = pos.x + (username._textRect.width / 2);
                    }
                    else
                    {
                        this.messageLabels[i].x = pos.x;
                    }
                    messageLabels[i].y = pos.y;
                    pos.y += 20;
                }
            }
            else
            {
                this.username.text = customization.nickname;
                pos.y += 20;
            }

            for (int i = messageQueue.Count; i < messageLabels.Count; i++)
            {
                messageLabels[i].alpha = 0f;
                messageLabels[i].text = "";
            }

            this.slugIcon.x = pos.x;
            this.slugIcon.y = pos.y;

            this.arrowSprite.alpha = num;
            this.slugIcon.alpha = num;
            if (this.messageQueue.Count > 0 && (flashIcons || RainMeadow.rainMeadowOptions.ShowFriends.Value))
            {
                this.username.alpha = lighter_color.a;
            }
            else
            {
                foreach (var label in messageLabels) label.alpha = num;
                this.username.alpha = num;
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
        }

        public override void ClearSprites()
        {
            base.ClearSprites();
            this.gradient.RemoveFromContainer();
            this.arrowSprite.RemoveFromContainer();
            this.username.RemoveFromContainer();
            foreach (var label in this.messageLabels) label.RemoveFromContainer();
            this.slugIcon.RemoveFromContainer();
        }
    }
}