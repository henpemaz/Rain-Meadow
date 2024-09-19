using HarmonyLib;
using HUD;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowHud : HudPart
    {
        private List<MeadowPlayerIndicator> indicators = new();
        private RoomCamera camera;
        private Creature owner;
        private bool showPlayerNames;
        private bool arrowOnSelfNeeded;
        private bool lastMapDown;

        public MeadowHud(HUD.HUD hud, RoomCamera camera, Creature owner) : base(hud)
        {
            this.hud = hud;
            this.camera = camera;
            this.owner = owner;
            UpdatePlayers();
        }

        public void UpdatePlayers()
        {
            var avatarSettings = OnlineManager.lobby.clientSettings.Values.OfType<MeadowAvatarSettings>().Where(mas => mas.inGame);
            var currentSettings = indicators.Select(i => i.avatarSettings).ToList(); //needs duplication
            avatarSettings.Except(currentSettings).Do(PlayerAdded);
            currentSettings.Except(avatarSettings).Do(PlayerRemoved);
        }

        public void PlayerAdded(MeadowAvatarSettings avatarSettings)
        {
            RainMeadow.DebugMe();
            MeadowPlayerIndicator indicator = new MeadowPlayerIndicator(hud, camera, avatarSettings, this);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
        }

        public void PlayerRemoved(MeadowAvatarSettings avatarSettings)
        {
            RainMeadow.DebugMe();
            var indicator = this.indicators.First(i => i.avatarSettings == avatarSettings);
            this.indicators.Remove(indicator);
            indicator.slatedForDeletion = true;
        }

        public override void Update()
        {
            base.Update();
            UpdatePlayers();
            var mapDown = RWInput.CheckSpecificButton(0, RewiredConsts.Action.Map);
            if (!mapDown && lastMapDown && hud.map.fade <= 0.8f)
            {
                showPlayerNames = !showPlayerNames;
            }
            arrowOnSelfNeeded = (owner.room == null && owner.NPCTransportationDestination != default)
                || mapDown;
            lastMapDown = mapDown;
        }

        private class MeadowPlayerIndicator : HUD.HudPart
        {
            public MeadowAvatarSettings avatarSettings;
            private MeadowHud meadowHud;
            private Rect camrect;
            private Vector2 pos;
            private Vector2 lastPos;
            private FSprite gradient;
            private HUD.HUD hud;
            private RoomCamera camera;
            private OnlinePhysicalObject avatar;
            private Vector2 pointDir;
            private FLabel label;
            private FSprite arrowSprite;
            private bool active;
            private float alpha;
            private float lastAlpha;

            public MeadowPlayerIndicator(HUD.HUD hud, RoomCamera camera, MeadowAvatarSettings avatarSettings, MeadowHud meadowHud) : base(hud)
            {
                this.hud = hud;
                this.camera = camera;
                this.avatarSettings = avatarSettings;
                this.meadowHud = meadowHud;

                this.camrect = new Rect(Vector2.zero, this.camera.sSize).CloneWithExpansion(-30f);

                this.pos = new Vector2(-1000f, -1000f);
                this.lastPos = this.pos;

                Color uiColor = (avatarSettings.MakeCustomization() as MeadowAvatarCustomization).EmoteBackgroundColor(MeadowProgression.Emote.emoteHello);

                this.gradient = new FSprite("Futile_White", true);
                this.gradient.shader = hud.rainWorld.Shaders["FlatLight"];
                this.gradient.color = new Color(0f, 0f, 0f);
                hud.fContainers[0].AddChild(this.gradient);
                this.gradient.alpha = 0f;
                this.gradient.x = -1000f;
                this.label = new FLabel(Custom.GetFont(), avatarSettings.owner.id.name);
                this.label.color = uiColor;
                hud.fContainers[0].AddChild(this.label);
                this.label.alpha = 0f;
                this.label.x = -1000f;
                this.arrowSprite = new FSprite("Multiplayer_Arrow", true);
                this.arrowSprite.color = uiColor;
                hud.fContainers[0].AddChild(this.arrowSprite);
                this.arrowSprite.alpha = 0f;
                this.arrowSprite.x = -1000f;
            }

            public override void Update()
            {
                base.Update();
                lastAlpha = alpha;
                this.active = false;
                bool needed;
                if (!meadowHud.showPlayerNames && !(avatarSettings.isMine && meadowHud.arrowOnSelfNeeded))
                {
                    needed = false;
                    if (alpha == 0f) return;
                }
                else
                {
                    needed = true;
                }
                alpha = Custom.LerpAndTick(alpha, needed ? 1f : 0f, 0.1f, 0.033f);

                // tracking
                lastPos = pos;

                if (avatar == null || avatar.primaryResource == null)
                {
                    if (avatarSettings.avatarId != null)
                    {
                        this.avatar = (OnlinePhysicalObject)avatarSettings.avatarId.FindEntity(true);
                    }
                }
                if (avatar == null || camera.room == null) return;

                Vector2 rawPos = new();
                // in this room
                if (avatar.apo.Room == camera.room.abstractRoom)
                {
                    // in room or in shortcut
                    if (avatar.apo.realizedObject is Creature player)
                    {
                        if (player.room == camera.room)
                        {
                            active = true;
                            rawPos = player.DangerPos - camera.pos;
                        }
                        else if (player.room == null)
                        {
                            Vector2? shortcutpos = camera.game.shortcuts.OnScreenPositionOfInShortCutCreature(camera.room, player);
                            if (shortcutpos != null)
                            {
                                active = true;
                                rawPos = shortcutpos.Value - camera.pos;
                            }
                        }
                        if (active)
                        {
                            this.pos = camrect.GetClosestInteriorPoint(rawPos); // gives straight arrows except corners
                            if (pos != rawPos)
                            {
                                pointDir = (rawPos - pos).normalized;
                            }
                            else
                            {
                                pos += new Vector2(0f, (player.room != null) ? 45f : 15f);
                                this.pointDir = Vector2.down;
                            }
                        }
                    }
                }
            }


            public override void Draw(float timeStacker)
            {
                if (!active)
                {
                    gradient.isVisible = false;
                    label.isVisible = false;
                    arrowSprite.isVisible = false;
                    return;
                }
                gradient.isVisible = true;
                label.isVisible = true;
                arrowSprite.isVisible = true;

                Vector2 usePos = Vector2.Lerp(this.lastPos, this.pos, timeStacker);
                float useAlpha = Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker);
                this.gradient.x = usePos.x;
                this.gradient.y = usePos.y + 10f;
                this.gradient.scale = Mathf.Lerp(80f, 110f, useAlpha) / 16f;
                this.gradient.alpha = 0.17f * Mathf.Pow(useAlpha, 2f);
                this.arrowSprite.x = usePos.x;
                this.arrowSprite.y = usePos.y;
                this.arrowSprite.rotation = RWCustom.Custom.VecToDeg(pointDir * -1);

                this.label.x = usePos.x;
                this.label.y = usePos.y + 20f;

                this.label.alpha = useAlpha;
                this.arrowSprite.alpha = useAlpha;
            }

            public override void ClearSprites()
            {
                base.ClearSprites();
                this.gradient.RemoveFromContainer();
                this.arrowSprite.RemoveFromContainer();
                this.label.RemoveFromContainer();
            }
        }
    }
}