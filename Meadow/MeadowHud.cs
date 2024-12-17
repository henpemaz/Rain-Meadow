using HarmonyLib;
using HUD;
using RWCustom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace RainMeadow
{
    internal class MeadowHud : HudPart
    {
        private List<MeadowPlayerIndicator> indicators = new();
        private List<MeadowMapIndicator> mapIndicators = new();
        private RoomCamera camera;
        private Creature owner;
        private bool displayNames;
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
            if (!mapDown && lastMapDown && hud.map.fade <= 0.7f)
            {
                MeadowProgression.progressionData.displayNames = !displayNames;
            }
            displayNames = MeadowProgression.progressionData.displayNames; // can be changed through menus

            arrowOnSelfNeeded = (owner.room == null && owner.NPCTransportationDestination != default)
                || mapDown;
            lastMapDown = mapDown;
        }

        internal class PlayerOnScreenTracker
        {
            public RoomCamera camera;
            public OnlineCreature avatar;
            private Rect camrect;

            public PlayerOnScreenTracker(RoomCamera camera, OnlineCreature avatar)
            {
                this.camera = camera;
                this.avatar = avatar;
                this.camrect = new Rect(Vector2.zero, this.camera.sSize).CloneWithExpansion(-30f);

                offscreenCounter = int.MaxValue / 2;
            }

            // output
            public Vector2 pos;
            public Vector2 lastPos;
            public Vector2 pointDir;
            public int offscreenCounter;
            public bool active;

            private WorldCoordinate lastWorldPos;
            private int lastCameraPos;
            private int lastAbstractRoom;

            public void Update()
            {
                lastPos = pos;
                active = false;

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
                                offscreenCounter++;
                            }
                            else
                            {
                                pos += new Vector2(0f, (player.room != null) ? 45f : 15f);
                                this.pointDir = Vector2.down;
                                offscreenCounter = 0;
                            }
                        }
                    }
                }
                else // different room
                {
                    offscreenCounter++;
                    // neighbor
                    var connections = camera.room.abstractRoom.connections;
                    for (int i = 0; i < connections.Length; i++)
                    {
                        if (avatar.apo.pos.room == connections[i])
                        {
                            active = true;
                            var shortcutpos = camera.room.LocalCoordinateOfNode(i);
                            rawPos = camera.room.MiddleOfTile(shortcutpos) - camera.pos;
                            pointDir = camera.room.ShorcutEntranceHoleDirection(shortcutpos.Tile).ToVector2() * -1;
                            this.pos = camrect.GetClosestInteriorPoint(rawPos);
                            if (pos != rawPos)
                            {
                                pointDir = (rawPos - pos).normalized;
                            }
                            break;
                        }
                    }
                    if (!active) // elsewhere
                    {
                        var world = camera.game.world;
                        if (world.GetAbstractRoom(avatar.apo.pos.room) is AbstractRoom abstractRoom) // room in region
                        {
                            active = true;
                            if (avatar.apo.pos != lastWorldPos || camera.currentCameraPosition != lastCameraPos || camera.room.abstractRoom.index != lastAbstractRoom) // cache these maths
                            {

                                var worldpos = (abstractRoom.mapPos / 3f + new Vector2(10f, 10f)) * 20f;
                                if (this.avatar.realizedCreature is Creature creature) worldpos += creature.mainBodyChunk.pos - abstractRoom.size.ToVector2() * 20f / 2f;
                                else if (avatar.apo.pos.TileDefined) worldpos += avatar.apo.pos.Tile.ToVector2() * 20f - abstractRoom.size.ToVector2() * 20f / 2f;

                                var viewpos = (camera.room.abstractRoom.mapPos / 3f + new Vector2(10f, 10f)) * 20f + camera.pos + this.camera.sSize / 2f - camera.room.abstractRoom.size.ToVector2() * 20f / 2f;

                                pointDir = (worldpos - viewpos).normalized;
                                pos = camrect.GetClosestInteriorPointAlongLineFromCenter(this.camera.sSize / 2f + pointDir * 2048f); // gives angled arrows

                                lastWorldPos = avatar.apo.pos;
                                lastCameraPos = camera.currentCameraPosition;
                                lastAbstractRoom = camera.room.abstractRoom.index;
                            }
                        } // else not found, inactive
                    }
                }
            }
        }

        private class MeadowPlayerIndicator : HUD.HudPart
        {
            public OnlineCreature avatar;
            public MeadowAvatarData avatarSettings;
            private MeadowHud meadowHud;
            private PlayerOnScreenTracker tracker;
            private FSprite gradient;
            private RoomCamera camera;
            private FLabel label;
            private FSprite arrowSprite;
            private float alpha;
            private bool showLabel;
            private float lastAlpha;

            public MeadowPlayerIndicator(HUD.HUD hud, RoomCamera camera, OnlineCreature avatar, MeadowHud meadowHud) : base(hud)
            {
                this.avatar = avatar;
                this.avatarSettings = avatar.GetData<MeadowAvatarData>();

                this.hud = hud;
                this.camera = camera;
                this.meadowHud = meadowHud;

                this.tracker = new PlayerOnScreenTracker(camera, avatar);

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


            const int offscreenTrack = 400;
            public override void Update()
            {
                base.Update();
                tracker.Update();

                lastAlpha = alpha;

                // old
                //bool needed = false;
                //if (meadowHud.showPlayerNames) needed = true;
                //if (avatar.isMine && meadowHud.arrowOnSelfNeeded) needed = true;
                //if (tracker.offscreenCounter < offscreenTrack) needed = true;

                //alpha = Custom.LerpAndTick(alpha, needed ? 1f : 0f, 0.1f, 0.033f);

                float targetAlpha = 0.2f;
                if (avatar.isMine && meadowHud.arrowOnSelfNeeded) targetAlpha = 1f;
                if (tracker.offscreenCounter < offscreenTrack) targetAlpha = 0.6f;
                if (tracker.offscreenCounter < int.MaxValue / 2) targetAlpha = 0.4f;

                alpha = Custom.LerpAndTick(alpha, targetAlpha, 0.1f, 0.033f);

                this.showLabel = meadowHud.displayNames;
            }


            public override void Draw(float timeStacker)
            {
                if (!tracker.active || (alpha == 0f && lastAlpha == 0))
                {
                    gradient.isVisible = false;
                    label.isVisible = false;
                    arrowSprite.isVisible = false;
                    return;
                }
                gradient.isVisible = true;
                label.isVisible = showLabel;
                arrowSprite.isVisible = true;

                Vector2 usePos = Vector2.Lerp(this.tracker.lastPos, this.tracker.pos, timeStacker);
                float useAlpha = Mathf.Lerp(this.lastAlpha, this.alpha, timeStacker);
                this.gradient.x = usePos.x;
                this.gradient.y = usePos.y + 10f;
                this.gradient.scale = Mathf.Lerp(80f, 110f, useAlpha) / 16f;
                this.gradient.alpha = 0.17f * Mathf.Pow(useAlpha, 2f);
                this.arrowSprite.x = usePos.x;
                this.arrowSprite.y = usePos.y;
                this.arrowSprite.rotation = RWCustom.Custom.VecToDeg(tracker.pointDir * -1);

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