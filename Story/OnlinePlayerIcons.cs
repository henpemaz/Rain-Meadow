using HUD;
using System.Collections.Generic;
using System.Security.AccessControl;
using UnityEngine;
using static Sony.NP.ActivityFeed;
using UnityEngine.UI;
using RainMeadow;
using HarmonyLib;
using System.Linq;
using Newtonsoft.Json.Linq;

    public class OnlineStoryHud : HudPart
    {
        public class OnlinePlayerIcon
        {
            public OnlineStoryHud meter;

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

            public StoryAvatarSettings personaSettings;

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
                this.meter = meter;
                lastPos = pos;
                // AddGradient(Mathf.Clamp01(color);
                iconSprite = new FSprite("Kill_Slugcat");
                this.color = color;
                this.meter.fContainer.AddChild(iconSprite);
                playerNumber = playerState?.playerNumber ?? 0;
                baseGradScale = 3.75f;
                baseGradAlpha = 0.45f;
            }

            public void AddGradient(Color color)
            {
                gradient = new FSprite("Futile_White");
                gradient.shader = meter.hud.rainWorld.Shaders["FlatLight"];
                gradient.color = color;
                gradient.scale = baseGradScale;
                gradient.alpha = baseGradAlpha;
                // meter.fContainer.AddChild(gradient);
            }

            public void Draw(float timeStacker)
            {
                float num = Mathf.Lerp(meter.lastFade, meter.fade, timeStacker);
                iconSprite.alpha = num;
                // gradient.alpha = Mathf.SmoothStep(0f, 1f, num) * baseGradAlpha;
                iconSprite.x = DrawPos(timeStacker).x;
                iconSprite.y = DrawPos(timeStacker).y + (float)(dead ? 7 : 0);
                // gradient.x = iconSprite.x;
                // gradient.y = iconSprite.y;
                if (meter.counter % 6 < 2 && lastBlink > 0f)
                {
                    color = Color.Lerp(color, RWCustom.Custom.HSL2RGB(RWCustom.Custom.RGB2HSL(color).x, RWCustom.Custom.RGB2HSL(color).y, RWCustom.Custom.RGB2HSL(color).z + 0.2f), Mathf.InverseLerp(0f, 0.5f, Mathf.Lerp(lastBlink, blink, timeStacker)));
                }

                iconSprite.color = color;
            }

            public void Update()
            {
                this.personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;

                blink = Mathf.Max(0f, blink - 0.05f);
                lastBlink = blink;
                lastPos = pos;
                color = personaSettings.bodyColor;
                rad = RWCustom.Custom.LerpAndTick(rad, RWCustom.Custom.LerpMap(meter.fade, 0f, 0.79f, 0.79f, 1f, 1.3f), 0.12f, 0.1f);
                if (blinkRed > 0)
                {
                    blinkRed--;
                    rad *= Mathf.SmoothStep(1.1f, 0.85f, (float)(meter.counter % 20) / 20f);
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
                        meter.fContainer.AddChild(iconSprite);
                        dead = true;
                        meter.customFade = 5f;
                        blink = 3f;
                        // gradient.color = Color.Lerp(Color.red, Color.black, 0.5f);
                    }
                }
            }
        }

        public Vector2 meterPos;

        public List<AbstractCreature> players;

        public StoryAvatarSettings personaSettings;

        public AbstractCreature ac;

        public Vector2 meterLastPos;

        public Dictionary<int, OnlinePlayerIcon> playerIcons;

        public float fade;

        public float lastFade;

        public float customFade;

        public const float IconDistance = 30f;

        public int iconOffsetIndex;

        public int counter;

        public bool cutscene;


        public FContainer fContainer;

        public FSprite cameraArrowSprite;

        public PlayerState playerStateFocusedByCamera;

        public Vector2 DrawPos(float timeStacker)
        {
            return Vector2.Lerp(meterLastPos, meterPos, timeStacker);
        }

        public OnlineStoryHud(global::HUD.HUD hud, FContainer fContainer, StoryGameMode gameMode)
                    : base(hud)
        {
            personaSettings = (StoryAvatarSettings)OnlineManager.lobby.gameMode.avatarSettings;
            meterPos = new Vector2(RWCustom.Custom.rainWorld.options.ScreenSize.x - 90f, 100f);
            meterLastPos = meterPos;

            /*players = new List<AbstractCreature>();*/



            players = gameMode.lobby.playerAvatars
                             .Where(avatar => avatar.type != (byte)OnlineEntity.EntityId.IdType.none)
                             .Select(avatar => avatar.FindEntity(true))
                             .OfType<OnlinePhysicalObject>()
                             .Select(opo => opo.apo)
                             .OfType<AbstractCreature>()
                             .ToList();


            playerIcons = new Dictionary<int, OnlinePlayerIcon>();
            base.hud = hud;
            this.fContainer = fContainer;
            for (int i = 0; i < players.Count; i++)
            {
                Color color = personaSettings.bodyColor;
                OnlinePlayerIcon value = new OnlinePlayerIcon(this, players[i], color); // TODO: Each HUD user dictates the color; make a function to handle the add / leave
                playerIcons.Add(i, value);
            }

            fade = 0f;
            lastFade = 0f;
            customFade = 0f;
            cameraArrowSprite = new FSprite("Multiplayer_Arrow");
            fContainer.AddChild(cameraArrowSprite);
        }


        public override void ClearSprites()
        {
            base.ClearSprites();
            foreach (OnlinePlayerIcon value in playerIcons.Values)
            {
                value.ClearSprites();
            }

            cameraArrowSprite.RemoveFromContainer();
            playerIcons = null;
            cameraArrowSprite = null;
            RainMeadow.RainMeadow.Debug("PlayerMeter: cleaning sprites");
        }

        public override void Draw(float timeStacker)
        {
            base.Draw(timeStacker);
            foreach (OnlinePlayerIcon value in playerIcons.Values)
            {
                value.Draw(timeStacker);
            }

            int num = iconOffsetIndex;
            bool flag = playerStateFocusedByCamera?.dead ?? ((hud.rainWorld.processManager.currentMainLoop as RainWorldGame).AlivePlayers.Count == 0);
            if (playerStateFocusedByCamera != null)
            {
                iconOffsetIndex = playerStateFocusedByCamera.playerNumber;
                cameraArrowSprite.y = DrawPos(timeStacker).y + 15f + (float)(flag ? 5 : 0) + (float)(cutscene ? 5 : 0);
                cameraArrowSprite.color = personaSettings.bodyColor;
            }

            if (num != iconOffsetIndex)
            {
                cameraArrowSprite.x = Mathf.Lerp(cameraArrowSprite.x, DrawPos(timeStacker).x + (float)iconOffsetIndex * 30f, timeStacker);
            }
            else
            {
                cameraArrowSprite.x = DrawPos(timeStacker).x + (float)iconOffsetIndex * 30f;
            }

            float alpha = Mathf.Lerp(lastFade, fade, timeStacker);
            cameraArrowSprite.alpha = alpha;
        }

        public override void Update()
        {
            base.Update();
            RainWorldGame rainWorldGame = hud.rainWorld.processManager.currentMainLoop as RainWorldGame;
            if (hud.foodMeter != null)
            {
                meterPos.x = rainWorldGame.rainWorld.options.ScreenSize.x - hud.foodMeter.pos.x - 75f + (float)(3 - 2) * 30f; // 3 - 2 is wrong
                meterPos.y = hud.foodMeter.pos.y;
                fade = Mathf.Max(hud.foodMeter.fade, customFade);
            }

            customFade = Mathf.Max(0f, customFade - 0.05f);
            lastFade = fade;
            meterLastPos = meterPos;
            if (fade > 0f)
            {
                counter++;
            }
            else
            {
                counter = 0;
            }

            players = OnlineManager.lobby.playerAvatars
                         .Where(avatar => avatar.type != (byte)OnlineEntity.EntityId.IdType.none)
                         .Select(avatar => avatar.FindEntity(true))
                         .OfType<OnlinePhysicalObject>()
                         .Select(opo => opo.apo)
                         .OfType<AbstractCreature>()
                         .ToList();



            if (players.Count != playerIcons.Count)
            {
                // Create new player icons for new players
                for (int i = 0; i < players.Count; i++)
                {
                    // Only create new icons if there isn't already one for this player
                    if (!playerIcons.ContainsKey(i))
                    {
                        Color color = personaSettings.bodyColor; // TODO
                        OnlinePlayerIcon value = new OnlinePlayerIcon(this, players[i], color);
                        playerIcons.Add(i, value);
                    }

                    if (playerIcons.Count > players.Count)
                    {

                        playerIcons.Remove(i); // TODO: Not good, but


                    }
                }
            }

            foreach (KeyValuePair<int, OnlinePlayerIcon> playerIcon in playerIcons)
            {
                playerIcon.Value.Update();
                playerIcon.Value.pos = meterPos + new Vector2((float)playerIcon.Key * 30f, 0f);
            }


            Player player = hud.owner as Player;
            if (player != null && player.room != null)
            {
                AbstractCreature followAbstractCreature = player.room.game.cameras[0].followAbstractCreature;


                playerStateFocusedByCamera = ((followAbstractCreature != null) ? (followAbstractCreature.state as PlayerState) : null);
                cutscene = player.room.game.cameras[0].InCutscene;
            }
            else
            {
                playerStateFocusedByCamera = null;
            }

            cameraArrowSprite.element = Futile.atlasManager.GetElementWithName(cutscene ? "Jolly_Lock_1" : "Multiplayer_Arrow");
        }
    }