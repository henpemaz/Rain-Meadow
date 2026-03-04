using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public static class DebugOverlay
    {
        private static FContainer overlayContainer;

        private class ResourceNode
        {
            public OnlineResource resource;
            private FSprite lineSprite;
            private FSprite lineSprite2;
            private FLabel label;
            public string text = "";
            public Vector2 pos;
            public Color color = Color.white;
            public float thickness = 3;
            public int lines = 0;
            public int entityCount = 0;
            public List<EntityIconPair> childEntities = [];

            public float width => label.textRect.width;
            public ResourceNode(RainWorld rainWorld, FContainer container, OnlineResource resource)
            {
                this.resource = resource;

                lineSprite = new FSprite("pixel");
                lineSprite2 = new FSprite("pixel");

                label = new FLabel(Custom.GetFont(), resource.ToString());
                label.alignment = FLabelAlignment.Left;
                label.color = Color.white;

                container.AddChild(lineSprite);
                container.AddChild(lineSprite2);
                container.AddChild(label);
            }

            public void Update()
            {
                label.x = pos.x + 0.01f + 20;
                label.y = pos.y;

                label.color =
                    resource.isOwner ? Color.green :
                    resource.isSupervisor ? Color.blue :
                    resource.canRelease ? Color.red : Color.white;

                lineSprite.x = pos.x + 20;
                lineSprite.y = pos.y - 8;
                lineSprite.scaleX = 2;
                lineSprite.anchorY = 1;
                lineSprite.scaleY = Mathf.Max(lines * 35 - 8, 0);

                lineSprite2.x = pos.x;
                lineSprite2.y = pos.y;
                lineSprite2.anchorX = 0;
                lineSprite2.scaleX = 15;
                lineSprite2.scaleY = 2;
                lineSprite2.color = OnlineManager.feeds.Exists(sub => sub.resource == resource) ? Color.green : Color.white;

                lines = 0;
                entityCount = 0;
            }

            public void RemoveSprites()
            {
                lineSprite.RemoveFromContainer();
                lineSprite2.RemoveFromContainer();
                label.RemoveFromContainer();
            }
        }

        private static List<ResourceNode> resourceNodes = new List<ResourceNode>();

        private class EntityNode
        {
            public OnlineEntity entity;
            private IconSymbol iconSymbol;
            private FLabel label;
            public string text = "";
            public Vector2 pos;
            public Color color = Color.white;
            public float rad = 5;
            public float thickness = 3;
            public EntityNode(RainWorld rainWorld, FContainer container, OnlineEntity onlineEntity)
            {
                this.entity = onlineEntity;

                if (onlineEntity is OnlinePhysicalObject onlinePhysicalObject)
                {
                    if (onlinePhysicalObject.apo is AbstractCreature creature)
                    {
                        iconSymbol = new CreatureSymbol(CreatureSymbol.SymbolDataFromCreature(creature), container);
                    }
                    else
                    {
                        iconSymbol = new ItemSymbol(ItemSymbol.SymbolDataFromItem(onlinePhysicalObject.apo).GetValueOrDefault(), container);
                    }
                    iconSymbol.Show(true);
                    iconSymbol.showFlash = iconSymbol.lastShowFlash = 0;
                }

                label = new FLabel(Custom.GetFont(), text);
                label.color = Color.white;

                container.AddChild(label);
            }

            public void Update()
            {
                float alpha = entity.isMine ? 1 : 0.5f;
                iconSymbol.symbolSprite.alpha = alpha;
                iconSymbol.shadowSprite1.alpha = alpha;
                iconSymbol.shadowSprite2.alpha = alpha;
                iconSymbol.Draw(1, pos + new Vector2(0.5f, 0));

                label.x = pos.x + 0.01f;
                label.y = pos.y + 15f;
                label.text = text;
            }

            public void RemoveSprites()
            {
                iconSymbol.RemoveSprites();
                label.RemoveFromContainer();
            }
        }

        private static List<EntityNode> entityNodes = new List<EntityNode>();
        private static List<FLabel> outgoingLabels = new List<FLabel>();
        private static List<FLabel> incomingLabels = new List<FLabel>();

        public class playerCache
        {
            public List<Individual> items;
            public static TimeSpan itemsExpiralDate;
            public class Individual
            {
                public OnlinePlayer player;
                public DateTime expiralDate;

                public bool timeToDelete => DateTime.Now > expiralDate;
                public Individual(OnlinePlayer Player)
                {
                    player = Player;
                    expiralDate = DateTime.Now + itemsExpiralDate;
                }
            }

            public playerCache(int size, TimeSpan expiry)
            {
                items = new List<Individual>(size);
                itemsExpiralDate = expiry;
            }

            public void addPlayer(OnlinePlayer Player)
            {
                if (items.Exists(x => x.player == Player))
                {
                    items.Find(x => x.player == Player).expiralDate = DateTime.Now + itemsExpiralDate;
                    return;
                }
                items.Add(new Individual(Player));
            }

            public void removeExpired()
            {
                items.RemoveAll(x => x.timeToDelete);
            }
        }

        public static playerCache playersRead = new playerCache(16, TimeSpan.FromSeconds(5));
        public static playerCache playersWritten = new playerCache(16, TimeSpan.FromSeconds(5));

        private class EntityIconPair
        {
            public OnlineEntity Entity { get; set; }
            public IconSymbol.IconSymbolData Icon { get; set; }
        }

        public static void Update(RainWorldGame self, float dt)
        {
            if (overlayContainer == null && self.devToolsActive && !ProfilerOverlay.profilerActive)
            {
                CreateOverlay(self);
            }

            if (overlayContainer != null && !self.devToolsActive)
            {
                RemoveOverlay(self);
            }

            if (overlayContainer != null && self.devToolsActive && ProfilerOverlay.profilerActive)
            {
                RemoveOverlay(self);
            }

            if (overlayContainer == null)
                return;

            Vector2 screenSize = self.rainWorld.options.ScreenSize;

            outgoingLabels.ForEach(label => label.RemoveFromContainer());
            outgoingLabels.Clear();
            incomingLabels.ForEach(label => label.RemoveFromContainer());
            incomingLabels.Clear();

            playersWritten.removeExpired();
            playersRead.removeExpired();

            int line = 0;
            foreach (playerCache.Individual idv in playersWritten.items)
            {
                var player = idv.player;
                var playerTruePing = Math.Max(1, player.ping - 16);
                var averageBytes = 0;
                foreach (var bytes in player.bytesOut)
                {
                    averageBytes += bytes;
                }
                // averageBytes = bytes per 40 frames
                averageBytes = (int)((float)averageBytes / 40 * OnlineManager.instance.framesPerSecond); // bytes per second
                var averageBits = averageBytes * 8;

                string clientFlags = AssembleClientFlags(player);
                FLabel label = new FLabel(Custom.GetFont(), $"{player}{clientFlags} ({averageBits / 1000}kbps - {playerTruePing}ms)")
                {
                    x = 5.01f,
                    y = screenSize.y - 25 - 15 * line,
                    alignment = FLabelAlignment.Left,
                    color =
                        player.eventsWritten ? new Color(1, 0.5f, 0) :
                        player.statesWritten ? Color.white :
                        Color.grey
                };

                overlayContainer.AddChild(label);
                outgoingLabels.Add(label);
                line++;
            }

            line = 0;
            foreach (playerCache.Individual idv in playersRead.items)
            {
                var player = idv.player;
                var playerTruePing = Math.Max(1, player.ping - 16);
                var averageBytes = 0;
                foreach (var bytes in player.bytesIn)
                {
                    averageBytes += bytes;
                }
                // averageBytes = bytes per 40 frames
                averageBytes = (int)((float)averageBytes / 40 * OnlineManager.instance.framesPerSecond); // bytes per second
                var averageBits = averageBytes * 8;

                string clientFlags = AssembleClientFlags(player);
                FLabel label = new FLabel(Custom.GetFont(), $"{player}{clientFlags} ({averageBits / 1000}kbps - {playerTruePing}ms)")
                {
                    x = 205.01f,
                    y = screenSize.y - 25 - 15 * line,
                    alignment = FLabelAlignment.Left,
                    color =
                        player.eventsRead ? new Color(1, 0.5f, 0) :
                        player.statesRead ? Color.white :
                        Color.grey
                };

                overlayContainer.AddChild(label);
                incomingLabels.Add(label);
                line++;
            }

            resourceNodes.RemoveAll(node =>
            {
                if (!node.resource.isActive)
                {
                    node.RemoveSprites();
                    return true;
                }
                return false;
            });

            var root = resourceNodes[0];

            // Worlds (Regions)

            RoomSession inRoomSession;
            WorldSession inWorldSession = null;
            if (self.cameras[0].room == null || !RoomSession.map.TryGetValue(self.cameras[0].room.abstractRoom, out inRoomSession))
            {
                return;
            }
            inWorldSession = inRoomSession.worldSession;

            var worlds = OnlineManager.lobby.overworld.worldSessions.Values.ToList();
            worlds.Sort((x, y) => (x == inWorldSession ? -1 : 0) + (y == inWorldSession ? 1 : 0));

            int lastWorldLines = 0;
            foreach (var worldSession in worlds)
            {
                if (!worldSession.isActive)
                    continue;

                ResourceNode regionNode = resourceNodes.Find(regionNode => regionNode.resource == worldSession);
                if (regionNode == null)
                {
                    regionNode = new ResourceNode(self.rainWorld, overlayContainer, worldSession);
                    resourceNodes.Add(regionNode);
                }

                root.lines += lastWorldLines + 1;
                regionNode.pos = root.pos + new Vector2(20, root.lines * -35);

                // Rooms
                var rooms = worldSession.roomSessions.Values.ToList();
                rooms.Sort((x, y) => (x == inRoomSession ? -1 : 0) + (y == inRoomSession ? 1 : 0));
                foreach (var roomSession in rooms)
                {
                    if (!roomSession.isActive)
                        continue;

                    ResourceNode roomNode = resourceNodes.Find(node => node.resource == roomSession);
                    if (roomNode == null)
                    {
                        roomNode = new ResourceNode(self.rainWorld, overlayContainer, roomSession);
                        resourceNodes.Add(roomNode);
                    }

                    regionNode.lines++;
                    roomNode.pos = regionNode.pos + new Vector2(20, regionNode.lines * -35);
                }

                lastWorldLines = regionNode.lines;
            }

            for (int i = 0; i < resourceNodes.Count; i++)
            {
                resourceNodes[i].Update();
                resourceNodes[i].childEntities.Clear();
            }

            var onlineEntities = OnlineManager.recentEntities.Values.ToList();
            for (int i = 0; i < onlineEntities.Count; i++)
            {
                ResourceNode resourceNode = resourceNodes.Find(node => node.resource == onlineEntities[i].currentlyJoinedResource);
                if (resourceNode != null && onlineEntities[i] is OnlinePhysicalObject onlinePhysicalObject)
                {
                    resourceNode.childEntities.Add(new EntityIconPair
                    {
                        Entity = onlineEntities[i],
                        Icon = (((OnlinePhysicalObject)onlineEntities[i]).apo is AbstractCreature creature) ?
                        CreatureSymbol.SymbolDataFromCreature(creature) :
                        ItemSymbol.SymbolDataFromItem(((OnlinePhysicalObject)onlineEntities[i]).apo).GetValueOrDefault(),
                    });
                }
            }

            for (int i = 0; i < resourceNodes.Count; i++)
            {
                resourceNodes[i].childEntities.Sort((x, y) =>
                {
                    int comp = (x.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature ? -1 : 0) + (y.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature ? 1 : 0); //Creatures then items
                    if (comp != 0) { return comp; }
                    if ((x.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature) && (y.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature))
                    {
                        comp = (((OnlineCreature)x.Entity).creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat ? -1 : 0) + (((OnlineCreature)y.Entity).creature.creatureTemplate.TopAncestor().type == CreatureTemplate.Type.Slugcat ? 1 : 0); //Us always first
                        if (comp != 0) { return comp; }
                        comp = (int)x.Icon.critType - (int)y.Icon.critType; //Creatures by type
                        if (comp != 0) { return comp; }
                    }
                    else
                    {
                        comp = (int)x.Icon.itemType - (int)y.Icon.itemType; //Items by type
                        if (comp != 0) { return comp; }
                    }
                    comp = x.Icon.intData - y.Icon.intData; //Objects by subtype (aka the root of all evil)
                    if (comp != 0) { return comp; }
                    comp = (x.Entity.isMine ? -1 : 0) + (y.Entity.isMine ? 1 : 0); //Owned first
                    return comp;
                });
            }

            for (int i = 0; i < entityNodes.Count; i++)
            {
                entityNodes[i].RemoveSprites();
            }
            entityNodes.Clear();

            for (int i = 0; i < resourceNodes.Count; i++)
            {
                if (resourceNodes[i].childEntities.Count == 0) { continue; }

                IconSymbol.IconSymbolData iconType = new();
                IconSymbol.IconSymbolData lastIconType = new();
                int j = 0;
                int iconCount = 1;
                bool lastIsMine = true;
                while (j < resourceNodes[i].childEntities.Count)
                {
                    iconType = (((OnlinePhysicalObject)resourceNodes[i].childEntities[j].Entity).apo is AbstractCreature creature) ?
                        CreatureSymbol.SymbolDataFromCreature(creature) :
                        ItemSymbol.SymbolDataFromItem(((OnlinePhysicalObject)resourceNodes[i].childEntities[j].Entity).apo).GetValueOrDefault();

                    if (iconType == lastIconType && resourceNodes[i].childEntities[j].Entity.isMine == lastIsMine)
                    {
                        iconCount++;
                        entityNodes.Last().text = iconCount.ToString();
                    }
                    else
                    {
                        iconCount = 1;
                        EntityNode entityNode = new EntityNode(self.rainWorld, overlayContainer, resourceNodes[i].childEntities[j].Entity)
                        {
                            text = (resourceNodes[i].childEntities[j].Entity.isMine && iconType.critType == CreatureTemplate.Type.Slugcat) ?
                                iconCount > 1 ?
                                    "U" + iconCount.ToString() : //This should never happen in normal gameplay, but, may as well support it I guess.
                                    "YOU" :
                                iconCount.ToString()
                        };
                        entityNodes.Add(entityNode);
                        entityNode.pos = resourceNodes[i].pos + new Vector2(40 + 22.5f * resourceNodes[i].entityCount + resourceNodes[i].width, 0);
                        resourceNodes[i].entityCount++;
                    }

                    lastIconType = iconType;
                    lastIsMine = resourceNodes[i].childEntities[j].Entity.isMine;
                    j++;
                }
            }

            for (int i = 0; i < entityNodes.Count; i++)
            {
                entityNodes[i].Update();
            }

            localClientSettings.text = "You:" + AssembleClientFlags(OnlineManager.mePlayer);
        }

        private static FLabel localClientSettings;
        public static void CreateOverlay(RainWorldGame self)
        {
            Vector2 screenSize = self.rainWorld.options.ScreenSize;
            overlayContainer = new FContainer();

            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Outgoing (Receivers)")
            {
                alignment = FLabelAlignment.Left,
                x = 5.01f,
                y = screenSize.y - 10,
            });
            overlayContainer.AddChild(new FLabel(Custom.GetFont(), "Incoming (Senders)")
            {
                alignment = FLabelAlignment.Left,
                x = 205.01f,
                y = screenSize.y - 10,
            });
            localClientSettings = (new FLabel(Custom.GetFont(), "You:" + AssembleClientFlags(OnlineManager.mePlayer))
            {
                alignment = FLabelAlignment.Left,
                x = 405.01f,
                y = screenSize.y - 10,
            });
            overlayContainer.AddChild(localClientSettings);

            // Lobby (Root)
            resourceNodes.Add(new ResourceNode(self.rainWorld, overlayContainer, OnlineManager.lobby)
            {
                pos = new Vector2(400, screenSize.y - 35),
            });

            Futile.stage.AddChild(overlayContainer);
        }

        public static void RemoveOverlay(RainWorldGame self)
        {
            resourceNodes.Clear();
            entityNodes.Clear();
            overlayContainer?.RemoveFromContainer();
            overlayContainer = null;
        }

        //This is no longer used but I do think it's good to keep around.
        private static List<OnlineEntity> SortOnlineEntities(List<OnlineEntity> onlineEntities)
        {
            onlineEntities.Sort((x, y) =>
            {
                int comp = (x is OnlinePhysicalObject ? -1 : 0) + (y is OnlinePhysicalObject ? 1 : 0);
                if (comp != 0)
                {
                    return comp;
                }
                comp = (x is OnlineCreature ? -1 : 0) + (y is OnlineCreature ? 1 : 0);
                if (comp != 0)
                {
                    return comp;
                }
                comp = (x is ClientSettings ? -1 : 0) + (y is ClientSettings ? 1 : 0);
                if (comp != 0)
                {
                    return comp;
                }
                comp = (x.isMine ? -1 : 0) + (y.isMine ? 1 : 0);
                if (comp != 0)
                {
                    return comp;
                }
                if (x is OnlinePhysicalObject && y is OnlinePhysicalObject)
                {
                    comp = (int)((OnlinePhysicalObject)x).apo.type - (int)((OnlinePhysicalObject)y).apo.type;
                    if (comp != 0)
                    {
                        return comp;
                    }
                }
                if (x is OnlineCreature && y is OnlineCreature)
                {
                    comp = (int)((AbstractCreature)((OnlineCreature)x).apo).creatureTemplate.type - (int)((AbstractCreature)((OnlineCreature)y).apo).creatureTemplate.type;
                    if (comp != 0)
                    {
                        return comp;
                    }
                }
                return comp;
            });
            return onlineEntities;
        }

        private static string AssembleClientFlags(OnlinePlayer player)
        {
            string clientFlags = "";
            if (OnlineManager.lobby.clientSettings.TryGetValue(player, out _) && OnlineManager.lobby.gameMode is StoryGameMode)
            {
                if (OnlineManager.lobby.clientSettings[player].TryGetData<StoryClientSettingsData>(out var currentClientSettings))
                {
                    if (!OnlineManager.lobby.clientSettings[player].inGame)
                    {
                        clientFlags += "L";
                    }
                    else
                    {
                        clientFlags += currentClientSettings.readyForWin        ? "S" : "";
                        clientFlags += currentClientSettings.readyForTransition ? "G" : "";
                        clientFlags += currentClientSettings.isDead             ? "D" : "";
                    }
                }
            }
            else if (OnlineManager.lobby.clientSettings.TryGetValue(player, out _) && OnlineManager.lobby.gameMode is ArenaOnlineGameMode)
            {
                if (OnlineManager.lobby.clientSettings[player].TryGetData<ArenaClientSettings>(out var currentClientSettings))
                {
                    if (!OnlineManager.lobby.clientSettings[player].inGame)
                    {
                        clientFlags += "L";
                    }
                }
            }
            return $" [{clientFlags}]";
        }
    }
}
