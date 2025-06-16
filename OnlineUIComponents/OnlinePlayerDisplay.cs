using RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.PlayerLoop;
using static RainMeadow.Arena.ArenaOnlineGameModes.TeamBattle.TeamBattleMode;

namespace RainMeadow
{
    public class OnlinePlayerDisplay : OnlinePlayerHudPart
    {
        public FSprite arrowSprite;
        public FSprite gradient;
        public FLabel username;
        public List<FLabel> messageLabels = new();
        public FLabel pingLabel;
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
        public int realPing;
        public bool showPing;

        SlugcatCustomization customization;


        public OnlinePlayerDisplay(PlayerSpecificOnlineHud owner, SlugcatCustomization customization, OnlinePlayer player) : base(owner)
        {
            this.player = player;
            this.owner = owner;

            this.color = customization.SlugcatColor();

            Color.RGBToHSV(color, out H, out S, out V);


            // FIX THIS
            if (V < 0.8f)
            {
                this.lighter_color = Color.HSVToRGB(H, S, 0.8f);
            }
            else
            {
                this.lighter_color = color;
            }

            if (RainMeadow.isArenaMode(out var a))
            {
                if (isTeamBattleMode(a, out var tb))
                {

                    if (OnlineManager.lobby.clientSettings[owner.clientSettings.owner].TryGetData<ArenaTeamClientSettings>(out var tb2))
                    {
                        this.color = tb.TeamColors[tb2.team];
                        this.lighter_color = this.color;
                    }
                }
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
            else if (arena.externalArenaGameMode.AddCustomIcon(arena, owner) != "")
            {
                this.iconString = arena.externalArenaGameMode.AddCustomIcon(arena, owner);
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

            showPing = false;
            this.realPing = System.Math.Max(1, player.ping - 16);
            this.pingLabel = new FLabel(Custom.GetFont(), $"({realPing}ms)"); //{System.Math.Max(1, player.ping - 16)})
            owner.hud.fContainers[0].AddChild(this.pingLabel);
            this.pingLabel.color = lighter_color;
            this.pingLabel.alpha = showPing ? 1 : 0;


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
            this.realPing = System.Math.Max(1, player.ping - 16);
            onlineTimeSinceSpawn++;

            this.flashIcons = (RainMeadow.rainMeadowOptions.ShowFriends.Value || RainMeadow.rainMeadowOptions.ReadyToContinueToggle.Value) && (owner.PlayerInGate || owner.PlayerInShelter);

            bool show = RainMeadow.rainMeadowOptions.ShowFriends.Value || (owner.clientSettings.isMine && onlineTimeSinceSpawn < 120);
            if (RainMeadow.isArenaMode(out var _) && owner.RealizedPlayer != null && owner.RealizedPlayer.isCamo && !player.isMe)
            {
                show = false; // Don't show if it's arena mode and the player is camo
                pos.x = -1000;
                this.alpha = 0f;
            }

            if (show || this.alpha > 0 || flashIcons)
            {
                this.lastAlpha = this.alpha;
                this.blink = 1f;
                this.alpha = Custom.LerpAndTick(this.alpha, owner.needed && show ? 1 : 0, 0.08f, 0.033333335f);

                if (owner.found)
                {
                    if (RainMeadow.rainMeadowOptions.ShowPing.Value && !player.isMe)//
                    {
                        this.pingLabel.alpha = Custom.LerpAndTick(this.alpha, owner.needed && show ? 1 : 0, 0.08f, 0.033333335f);
                    }

                    this.pos = owner.drawpos;
                    if (owner.pointDir == Vector2.down) pos += new Vector2(0f, 45f);

                    if (this.lastAlpha == 0) this.lastPos = pos;

                    if (owner.PlayerConsideredDead) this.alpha = Mathf.Min(this.alpha, 0.5f);

                    if (onlineTimeSinceSpawn < 135 && owner.clientSettings.isMine) slugIcon.SetElementByName("Kill_Slugcat");
                    else if (RainMeadow.isArenaMode(out var arena))
                    {
                        if (arena.reigningChamps != null && arena.reigningChamps.list != null && arena.reigningChamps.list.Contains(player.id))
                        {
                            slugIcon.SetElementByName("Multiplayer_Star");
                            slugIcon.color = owner.PlayerConsideredDead ? lighter_color : Color.yellow;
                        }
                        else if (arena.externalArenaGameMode.AddCustomIcon(arena, owner) != "")
                        {
                            slugIcon.SetElementByName(arena.externalArenaGameMode.AddCustomIcon(arena, owner));
                        }

                        if (TeamBattleMode.isTeamBattleMode(arena, out _)  && owner.PlayerConsideredDead) slugIcon.color = Color.gray;

                        else if (owner.PlayerConsideredDead) slugIcon.SetElementByName("Multiplayer_Death");
                    }
                    else if (owner.PlayerInAncientShelter) slugIcon.SetElementByName("ShortcutAShelter");
                    else if (owner.PlayerInShelter) slugIcon.SetElementByName("ShortcutShelter");
                    else if (owner.PlayerInGate) slugIcon.SetElementByName("ShortcutGate");
                    else if (owner.PlayerConsideredDead) slugIcon.SetElementByName("Multiplayer_Death");

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
            this.pingLabel.text = $"({realPing}ms)";

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

            if (this.realPing <= 100)
            {
                pingLabel.color = Color.green;
            }


            if (this.realPing > 100)
            {
                pingLabel.color = Color.yellow;
            }

            if (this.realPing > 200)
            {
                pingLabel.color = Color.red;
            }

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
                        this.pingLabel.x = this.messageLabels[i].x + (this.messageLabels[i]._textRect.width / 2) + 20f; // Position after the first message
                        if (RainMeadow.rainMeadowOptions.ShowPingLocation.Value == 0)
                        {
                            this.pingLabel.y = username.y;
                        }
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
                if (RainMeadow.rainMeadowOptions.ShowPingLocation.Value == 0)
                {
                    this.pingLabel.x = pos.x + (this.username._textRect.width / 2) + 20f; // Position after the username
                    this.pingLabel.y = username.y;
                }
                pos.y += 20;
            }
            if (Input.GetKeyDown(KeyCode.P))
            {
                RainMeadow.rainMeadowOptions.ShowPingLocation.Value += 1;
            }
            if (RainMeadow.rainMeadowOptions.ShowPingLocation.Value == 1)
            {
                this.pingLabel.y = this.gradient.y - 25f;
                this.pingLabel.x = pos.x;
            }

            if (RainMeadow.rainMeadowOptions.ShowPingLocation.Value == 2)
            {
                this.pingLabel.alpha = 0;
            }
            if (RainMeadow.rainMeadowOptions.ShowPingLocation.Value > 2)
            {
                RainMeadow.rainMeadowOptions.ShowPingLocation.Value = 0;
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
