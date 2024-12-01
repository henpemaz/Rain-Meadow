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
        private List<MeadowMapIndicator> mapIndicators = new();
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
            UpdateAvatars();
        }

        public void UpdateAvatars()
        {
            var activeAvatars = OnlineManager.lobby.playerAvatars.Select(kv => kv.Value.FindEntity(true) as OnlineCreature).Where(e => e != null);
            var currentAvatars = indicators.Select(i => i.avatar).ToList(); //needs duplication
            activeAvatars.Except(currentAvatars).Do(AvatarAdded);
            currentAvatars.Except(activeAvatars).Do(AvatarRemoved);
        }

        public void AvatarAdded(OnlineCreature avatar)
        {
            RainMeadow.DebugMe();
            MeadowPlayerIndicator indicator = new MeadowPlayerIndicator(hud, camera, avatar, this);
            this.indicators.Add(indicator);
            hud.AddPart(indicator);
            MeadowMapIndicator mapIndicator = new MeadowMapIndicator(hud.map, avatar, this);
            this.mapIndicators.Add(mapIndicator);
            hud.map.mapObjects.Add(mapIndicator);
        }

        public void AvatarRemoved(OnlineCreature avatar)
        {
            RainMeadow.DebugMe();
            var indicator = this.indicators.First(i => i.avatar == avatar);
            this.indicators.Remove(indicator);
            indicator.slatedForDeletion = true;
            var mapIndicator = this.mapIndicators.First(i => i.avatar == avatar);
            this.mapIndicators.Remove(mapIndicator);
            mapIndicator.Destroy();
        }

        public override void Update()
        {
            base.Update();
            UpdateAvatars();
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
            public OnlineCreature avatar;
            public MeadowAvatarData avatarSettings;
            private MeadowHud meadowHud;
            private Rect camrect;
            private Vector2 pos;
            private Vector2 lastPos;
            private FSprite gradient;
            private HUD.HUD hud;
            private RoomCamera camera;
            private Vector2 pointDir;
            private FLabel label;
            private FSprite arrowSprite;
            private bool active;
            private float alpha;
            private float lastAlpha;

            public MeadowPlayerIndicator(HUD.HUD hud, RoomCamera camera, OnlineCreature avatar, MeadowHud meadowHud) : base(hud)
            {
                this.avatar = avatar;
                this.avatarSettings = avatar.GetData<MeadowAvatarData>();

                this.hud = hud;
                this.camera = camera;
                this.meadowHud = meadowHud;

                this.camrect = new Rect(Vector2.zero, this.camera.sSize).CloneWithExpansion(-30f);

                this.pos = new Vector2(-1000f, -1000f);
                this.lastPos = this.pos;

                Color uiColor = avatarSettings.EmoteBackgroundColor(MeadowProgression.Emote.emoteHello);

                this.gradient = new FSprite("Futile_White", true);
                this.gradient.shader = hud.rainWorld.Shaders["FlatLight"];
                this.gradient.color = new Color(0f, 0f, 0f);
                hud.fContainers[0].AddChild(this.gradient);
                this.gradient.alpha = 0f;
                this.gradient.x = -1000f;
                this.label = new FLabel(Custom.GetFont(), avatar.owner.id.name);
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
                if (!meadowHud.showPlayerNames && !(avatar.isMine && meadowHud.arrowOnSelfNeeded))
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

        public class MeadowMapIndicator : Map.MapObject
        {
            public OnlineCreature avatar;
            public MeadowAvatarData avatarSettings;
            public MeadowHud meadowHud;
            private CreatureSymbol symbol;

            public MeadowMapIndicator(Map map, OnlineCreature avatar, MeadowHud meadowHud) : base(map)
            {
                this.avatar = avatar;
                this.avatarSettings = avatar.GetData<MeadowAvatarData>();
                this.meadowHud = meadowHud;

                this.symbol = new CreatureSymbol(CreatureSymbol.SymbolDataFromCreature(avatar.abstractCreature), this.meadowHud.hud.map.inFrontContainer);
                symbol.Show(false);
                avatarSettings.ModifyBodyColor(ref symbol.myColor);
            }

            public override void Update()
            {
                base.Update();
                symbol.Update();
            }

            public override void Draw(float timeStacker)
            {
                if (!this.map.visible)
                {
                    symbol.symbolSprite.isVisible = false;
                    return;
                }

                symbol.symbolSprite.isVisible = true;
                Vector2 drawPos;
                if (avatar.abstractCreature.pos.TileDefined)
                {
                    drawPos = map.RoomToMapPos(avatar.abstractCreature.pos.Tile.ToVector2() * 20f, avatar.abstractCreature.pos.room, timeStacker);
                }
                else
                {
                    drawPos = map.RoomToMapPos(map.mapData.SizeOfRoom(avatar.abstractCreature.pos.room).ToVector2() * 10f, avatar.abstractCreature.pos.room, timeStacker);
                }
                symbol.Draw(timeStacker, drawPos);
                symbol.symbolSprite.alpha = this.map.fade * Mathf.Lerp(this.map.Alpha(this.map.mapData.LayerOfRoom(avatar.abstractCreature.pos.room), 1f, true), 1f, 0.25f);
            }

            public override void Destroy()
            {
                symbol.RemoveSprites();
                base.Destroy();
            }
        }
    }
}