using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace RainMeadow
{
    public static class DebugOverlay
    {
        private static FContainer overlayContainer;

        public abstract class DebugNode
        {
            protected FSprite lineSprite;
            protected FSprite lineSprite2;
            protected FLabel label;
            public Vector2 pos;
            public Color color = Color.white;
            public float thickness = 3;
            public int lines = 0;
            public List<EntityIconPair> childEntities = [];
            public int entityNodeCount;

            public float width => label.textRect.width;
            public abstract bool ClaimsEntity(OnlineEntity x);
            public DebugNode(FContainer container)
            {
                lineSprite = new FSprite("pixel");
                lineSprite2 = new FSprite("pixel");

                label = new FLabel(Custom.GetFont(), "node") { 
                    alignment =  FLabelAlignment.Left,
                    color = Color.white
                };

                container.AddChild(lineSprite);
                container.AddChild(lineSprite2);
                container.AddChild(label);
            }

            public virtual void Update()
            {
                label.x = pos.x + 0.01f + 20;
                label.y = pos.y;

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
                lines = 0;
            }

            public virtual void RemoveSprites()
            {
                lineSprite.RemoveFromContainer();
                lineSprite2.RemoveFromContainer();
                label.RemoveFromContainer();
            }
        }

        public  class ResourceDebugNode : DebugNode
        {
            public OnlineResource Resource { get; set; }
            public ResourceDebugNode(FContainer container, OnlineResource resource) : base(container)
            {
                this.Resource = resource;
                this.label.text = resource.ToString();
            }

            public override bool ClaimsEntity(OnlineEntity x) => x.currentlyJoinedResource == Resource;
            public override void Update()
            {
                base.Update();
                label.color =
                        Resource.isOwner ? Color.green :
                        Resource.isSupervisor ? Color.blue :
                        Resource.canRelease ? Color.red : Color.white;
                
            }
        }


        private class PlayerDebugNode : DebugNode
        {
            public OnlinePlayer Player { get; set; }
            public PlayerDebugNode(FContainer container, OnlinePlayer player) : base(container)
            {
                this.Player = player;
                this.label.text = player.ToString();
            }

            public override bool ClaimsEntity(OnlineEntity x) => x.owner == Player;
            public override void Update()
            {
                base.Update();
                
                StringBuilder labelText = new(Player.ToString());
                if (Player.isMe)
                {
                    this.label.color = Color.white;
                }
                else
                {
                    int ping = Utils.RealPing(Player.ping);
                    labelText.Append($" ({ping})");
                    this.label.color = Utils.RealPingColor(ping);
                }

                this.label.text = labelText.ToString();
            }
        }


        private static List<DebugNode> debugNodes = new List<DebugNode>();

        public class EntityNode
        {
            public EntityIconPair entityIcon;
            private IconSymbol iconSymbol;
            private FLabel label;
            public string text = "";
            public Vector2 pos;
            public Color color = Color.white;
            public float rad = 5;
            public float thickness = 3;
            public EntityNode(FContainer container, EntityIconPair entityIcon)
            {
                this.entityIcon = entityIcon;
                
                if (entityIcon.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature)
                {
                    iconSymbol = new CreatureSymbol(entityIcon.Icon, container);
                }
                else
                {
                    iconSymbol = new ItemSymbol(entityIcon.Icon, container);
                }

                iconSymbol.Show(true);
                iconSymbol.showFlash = iconSymbol.lastShowFlash = 0;

                label = new FLabel(Custom.GetFont(), text);
                label.color = Color.white;

                container.AddChild(label);
            }

            public void Update()
            {
                float alpha = entityIcon.Entity.isMine ? 1 : 0.5f;
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

        public class EntityIconPair
        {
            public OnlineEntity Entity { get; set; }
            public IconSymbol.IconSymbolData Icon { get; set; }
        }
        public static int TraverseAddResources(ResourceDebugNode node)
        {      

            if (node.Resource.isActive)
            {
                node.lines = 0;
                foreach (OnlineResource resource in node.Resource.subresources)
                {
                    node.lines += 1;
                    ResourceDebugNode childNode = debugNodes.OfType<ResourceDebugNode>().FirstOrDefault(regionNode => regionNode.Resource == resource);
                    if (childNode == null)
                    {
                        childNode = new ResourceDebugNode(overlayContainer, resource);
                        int index = debugNodes.IndexOf(node);
                        if (index >= 0) 
                        {
                            debugNodes.Insert(index + 1, childNode);
                        }
                        else 
                        {
                            debugNodes.Add(childNode);
                        }
                    }
                    childNode.pos = node.pos + new Vector2(20, node.lines * -35);
                    node.lines += TraverseAddResources(childNode);
                }
            }
            return node.lines;
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

            if (self.devToolsActive && Input.GetKey(KeyCode.Minus) && !keyDown)
            {
                ownershipView = !ownershipView;
            }
            keyDown = self.devToolsActive && Input.GetKey(KeyCode.Minus);

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

            debugNodes.RemoveAll(node =>
            {
                if (node is ResourceDebugNode rdn && (ownershipView || !rdn.Resource.isActive) && rdn.Resource is not Lobby)
                {
                    node.RemoveSprites();
                    return true;
                }
                if (node is PlayerDebugNode pdn && (!ownershipView || pdn.Player.hasLeft))
                {
                    node.RemoveSprites();
                    return true;
                }
                return false;
            });

            var root = debugNodes[0];
            if (!ownershipView) // World
            {
                TraverseAddResources((ResourceDebugNode)debugNodes[0]);
            }
            else
            {
                // Players
                var players = OnlineManager.lobby.participants.OrderBy(x => x.inLobbyId);
                foreach(var player in players)
                {
                    DebugNode playerNode = debugNodes.OfType<PlayerDebugNode>().FirstOrDefault(playerNode => playerNode.Player == player);
                    if (playerNode == null)
                    {
                        playerNode = new PlayerDebugNode(overlayContainer, player);
                        debugNodes.Add(playerNode);
                    }

                    root.lines += 1;
                    playerNode.pos = root.pos + new Vector2(20, root.lines * -35);
                }
            }


            //Creature icons

            for (int i = 0; i < entityNodes.Count; i++)
            {
                entityNodes[i].RemoveSprites();
            }
            entityNodes.Clear();

            for (int i = 0; i < debugNodes.Count; i++)
            {
                debugNodes[i].Update();
                debugNodes[i].entityNodeCount = 0;
                debugNodes[i].childEntities.Clear();
            }

            List<OnlineEntity> onlineEntities = OnlineManager.recentEntities.Values.ToList();
            for (int i = 0; i < onlineEntities.Count; i++)
            {
                DebugNode resourceNode = debugNodes.Find(node => node.ClaimsEntity(onlineEntities[i]));
                if (resourceNode is null) return;

                if (onlineEntities[i] is OnlinePhysicalObject onlinePhysicalObject)
                {
                    resourceNode.childEntities.Add(new EntityIconPair
                    {
                        Entity = onlineEntities[i],
                        Icon = (((OnlinePhysicalObject)onlineEntities[i]).apo is AbstractCreature creature) ?
                        CreatureSymbol.SymbolDataFromCreature(creature) :
                        ItemSymbol.SymbolDataFromItem(((OnlinePhysicalObject)onlineEntities[i]).apo).GetValueOrDefault(),
                    });
                }
                
                if (onlineEntities[i] is OnlinePearlString pearlString)
                {
                    resourceNode.childEntities.Add(new EntityIconPair
                    {
                        Entity = pearlString,
                        Icon = new IconSymbol.IconSymbolData(CreatureTemplate.Type.StandardGroundCreature, AbstractPhysicalObject.AbstractObjectType.PebblesPearl, 2),
                    });
                }
            }

            for (int i = 0; i < debugNodes.Count; i++)
            {
                if (debugNodes[i].childEntities.Count == 0) { continue; }

                debugNodes[i].childEntities.Sort((x, y) =>
                {
                    int comp = (x.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature ? -1 : 0) + (y.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature ? 1 : 0); //Creatures then items
                    if (comp != 0) { return comp; }
                    if ((x.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature) && (y.Icon.itemType == AbstractPhysicalObject.AbstractObjectType.Creature))
                    {
                        comp = 0;
                        if (x.Entity is OnlineCreature critter && critter.isAvatar) comp -= 1;
                        if (y.Entity is OnlineCreature critter2 && critter2.isAvatar) comp += 1;
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

                IconSymbol.IconSymbolData lastIconType = new();
                int iconCount = 1;
                bool lastIsMine = true;
                bool lastAvatar = false;
                
                for  (int j = 0; j < debugNodes[i].childEntities.Count; j++)
                {
                    IconSymbol.IconSymbolData iconType = debugNodes[i].childEntities[j].Icon;
                    if (iconType == lastIconType && debugNodes[i].childEntities[j].Entity.isMine == lastIsMine && !lastAvatar)
                    {
                        iconCount++;
                        entityNodes.Last().text = iconCount.ToString();
                    }
                    else
                    {
                        iconCount = 1;
                        //Ok so, I'm not really using EntityNode for its intended purpose here; it's designed to link a creature instance to an icon, and I'm using it to just show *an* icon.
                        //I've done this because the code already works and does roughly what I want with very few modifications, and also so EntityNode can be reused later if it's ever helpful.

                        lastAvatar = false;
                        if (debugNodes[i].childEntities[j].Entity is OnlineCreature critter && critter.isMine && critter.isAvatar) lastAvatar = true;

                        EntityNode entityNode = new EntityNode(overlayContainer, debugNodes[i].childEntities[j])
                        {
                            text = lastAvatar?
                                iconCount > 1 ?
                                    "U" + iconCount.ToString() : //This should never happen in normal gameplay, but, may as well support it I guess.
                                    "YOU" :
                                iconCount.ToString()
                        };
                        entityNodes.Add(entityNode);
                        entityNode.pos = debugNodes[i].pos + new Vector2(40 + 22.5f * debugNodes[i].entityNodeCount + debugNodes[i].width, 0);
                        debugNodes[i].entityNodeCount++;
                    }

                    lastIconType = iconType;
                    lastIsMine = debugNodes[i].childEntities[j].Entity.isMine;
                }
            }

            for (int i = 0; i < entityNodes.Count; i++)
            {
                entityNodes[i].Update();
            }

            viewLabel.text = ownershipView ? "World - [Ownership]" : "[World] - Ownership";
            localClientSettings.text = "You:" + AssembleClientFlags(OnlineManager.mePlayer);
        }

        private static FLabel localClientSettings;
        private static FLabel viewLabel;
        private static bool ownershipView = false;
        private static bool keyDown = false;
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
            viewLabel = new(Custom.GetFont(), ownershipView ? "World - [Ownership]" : "[World] - Ownership")
            {
                alignment = FLabelAlignment.Center,
                x = 405.01f,
                y = screenSize.y - 10,
            };
            localClientSettings = (new FLabel(Custom.GetFont(), "You:" + AssembleClientFlags(OnlineManager.mePlayer))
            {
                alignment = FLabelAlignment.Left,
                x = 405.01f,
                y = screenSize.y - 25,
            });

            overlayContainer.AddChild(viewLabel);
            overlayContainer.AddChild(localClientSettings);

            // Lobby (Root)
            debugNodes.Add(new ResourceDebugNode(overlayContainer, OnlineManager.lobby)
            {
                pos = new Vector2(400, screenSize.y - 45),
            });

            Futile.stage.AddChild(overlayContainer);
        }

        public static void RemoveOverlay(RainWorldGame self)
        {
            debugNodes.Clear();
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
                    clientFlags += OnlineManager.lobby.owner == player ? "H" : "";
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
